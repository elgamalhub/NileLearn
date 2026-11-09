using Microsoft.AspNetCore.Identity;
using NileLearn.Domain.Common;
using NileLearn.Domain.Enums;

namespace NileLearn.Domain.Entities.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    public string FullName { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }
    public string? Headline { get; set; }
    public string? Bio { get; set; }
    public string PreferredLanguage { get; set; } = "ar"; // "ar" or "en"
    public bool IsSuspended { get; set; }
    public string? SuspensionReason { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAtUtc { get; set; }
    public string? LastLoginIp { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }

    // Navigation
    public virtual ICollection<UserSession> Sessions { get; set; } = new List<UserSession>();
    public virtual InstructorProfile? InstructorProfile { get; set; }
}

public class ApplicationRole : IdentityRole<Guid>
{
    public string? Description { get; set; }
    public string? DescriptionAr { get; set; }

    public ApplicationRole() : base() { }
    public ApplicationRole(string roleName) : base(roleName) { }
}

public class UserSession : BaseEntity
{
    public Guid UserId { get; set; }
    public virtual ApplicationUser User { get; set; } = null!;
    public string DeviceName { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public string RefreshTokenHash { get; set; } = string.Empty;
    public DateTime RefreshTokenExpiresAtUtc { get; set; }
    public bool IsRevoked { get; set; }
    public string? RevokedReason { get; set; }
    public DateTime LastActiveAtUtc { get; set; } = DateTime.UtcNow;
}

public class InstructorProfile : BaseEntity
{
    public Guid UserId { get; set; }
    public virtual ApplicationUser User { get; set; } = null!;
    public string TitleEn { get; set; } = string.Empty;
    public string TitleAr { get; set; } = string.Empty;
    public string BiographyEn { get; set; } = string.Empty;
    public string BiographyAr { get; set; } = string.Empty;
    public string? WebsiteUrl { get; set; }
    public string? LinkedInUrl { get; set; }
    public string? GithubUrl { get; set; }
    public string? TwitterUrl { get; set; }
    public int TotalStudentsCount { get; set; }
    public int TotalReviewsCount { get; set; }
    public double AverageRating { get; set; }
    public bool IsApproved { get; set; } = true;
    public DateTime? ApprovedAtUtc { get; set; }
    public decimal RevenueSharePercentage { get; set; } = 70.0m;
}
