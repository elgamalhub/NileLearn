using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NileLearn.Application.Common.Interfaces;
using NileLearn.Application.Common.Models;
using NileLearn.Application.Features.Billing;
using NileLearn.Domain.Entities.Billing;
using NileLearn.Domain.Enums;

namespace NileLearn.Infrastructure.Services.Billing;

public class EntitlementService : IEntitlementService
{
    private readonly IApplicationDbContext _context;

    public EntitlementService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> HasAccessToCourseAsync(Guid userId, Guid courseId, CancellationToken ct = default)
    {
        // 1. Direct enrollment
        var hasEnrollment = await _context.Enrollments
            .AnyAsync(e => e.UserId == userId && e.CourseId == courseId, ct);

        if (hasEnrollment) return true;

        // 2. Is course free?
        var isFreeCourse = await _context.Courses
            .Where(c => c.Id == courseId)
            .Select(c => c.IsFree)
            .FirstOrDefaultAsync(ct);

        if (isFreeCourse) return true;

        // 3. Active subscription covering all courses or category
        var course = await _context.Courses
            .Where(c => c.Id == courseId)
            .Select(c => new { c.Id, c.CategoryId })
            .FirstOrDefaultAsync(ct);

        if (course == null) return false;

        var activeSubscription = await _context.Subscriptions
            .Include(s => s.Plan).ThenInclude(p => p.Entitlements)
            .Where(s => s.UserId == userId &&
                        (s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.Trialing) &&
                        s.CurrentPeriodEndUtc >= DateTime.UtcNow)
            .FirstOrDefaultAsync(ct);

        if (activeSubscription != null)
        {
            // Check entitlements
            var entitlements = activeSubscription.Plan.Entitlements;
            if (!entitlements.Any() || entitlements.Any(e => e.AppliesToAllCourses))
            {
                return true;
            }

            if (entitlements.Any(e => e.CourseId == course.Id || e.CategoryId == course.CategoryId))
            {
                return true;
            }
        }

        return false;
    }

    public async Task<bool> HasAccessToLessonAsync(Guid userId, Guid lessonId, CancellationToken ct = default)
    {
        var lesson = await _context.Lessons
            .Include(l => l.Section)
            .FirstOrDefaultAsync(l => l.Id == lessonId, ct);

        if (lesson == null) return false;

        // Free preview lessons are accessible to anyone authenticated
        if (lesson.IsFreePreview) return true;

        return await HasAccessToCourseAsync(userId, lesson.Section.CourseId, ct);
    }

    public async Task<int> GetActiveDeviceSessionsCountAsync(Guid userId, CancellationToken ct = default)
    {
        var cutoff = DateTime.UtcNow.AddMinutes(-30);
        return await _context.UserSessions
            .CountAsync(s => s.UserId == userId && !s.IsRevoked && s.LastActiveAtUtc >= cutoff, ct);
    }
}

public class SubscriptionService : ISubscriptionService
{
    private readonly IApplicationDbContext _context;

    public SubscriptionService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<IReadOnlyList<SubscriptionPlanDto>>> GetPlansAsync(CancellationToken ct = default)
    {
        var plans = await _context.SubscriptionPlans
            .Where(p => p.IsActive)
            .OrderBy(p => p.DisplayOrder)
            .ToListAsync(ct);

        var dtos = plans.Select(p => new SubscriptionPlanDto
        {
            Id = p.Id,
            NameEn = p.NameEn,
            NameAr = p.NameAr,
            Slug = p.Slug,
            DescriptionEn = p.DescriptionEn,
            DescriptionAr = p.DescriptionAr,
            MonthlyPriceEgp = p.MonthlyPriceEgp,
            AnnualPriceEgp = p.AnnualPriceEgp,
            Currency = p.Currency,
            MaxSimultaneousDevices = p.MaxSimultaneousDevices,
            MaxCourseAccessLimit = p.MaxCourseAccessLimit,
            AllowDownloads = p.AllowDownloads,
            TrialPeriodDays = p.TrialPeriodDays,
            GracePeriodDays = p.GracePeriodDays,
            FeaturesEn = DeserializeFeatures(p.FeaturesJsonEn),
            FeaturesAr = DeserializeFeatures(p.FeaturesJsonAr)
        }).ToList();

        return ApiResponse<IReadOnlyList<SubscriptionPlanDto>>.Ok(dtos);
    }

    public async Task<ApiResponse<UserSubscriptionDto?>> GetUserActiveSubscriptionAsync(Guid userId, CancellationToken ct = default)
    {
        var subscription = await _context.Subscriptions
            .Include(s => s.Plan)
            .Where(s => s.UserId == userId &&
                        (s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.Trialing || s.Status == SubscriptionStatus.PastDue))
            .OrderByDescending(s => s.CurrentPeriodEndUtc)
            .FirstOrDefaultAsync(ct);

        if (subscription == null)
        {
            return ApiResponse<UserSubscriptionDto?>.Ok(null);
        }

        var dto = new UserSubscriptionDto
        {
            Id = subscription.Id,
            PlanId = subscription.PlanId,
            PlanNameEn = subscription.Plan.NameEn,
            PlanNameAr = subscription.Plan.NameAr,
            BillingCycle = subscription.BillingCycle,
            Status = subscription.Status,
            CurrentPeriodStartUtc = subscription.CurrentPeriodStartUtc,
            CurrentPeriodEndUtc = subscription.CurrentPeriodEndUtc,
            CancelAtPeriodEnd = subscription.CancelAtPeriodEnd,
            AutoRenew = subscription.AutoRenew
        };

        return ApiResponse<UserSubscriptionDto?>.Ok(dto);
    }

    public async Task<ApiResponse<Guid>> CreatePlanAsync(CreatePlanRequest request, CancellationToken ct = default)
    {
        var slug = request.NameEn.ToLower().Replace(" ", "-");
        var plan = new SubscriptionPlan
        {
            NameEn = request.NameEn,
            NameAr = request.NameAr,
            Slug = slug,
            DescriptionEn = request.DescriptionEn,
            DescriptionAr = request.DescriptionAr,
            MonthlyPriceEgp = request.MonthlyPriceEgp,
            AnnualPriceEgp = request.AnnualPriceEgp,
            MaxSimultaneousDevices = request.MaxSimultaneousDevices,
            MaxCourseAccessLimit = request.MaxCourseAccessLimit,
            AllowDownloads = request.AllowDownloads,
            TrialPeriodDays = request.TrialPeriodDays,
            GracePeriodDays = request.GracePeriodDays,
            FeaturesJsonEn = JsonSerializer.Serialize(request.FeaturesEn),
            FeaturesJsonAr = JsonSerializer.Serialize(request.FeaturesAr)
        };

        _context.SubscriptionPlans.Add(plan);
        await _context.SaveChangesAsync(ct);

        return ApiResponse<Guid>.Ok(plan.Id, "Subscription plan created.", "تم إنشاء خطة الاشتراك بنجاح.");
    }

    public async Task<ApiResponse<bool>> CancelSubscriptionAsync(Guid userId, string? reason, CancellationToken ct = default)
    {
        var subscription = await _context.Subscriptions
            .Where(s => s.UserId == userId && s.Status == SubscriptionStatus.Active)
            .FirstOrDefaultAsync(ct);

        if (subscription == null)
        {
            return ApiResponse<bool>.Fail("Active subscription not found.", "لا يوجد اشتراك نشط للإلغاء.");
        }

        subscription.CancelAtPeriodEnd = true;
        subscription.CancelledAtUtc = DateTime.UtcNow;
        subscription.CancellationReason = reason ?? "Cancelled by user";
        subscription.AutoRenew = false;

        await _context.SaveChangesAsync(ct);
        return ApiResponse<bool>.Ok(true, "Subscription will cancel at the end of the billing period.", "سيتم إلغاء الاشتراك بنهاية فترة الفاتورة الحالية.");
    }

    private static List<string> DeserializeFeatures(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new List<string>();
        try { return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>(); }
        catch { return new List<string>(); }
    }
}
