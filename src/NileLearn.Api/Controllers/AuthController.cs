using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NileLearn.Application.Common.Models;
using NileLearn.Application.Features.Auth;

namespace NileLearn.Api.Controllers;

public class AuthController : BaseApiController
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        var result = await _authService.RegisterAsync(request, ClientIpAddress, ClientUserAgent, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var result = await _authService.LoginAsync(request, ClientIpAddress, ClientUserAgent, ct);
        return result.Success ? Ok(result) : Unauthorized(result);
    }

    [HttpPost("refresh-token")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> RefreshToken([FromBody] RefreshTokenRequest request, CancellationToken ct)
    {
        var result = await _authService.RefreshTokenAsync(request.RefreshToken, ClientIpAddress, ClientUserAgent, ct);
        return result.Success ? Ok(result) : Unauthorized(result);
    }

    [Authorize]
    [HttpGet("profile")]
    public async Task<ActionResult<ApiResponse<UserProfileDto>>> GetProfile(CancellationToken ct)
    {
        var result = await _authService.GetProfileAsync(CurrentUserId, ct);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [Authorize]
    [HttpPut("profile")]
    public async Task<ActionResult<ApiResponse<UserProfileDto>>> UpdateProfile([FromBody] UpdateProfileRequest request, CancellationToken ct)
    {
        var result = await _authService.UpdateProfileAsync(CurrentUserId, request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<ActionResult<ApiResponse<bool>>> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken ct)
    {
        var result = await _authService.ChangePasswordAsync(CurrentUserId, request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [Authorize]
    [HttpGet("sessions")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SessionDto>>>> GetSessions(CancellationToken ct)
    {
        var currentToken = Request.Headers["X-Refresh-Token"].ToString();
        var result = await _authService.GetUserSessionsAsync(CurrentUserId, currentToken, ct);
        return Ok(result);
    }

    [Authorize]
    [HttpDelete("sessions/{sessionId:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> RevokeSession(Guid sessionId, CancellationToken ct)
    {
        var result = await _authService.RevokeSessionAsync(CurrentUserId, sessionId, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
