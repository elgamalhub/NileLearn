using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NileLearn.Application.Common.Interfaces;
using NileLearn.Application.Common.Models;
using NileLearn.Application.Features.Admin;
using NileLearn.Domain.Entities.Identity;
using NileLearn.Domain.Enums;

namespace NileLearn.Infrastructure.Services.Admin;

public class AdminService : IAdminService
{
    private readonly IApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminService(IApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<ApiResponse<AdminAnalyticsDto>> GetAnalyticsAsync(CancellationToken ct = default)
    {
        var totalStudents = await _userManager.GetUsersInRoleAsync(UserRoleType.Student.ToString());
        var totalInstructors = await _userManager.GetUsersInRoleAsync(UserRoleType.Instructor.ToString());
        var totalCourses = await _context.Courses.CountAsync(c => !c.IsDeleted, ct);
        var totalEnrollments = await _context.Enrollments.CountAsync(ct);

        var activeSubs = await _context.Subscriptions
            .Include(s => s.Plan)
            .Where(s => s.Status == SubscriptionStatus.Active && s.CurrentPeriodEndUtc >= DateTime.UtcNow)
            .ToListAsync(ct);

        var mrr = activeSubs.Sum(s => s.BillingCycle == BillingCycle.Annual ? s.Plan.AnnualPriceEgp / 12 : s.Plan.MonthlyPriceEgp);

        var successfulPayments = await _context.Payments
            .Where(p => p.Status == PaymentStatus.Paid)
            .ToListAsync(ct);

        var totalRevenue = successfulPayments.Sum(p => p.Amount);
        var totalPaymentAttempts = await _context.Payments.CountAsync(ct);
        var successRate = totalPaymentAttempts > 0
            ? Math.Round((double)successfulPayments.Count / totalPaymentAttempts * 100, 1)
            : 100.0;

        var recentPayments = await _context.Payments
            .Include(p => p.Order)
            .Include(p => p.User)
            .OrderByDescending(p => p.CreatedAtUtc)
            .Take(10)
            .Select(p => new RecentTransactionDto
            {
                PaymentId = p.Id,
                OrderNumber = p.Order.OrderNumber,
                UserFullName = p.User.FullName,
                Amount = p.Amount,
                Currency = p.Currency,
                Gateway = p.GatewayType.ToString(),
                Status = p.Status.ToString(),
                CreatedAtUtc = p.CreatedAtUtc
            })
            .ToListAsync(ct);

        var totalStorageBytes = await _context.VideoAssets.SumAsync(v => v.OriginalFileSizeBytes, ct);

        var dto = new AdminAnalyticsDto
        {
            TotalStudents = totalStudents.Count,
            TotalInstructors = totalInstructors.Count,
            TotalCourses = totalCourses,
            TotalEnrollments = totalEnrollments,
            MonthlyRecurringRevenueEgp = Math.Round(mrr, 2),
            TotalRevenueEgp = Math.Round(totalRevenue, 2),
            PaymentSuccessRate = successRate,
            ActiveSubscriptionsCount = activeSubs.Count,
            StorageUsedMb = (int)(totalStorageBytes / (1024 * 1024)),
            VideoBandwidthUsedGb = (int)(totalStorageBytes * 3 / (1024 * 1024 * 1024)),
            RecentTransactions = recentPayments
        };

        return ApiResponse<AdminAnalyticsDto>.Ok(dto);
    }

    public async Task<ApiResponse<PagedResult<AdminUserDto>>> GetUsersAsync(int pageIndex, int pageSize, string? searchTerm, CancellationToken ct = default)
    {
        var query = _userManager.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(u => u.Email!.ToLower().Contains(term) || u.FullName.ToLower().Contains(term));
        }

        var totalCount = await query.CountAsync(ct);
        pageIndex = Math.Max(1, pageIndex);
        pageSize = Math.Max(1, Math.Min(100, pageSize));

        var users = await query
            .OrderByDescending(u => u.CreatedAtUtc)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var dtos = new List<AdminUserDto>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            dtos.Add(new AdminUserDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber,
                Roles = roles,
                IsSuspended = user.IsSuspended,
                CreatedAtUtc = user.CreatedAtUtc,
                LastLoginAtUtc = user.LastLoginAtUtc
            });
        }

        return ApiResponse<PagedResult<AdminUserDto>>.Ok(PagedResult<AdminUserDto>.Create(dtos, pageIndex, pageSize, totalCount));
    }

    public async Task<ApiResponse<bool>> ToggleUserSuspensionAsync(Guid userId, bool suspend, string? reason, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return ApiResponse<bool>.Fail("User not found.", "المستخدم غير موجود.");

        user.IsSuspended = suspend;
        user.SuspensionReason = suspend ? reason ?? "Administrative action" : null;
        await _userManager.UpdateAsync(user);

        return ApiResponse<bool>.Ok(true, suspend ? "User account suspended." : "User account reactivated.", suspend ? "تم تعليق حساب المستخدم." : "تم إلغاء تعليق الحساب بنجاح.");
    }

    public async Task<ApiResponse<bool>> UpdateUserRoleAsync(Guid userId, string newRole, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return ApiResponse<bool>.Fail("User not found.", "المستخدم غير موجود.");

        var currentRoles = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, currentRoles);
        await _userManager.AddToRoleAsync(user, newRole);

        return ApiResponse<bool>.Ok(true, "User role updated.", "تم تحديث دور المستخدم بنجاح.");
    }

    public async Task<ApiResponse<PagedResult<AdminAuditLogDto>>> GetAuditLogsAsync(int pageIndex, int pageSize, CancellationToken ct = default)
    {
        var query = _context.AuditLogs.AsNoTracking().OrderByDescending(a => a.CreatedAtUtc);
        var totalCount = await query.CountAsync(ct);

        pageIndex = Math.Max(1, pageIndex);
        pageSize = Math.Max(1, Math.Min(100, pageSize));

        var logs = await query
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AdminAuditLogDto
            {
                Id = a.Id,
                UserEmail = a.UserEmail,
                Action = a.Action,
                EntityName = a.EntityName,
                EntityId = a.EntityId,
                CreatedAtUtc = a.CreatedAtUtc,
                IpAddress = a.IpAddress
            })
            .ToListAsync(ct);

        return ApiResponse<PagedResult<AdminAuditLogDto>>.Ok(PagedResult<AdminAuditLogDto>.Create(logs, pageIndex, pageSize, totalCount));
    }
}
