using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NileLearn.Application.Common.Models;
using NileLearn.Application.Features.Billing;

namespace NileLearn.Api.Controllers;

public class SubscriptionsController : BaseApiController
{
    private readonly ISubscriptionService _subscriptionService;

    public SubscriptionsController(ISubscriptionService subscriptionService)
    {
        _subscriptionService = subscriptionService;
    }

    [HttpGet("plans")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SubscriptionPlanDto>>>> GetPlans(CancellationToken ct)
    {
        var result = await _subscriptionService.GetPlansAsync(ct);
        return Ok(result);
    }

    [Authorize]
    [HttpGet("my-subscription")]
    public async Task<ActionResult<ApiResponse<UserSubscriptionDto?>>> GetMySubscription(CancellationToken ct)
    {
        var result = await _subscriptionService.GetUserActiveSubscriptionAsync(CurrentUserId, ct);
        return Ok(result);
    }

    [Authorize(Roles = "Admin,SuperAdmin")]
    [HttpPost("plans")]
    public async Task<ActionResult<ApiResponse<Guid>>> CreatePlan([FromBody] CreatePlanRequest request, CancellationToken ct)
    {
        var result = await _subscriptionService.CreatePlanAsync(request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [Authorize]
    [HttpPost("cancel")]
    public async Task<ActionResult<ApiResponse<bool>>> CancelSubscription([FromBody] string? reason, CancellationToken ct)
    {
        var result = await _subscriptionService.CancelSubscriptionAsync(CurrentUserId, reason, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
