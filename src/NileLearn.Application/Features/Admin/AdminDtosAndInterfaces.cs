using NileLearn.Application.Common.Models;

namespace NileLearn.Application.Features.Admin;

public class AdminAnalyticsDto
{
    public int TotalStudents { get; set; }
    public int TotalInstructors { get; set; }
    public int TotalCourses { get; set; }
    public int TotalEnrollments { get; set; }
    public decimal MonthlyRecurringRevenueEgp { get; set; }
    public decimal TotalRevenueEgp { get; set; }
    public double PaymentSuccessRate { get; set; }
    public int ActiveSubscriptionsCount { get; set; }
    public int StorageUsedMb { get; set; }
    public int VideoBandwidthUsedGb { get; set; }
    public List<RecentTransactionDto> RecentTransactions { get; set; } = new();
}

public class RecentTransactionDto
{
    public Guid PaymentId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string UserFullName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "EGP";
    public string Gateway { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}

public class AdminUserDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public IList<string> Roles { get; set; } = new List<string>();
    public bool IsSuspended { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? LastLoginAtUtc { get; set; }
}

public class AdminAuditLogDto
{
    public Guid Id { get; set; }
    public string? UserEmail { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string? IpAddress { get; set; }
}

public interface IAdminService
{
    Task<ApiResponse<AdminAnalyticsDto>> GetAnalyticsAsync(CancellationToken ct = default);
    Task<ApiResponse<PagedResult<AdminUserDto>>> GetUsersAsync(int pageIndex, int pageSize, string? searchTerm, CancellationToken ct = default);
    Task<ApiResponse<bool>> ToggleUserSuspensionAsync(Guid userId, bool suspend, string? reason, CancellationToken ct = default);
    Task<ApiResponse<bool>> UpdateUserRoleAsync(Guid userId, string newRole, CancellationToken ct = default);
    Task<ApiResponse<PagedResult<AdminAuditLogDto>>> GetAuditLogsAsync(int pageIndex, int pageSize, CancellationToken ct = default);
}
