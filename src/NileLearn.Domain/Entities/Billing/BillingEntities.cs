using NileLearn.Domain.Common;
using NileLearn.Domain.Entities.Courses;
using NileLearn.Domain.Entities.Identity;
using NileLearn.Domain.Enums;

namespace NileLearn.Domain.Entities.Billing;

public class SubscriptionPlan : BaseEntity
{
    public string NameEn { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string DescriptionEn { get; set; } = string.Empty;
    public string DescriptionAr { get; set; } = string.Empty;

    public decimal MonthlyPriceEgp { get; set; }
    public decimal AnnualPriceEgp { get; set; }
    public string Currency { get; set; } = "EGP";

    public int MaxSimultaneousDevices { get; set; } = 2;
    public int? MaxCourseAccessLimit { get; set; } // null = unlimited
    public bool AllowDownloads { get; set; }
    public int TrialPeriodDays { get; set; } = 0;
    public int GracePeriodDays { get; set; } = 3;
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }

    // JSON serialized list of feature items
    public string FeaturesJsonEn { get; set; } = "[]";
    public string FeaturesJsonAr { get; set; } = "[]";

    public virtual ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
    public virtual ICollection<SubscriptionEntitlement> Entitlements { get; set; } = new List<SubscriptionEntitlement>();
}

public class Subscription : BaseEntity
{
    public Guid UserId { get; set; }
    public virtual ApplicationUser User { get; set; } = null!;

    public Guid PlanId { get; set; }
    public virtual SubscriptionPlan Plan { get; set; } = null!;

    public BillingCycle BillingCycle { get; set; } = BillingCycle.Monthly;
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;

    public DateTime CurrentPeriodStartUtc { get; set; }
    public DateTime CurrentPeriodEndUtc { get; set; }
    public DateTime? TrialEndUtc { get; set; }
    public DateTime? GracePeriodEndUtc { get; set; }

    public bool CancelAtPeriodEnd { get; set; }
    public DateTime? CancelledAtUtc { get; set; }
    public string? CancellationReason { get; set; }
    public bool AutoRenew { get; set; } = true;

    public Guid? LastPaymentId { get; set; }
}

public class SubscriptionEntitlement : BaseEntity
{
    public Guid PlanId { get; set; }
    public virtual SubscriptionPlan Plan { get; set; } = null!;

    public Guid? CourseId { get; set; }
    public virtual Course? Course { get; set; }

    public Guid? CategoryId { get; set; }
    public virtual Category? Category { get; set; }

    public bool AppliesToAllCourses { get; set; } = true;
}
