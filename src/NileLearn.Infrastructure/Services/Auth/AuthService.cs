using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NileLearn.Application.Common.Interfaces;
using NileLearn.Application.Common.Models;
using NileLearn.Application.Features.Auth;
using NileLearn.Domain.Entities.Identity;
using NileLearn.Domain.Enums;

namespace NileLearn.Infrastructure.Services.Auth;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly IApplicationDbContext _context;
    private readonly IJwtTokenGenerator _tokenGenerator;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        IApplicationDbContext context,
        IJwtTokenGenerator tokenGenerator)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _context = context;
        _tokenGenerator = tokenGenerator;
    }

    public async Task<ApiResponse<AuthResponse>> RegisterAsync(RegisterRequest request, string ipAddress, string userAgent, CancellationToken ct = default)
    {
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            return ApiResponse<AuthResponse>.Fail("An account with this email address already exists.", "يوجد حساب مسجل بهذا البريد الإلكتروني بالفعل.");
        }

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FullName = request.FullName,
            PhoneNumber = request.PhoneNumber,
            PreferredLanguage = request.PreferredLanguage ?? "ar",
            EmailConfirmed = true // Auto-confirmed in initial setup
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).ToList();
            return ApiResponse<AuthResponse>.Fail("Registration failed", "فشل إنشاء الحساب", errors);
        }

        // Assign role
        var roleName = request.Role.ToString();
        if (!await _roleManager.RoleExistsAsync(roleName))
        {
            await _roleManager.CreateAsync(new ApplicationRole(roleName));
        }
        await _userManager.AddToRoleAsync(user, roleName);

        // If instructor, create instructor profile
        if (request.Role == UserRoleType.Instructor)
        {
            var instructorProfile = new InstructorProfile
            {
                UserId = user.Id,
                TitleEn = "Instructor",
                TitleAr = "مدرب معتمد",
                BiographyEn = "Welcome to my instructor profile.",
                BiographyAr = "أهلاً بكم في صفحتي التعليمية."
            };
            _context.InstructorProfiles.Add(instructorProfile);
            await _context.SaveChangesAsync(ct);
        }

        // Create session & tokens
        return await CreateAuthResponseForUserAsync(user, ipAddress, userAgent, ct);
    }

    public async Task<ApiResponse<AuthResponse>> LoginAsync(LoginRequest request, string ipAddress, string userAgent, CancellationToken ct = default)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            return ApiResponse<AuthResponse>.Fail("Invalid email or password.", "البريد الإلكتروني أو كلمة المرور غير صحيحة.");
        }

        if (user.IsSuspended)
        {
            return ApiResponse<AuthResponse>.Fail($"Your account has been suspended: {user.SuspensionReason}", $"تم تعليق حسابك: {user.SuspensionReason}");
        }

        var isPasswordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!isPasswordValid)
        {
            return ApiResponse<AuthResponse>.Fail("Invalid email or password.", "البريد الإلكتروني أو كلمة المرور غير صحيحة.");
        }

        user.LastLoginAtUtc = DateTime.UtcNow;
        user.LastLoginIp = ipAddress;
        await _userManager.UpdateAsync(user);

        return await CreateAuthResponseForUserAsync(user, ipAddress, userAgent, ct, request.DeviceName);
    }

    public async Task<ApiResponse<AuthResponse>> RefreshTokenAsync(string refreshToken, string ipAddress, string userAgent, CancellationToken ct = default)
    {
        var hashedToken = _tokenGenerator.HashToken(refreshToken);

        var session = await _context.UserSessions
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.RefreshTokenHash == hashedToken && !s.IsRevoked, ct);

        if (session == null || session.RefreshTokenExpiresAtUtc < DateTime.UtcNow)
        {
            return ApiResponse<AuthResponse>.Fail("Invalid or expired refresh token.", "رمز التحديث غير صالح أو منتهي الصلاحية.");
        }

        if (session.User.IsSuspended)
        {
            return ApiResponse<AuthResponse>.Fail("Account is suspended.", "الحساب معلق.");
        }

        // Rotate refresh token
        var newRefreshToken = _tokenGenerator.GenerateRefreshToken();
        session.RefreshTokenHash = _tokenGenerator.HashToken(newRefreshToken);
        session.RefreshTokenExpiresAtUtc = DateTime.UtcNow.AddDays(14);
        session.LastActiveAtUtc = DateTime.UtcNow;
        session.IpAddress = ipAddress;

        await _context.SaveChangesAsync(ct);

        var roles = await _userManager.GetRolesAsync(session.User);
        var accessToken = _tokenGenerator.GenerateAccessToken(session.User.Id, session.User.Email!, session.User.FullName, roles, out var expiresAtUtc);

        var response = new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = newRefreshToken,
            ExpiresAtUtc = expiresAtUtc,
            User = MapToUserProfileDto(session.User, roles)
        };

        return ApiResponse<AuthResponse>.Ok(response, "Token refreshed successfully.", "تم تجديد جلسة الدخول بنجاح.");
    }

    public async Task<ApiResponse<bool>> RevokeSessionAsync(Guid userId, Guid sessionId, CancellationToken ct = default)
    {
        var session = await _context.UserSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId, ct);

        if (session == null)
        {
            return ApiResponse<bool>.Fail("Session not found.", "الجلسة غير موجودة.");
        }

        session.IsRevoked = true;
        session.RevokedReason = "Revoked by user";
        await _context.SaveChangesAsync(ct);

        return ApiResponse<bool>.Ok(true, "Session revoked.", "تم إنهاء الجلسة بنجاح.");
    }

    public async Task<ApiResponse<IReadOnlyList<SessionDto>>> GetUserSessionsAsync(Guid userId, string? currentRefreshToken, CancellationToken ct = default)
    {
        var hashedCurrentToken = !string.IsNullOrEmpty(currentRefreshToken) ? _tokenGenerator.HashToken(currentRefreshToken) : null;

        var sessions = await _context.UserSessions
            .Where(s => s.UserId == userId && !s.IsRevoked)
            .OrderByDescending(s => s.LastActiveAtUtc)
            .Select(s => new SessionDto
            {
                Id = s.Id,
                DeviceName = s.DeviceName,
                IpAddress = s.IpAddress,
                UserAgent = s.UserAgent,
                CreatedAtUtc = s.CreatedAtUtc,
                LastActiveAtUtc = s.LastActiveAtUtc,
                IsCurrentSession = hashedCurrentToken != null && s.RefreshTokenHash == hashedCurrentToken
            })
            .ToListAsync(ct);

        return ApiResponse<IReadOnlyList<SessionDto>>.Ok(sessions);
    }

    public async Task<ApiResponse<UserProfileDto>> GetProfileAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return ApiResponse<UserProfileDto>.Fail("User not found.", "المستخدم غير موجود.");
        }

        var roles = await _userManager.GetRolesAsync(user);
        return ApiResponse<UserProfileDto>.Ok(MapToUserProfileDto(user, roles));
    }

    public async Task<ApiResponse<UserProfileDto>> UpdateProfileAsync(Guid userId, UpdateProfileRequest request, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return ApiResponse<UserProfileDto>.Fail("User not found.", "المستخدم غير موجود.");
        }

        user.FullName = request.FullName;
        user.PhoneNumber = request.PhoneNumber;
        user.Headline = request.Headline;
        user.Bio = request.Bio;
        user.PreferredLanguage = request.PreferredLanguage;

        await _userManager.UpdateAsync(user);
        var roles = await _userManager.GetRolesAsync(user);

        return ApiResponse<UserProfileDto>.Ok(MapToUserProfileDto(user, roles), "Profile updated successfully.", "تم تحديث الملف الشخصي بنجاح.");
    }

    public async Task<ApiResponse<bool>> ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return ApiResponse<bool>.Fail("User not found.", "المستخدم غير موجود.");
        }

        var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).ToList();
            return ApiResponse<bool>.Fail("Failed to change password.", "فشل تغيير كلمة المرور.", errors);
        }

        return ApiResponse<bool>.Ok(true, "Password changed successfully.", "تم تغيير كلمة المرور بنجاح.");
    }

    public async Task<ApiResponse<bool>> VerifyEmailAsync(string email, string token, CancellationToken ct = default)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null) return ApiResponse<bool>.Fail("User not found.", "المستخدم غير موجود.");

        user.EmailConfirmed = true;
        await _userManager.UpdateAsync(user);
        return ApiResponse<bool>.Ok(true, "Email verified successfully.", "تم تأكيد البريد الإلكتروني بنجاح.");
    }

    private async Task<ApiResponse<AuthResponse>> CreateAuthResponseForUserAsync(
        ApplicationUser user,
        string ipAddress,
        string userAgent,
        CancellationToken ct,
        string deviceName = "Web Browser")
    {
        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = _tokenGenerator.GenerateAccessToken(user.Id, user.Email!, user.FullName, roles, out var expiresAtUtc);
        var refreshToken = _tokenGenerator.GenerateRefreshToken();

        var session = new UserSession
        {
            UserId = user.Id,
            DeviceName = deviceName,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            RefreshTokenHash = _tokenGenerator.HashToken(refreshToken),
            RefreshTokenExpiresAtUtc = DateTime.UtcNow.AddDays(14),
            LastActiveAtUtc = DateTime.UtcNow
        };

        _context.UserSessions.Add(session);
        await _context.SaveChangesAsync(ct);

        var response = new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAtUtc = expiresAtUtc,
            User = MapToUserProfileDto(user, roles)
        };

        return ApiResponse<AuthResponse>.Ok(response, "Authentication successful.", "تم تسجيل الدخول بنجاح.");
    }

    private static UserProfileDto MapToUserProfileDto(ApplicationUser user, IList<string> roles)
    {
        return new UserProfileDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            PhoneNumber = user.PhoneNumber,
            ProfilePictureUrl = user.ProfilePictureUrl,
            Headline = user.Headline,
            Bio = user.Bio,
            PreferredLanguage = user.PreferredLanguage,
            Roles = roles,
            EmailConfirmed = user.EmailConfirmed,
            PhoneNumberConfirmed = user.PhoneNumberConfirmed,
            TwoFactorEnabled = user.TwoFactorEnabled
        };
    }
}
