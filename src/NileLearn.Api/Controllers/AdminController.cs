using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NileLearn.Application.Common.Models;
using NileLearn.Application.Features.Admin;

namespace NileLearn.Api.Controllers;

[Authorize(Roles = "Admin,SuperAdmin")]
public class AdminController : BaseApiController
{
    private readonly IAdminService _adminService;

    public AdminController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    [HttpGet("analytics")]
    public async Task<ActionResult<ApiResponse<AdminAnalyticsDto>>> GetAnalytics(CancellationToken ct)
    {
        var result = await _adminService.GetAnalyticsAsync(ct);
        return Ok(result);
    }

    [HttpGet("users")]
    public async Task<ActionResult<ApiResponse<PagedResult<AdminUserDto>>>> GetUsers(
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 15,
        [FromQuery] string? searchTerm = null,
        CancellationToken ct = default)
    {
        var result = await _adminService.GetUsersAsync(pageIndex, pageSize, searchTerm, ct);
        return Ok(result);
    }

    [HttpPost("users/{userId:guid}/toggle-suspend")]
    public async Task<ActionResult<ApiResponse<bool>>> ToggleSuspend(Guid userId, [FromBody] SuspendUserRequest request, CancellationToken ct)
    {
        var result = await _adminService.ToggleUserSuspensionAsync(userId, request.Suspend, request.Reason, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [Authorize(Roles = "SuperAdmin")]
    [HttpPost("users/{userId:guid}/role")]
    public async Task<ActionResult<ApiResponse<bool>>> UpdateRole(Guid userId, [FromBody] UpdateRoleRequest request, CancellationToken ct)
    {
        var result = await _adminService.UpdateUserRoleAsync(userId, request.NewRole, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("audit-logs")]
    public async Task<ActionResult<ApiResponse<PagedResult<AdminAuditLogDto>>>> GetAuditLogs(
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _adminService.GetAuditLogsAsync(pageIndex, pageSize, ct);
        return Ok(result);
    }
}

public class SuspendUserRequest
{
    public bool Suspend { get; set; }
    public string? Reason { get; set; }
}

public class UpdateRoleRequest
{
    public string NewRole { get; set; } = string.Empty;
}
