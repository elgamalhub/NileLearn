using NileLearn.Application.Common.Models;
using NileLearn.Domain.Enums;

namespace NileLearn.Application.Features.Billing;

public class SubscriptionPlanDto
{
    public Guid Id { get; set; }
    public string NameEn { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string DescriptionEn { get; set; } = string.Empty;
    public string DescriptionAr { get; set; } = string.Empty;
    public decimal MonthlyPriceEgp { get; set; }
    public decimal AnnualPriceEgp { get; set; }
    public string Currency { get; set; } = "EGP";
    public int MaxSimultaneousDevices { get; set; }
    public int? MaxCourseAccessLimit { get; set; }
    public bool AllowDownloads { get; set; }
    public int TrialPeriodDays { get; set; }
    public int GracePeriodDays { get; set; }
    public List<string> FeaturesEn { get; set; } = new();
    public List<string> FeaturesAr { get; set; } = new();
}

public class UserSubscriptionDto
{
    public Guid Id { get; set; }
    public Guid PlanId { get; set; }
    public string PlanNameEn { get; set; } = string.Empty;
    public string PlanNameAr { get; set; } = string.Empty;
    public BillingCycle BillingCycle { get; set; }
    public SubscriptionStatus Status { get; set; }
    public DateTime CurrentPeriodStartUtc { get; set; }
    public DateTime CurrentPeriodEndUtc { get; set; }
    public bool CancelAtPeriodEnd { get; set; }
    public bool AutoRenew { get; set; }
    public bool IsActive => Status == SubscriptionStatus.Active || Status == SubscriptionStatus.Trialing;
}

public class CreatePlanRequest
{
    public string NameEn { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string DescriptionEn { get; set; } = string.Empty;
    public string DescriptionAr { get; set; } = string.Empty;
    public decimal MonthlyPriceEgp { get; set; }
    public decimal AnnualPriceEgp { get; set; }
    public int MaxSimultaneousDevices { get; set; } = 2;
    public int? MaxCourseAccessLimit { get; set; }
    public bool AllowDownloads { get; set; }
    public int TrialPeriodDays { get; set; } = 0;
    public int GracePeriodDays { get; set; } = 3;
    public List<string> FeaturesEn { get; set; } = new();
    public List<string> FeaturesAr { get; set; } = new();
}

public interface ISubscriptionService
{
    Task<ApiResponse<IReadOnlyList<SubscriptionPlanDto>>> GetPlansAsync(CancellationToken ct = default);
    Task<ApiResponse<UserSubscriptionDto?>> GetUserActiveSubscriptionAsync(Guid userId, CancellationToken ct = default);
    Task<ApiResponse<Guid>> CreatePlanAsync(CreatePlanRequest request, CancellationToken ct = default);
    Task<ApiResponse<bool>> CancelSubscriptionAsync(Guid userId, string? reason, CancellationToken ct = default);
}

public interface IEntitlementService
{
    Task<bool> HasAccessToCourseAsync(Guid userId, Guid courseId, CancellationToken ct = default);
    Task<bool> HasAccessToLessonAsync(Guid userId, Guid lessonId, CancellationToken ct = default);
    Task<int> GetActiveDeviceSessionsCountAsync(Guid userId, CancellationToken ct = default);
}
