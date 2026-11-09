using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NileLearn.Application.Common.Models;
using NileLearn.Application.Features.Payments;
using NileLearn.Domain.Enums;

namespace NileLearn.Api.Controllers;

public class PaymentsController : BaseApiController
{
    private readonly IPaymentService _paymentService;

    public PaymentsController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    [Authorize]
    [HttpPost("checkout/course")]
    public async Task<ActionResult<ApiResponse<CheckoutResponseDto>>> CheckoutCourse([FromBody] CheckoutCourseRequest request, CancellationToken ct)
    {
        var result = await _paymentService.CheckoutCourseAsync(CurrentUserId, request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [Authorize]
    [HttpPost("checkout/subscription")]
    public async Task<ActionResult<ApiResponse<CheckoutResponseDto>>> CheckoutSubscription([FromBody] CheckoutSubscriptionRequest request, CancellationToken ct)
    {
        var result = await _paymentService.CheckoutSubscriptionAsync(CurrentUserId, request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("webhook/{gatewayType}")]
    public async Task<IActionResult> HandleWebhook(string gatewayType, CancellationToken ct)
    {
        using var reader = new StreamReader(Request.Body);
        var rawBody = await reader.ReadToEndAsync(ct);

        var headers = Request.Headers.ToDictionary(h => h.Key.ToLower(), h => h.Value.ToString());

        if (!Enum.TryParse<PaymentGatewayType>(gatewayType, true, out var parsedGateway))
        {
            parsedGateway = PaymentGatewayType.Sandbox;
        }

        var result = await _paymentService.ProcessIncomingWebhookAsync(parsedGateway, rawBody, headers, ct);
        return result.Success ? Ok(new { status = "success" }) : BadRequest(new { status = "failed", message = result.Message });
    }

    [Authorize]
    [HttpGet("status/{orderId:guid}")]
    public async Task<ActionResult<ApiResponse<PaymentVerificationResult>>> GetPaymentStatus(Guid orderId, CancellationToken ct)
    {
        var result = await _paymentService.CheckPaymentStatusAsync(orderId, ct);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [Authorize]
    [HttpGet("invoices")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<OrderInvoiceDto>>>> GetMyInvoices(CancellationToken ct)
    {
        var result = await _paymentService.GetUserInvoicesAsync(CurrentUserId, ct);
        return Ok(result);
    }
}
