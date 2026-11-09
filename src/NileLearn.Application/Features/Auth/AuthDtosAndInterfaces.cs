using NileLearn.Application.Common.Models;
using NileLearn.Domain.Enums;

namespace NileLearn.Application.Features.Auth;

public class RegisterRequest
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string PreferredLanguage { get; set; } = "ar";
    public UserRoleType Role { get; set; } = UserRoleType.Student;
}

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? TwoFactorCode { get; set; }
    public string DeviceName { get; set; } = "Web Browser";
}

public class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}

public class AuthResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public UserProfileDto User { get; set; } = null!;
}

public class UserProfileDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public string? Headline { get; set; }
    public string? Bio { get; set; }
    public string PreferredLanguage { get; set; } = "ar";
    public IList<string> Roles { get; set; } = new List<string>();
    public bool EmailConfirmed { get; set; }
    public bool PhoneNumberConfirmed { get; set; }
    public bool TwoFactorEnabled { get; set; }
}

public class UpdateProfileRequest
{
    public string FullName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Headline { get; set; }
    public string? Bio { get; set; }
    public string PreferredLanguage { get; set; } = "ar";
}

public class ChangePasswordRequest
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

public class SessionDto
{
    public Guid Id { get; set; }
    public string DeviceName { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime LastActiveAtUtc { get; set; }
    public bool IsCurrentSession { get; set; }
}

public interface IAuthService
{
    Task<ApiResponse<AuthResponse>> RegisterAsync(RegisterRequest request, string ipAddress, string userAgent, CancellationToken ct = default);
    Task<ApiResponse<AuthResponse>> LoginAsync(LoginRequest request, string ipAddress, string userAgent, CancellationToken ct = default);
    Task<ApiResponse<AuthResponse>> RefreshTokenAsync(string refreshToken, string ipAddress, string userAgent, CancellationToken ct = default);
    Task<ApiResponse<bool>> RevokeSessionAsync(Guid userId, Guid sessionId, CancellationToken ct = default);
    Task<ApiResponse<IReadOnlyList<SessionDto>>> GetUserSessionsAsync(Guid userId, string? currentRefreshToken, CancellationToken ct = default);
    Task<ApiResponse<UserProfileDto>> GetProfileAsync(Guid userId, CancellationToken ct = default);
    Task<ApiResponse<UserProfileDto>> UpdateProfileAsync(Guid userId, UpdateProfileRequest request, CancellationToken ct = default);
    Task<ApiResponse<bool>> ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken ct = default);
    Task<ApiResponse<bool>> VerifyEmailAsync(string email, string token, CancellationToken ct = default);
}

public interface IJwtTokenGenerator
{
    string GenerateAccessToken(Guid userId, string email, string fullName, IEnumerable<string> roles, out DateTime expiresAtUtc);
    string GenerateRefreshToken();
    string HashToken(string token);
}
