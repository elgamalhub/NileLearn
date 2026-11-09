using NileLearn.Application.Common.Models;
using NileLearn.Domain.Enums;

namespace NileLearn.Application.Features.Payments;

public class PaymentInitiationRequest
{
    public Guid OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "EGP";
    public Guid UserId { get; set; }
    public string UserEmail { get; set; } = string.Empty;
    public string UserFullName { get; set; } = string.Empty;
    public string? UserPhoneNumber { get; set; }
    public PaymentGatewayType GatewayType { get; set; }
    public PaymentMethodType PaymentMethod { get; set; } = PaymentMethodType.Card;
    public string ReturnUrl { get; set; } = string.Empty;
    public string CancelUrl { get; set; } = string.Empty;
}

public class PaymentInitiationResult
{
    public bool Success { get; set; }
    public string? TransactionId { get; set; }
    public string? ReferenceNumber { get; set; } // E.g. Fawry reference number (e.g. 912345678)
    public string? RedirectUrl { get; set; }
    public string? ClientSecret { get; set; } // E.g. Stripe client secret or Paymob token
    public string? ErrorMessage { get; set; }
    public IDictionary<string, string> AdditionalData { get; set; } = new Dictionary<string, string>();
}

public class PaymentVerificationResult
{
    public bool Success { get; set; }
    public bool IsPaid { get; set; }
    public string? GatewayTransactionId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "EGP";
    public string? ErrorMessage { get; set; }
}

public class WebhookProcessingResult
{
    public bool IsValid { get; set; }
    public string? TransactionReference { get; set; }
    public string? GatewayTransactionId { get; set; }
    public PaymentStatus Status { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "EGP";
    public string? ErrorMessage { get; set; }
    public string EventType { get; set; } = string.Empty;
}

public class RefundRequest
{
    public string TransactionId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "EGP";
    public string Reason { get; set; } = string.Empty;
}

public class RefundResult
{
    public bool Success { get; set; }
    public string? RefundTransactionId { get; set; }
    public string? ErrorMessage { get; set; }
}

public interface IPaymentGateway
{
    PaymentGatewayType GatewayType { get; }
    string ProviderName { get; }
    Task<PaymentInitiationResult> InitiatePaymentAsync(PaymentInitiationRequest request, CancellationToken ct = default);
    Task<PaymentVerificationResult> VerifyPaymentAsync(string transactionReference, CancellationToken ct = default);
    Task<WebhookProcessingResult> ProcessWebhookAsync(string rawBody, IDictionary<string, string> headers, CancellationToken ct = default);
    Task<RefundResult> ProcessRefundAsync(RefundRequest request, CancellationToken ct = default);
}

public class CheckoutCourseRequest
{
    public Guid CourseId { get; set; }
    public PaymentGatewayType GatewayType { get; set; } = PaymentGatewayType.Sandbox;
    public PaymentMethodType PaymentMethod { get; set; } = PaymentMethodType.Card;
    public string? CouponCode { get; set; }
}

public class CheckoutSubscriptionRequest
{
    public Guid PlanId { get; set; }
    public BillingCycle BillingCycle { get; set; } = BillingCycle.Monthly;
    public PaymentGatewayType GatewayType { get; set; } = PaymentGatewayType.Sandbox;
    public PaymentMethodType PaymentMethod { get; set; } = PaymentMethodType.Card;
    public string? CouponCode { get; set; }
}

public class CheckoutResponseDto
{
    public Guid OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "EGP";
    public string? RedirectUrl { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? ClientSecret { get; set; }
    public PaymentStatus Status { get; set; }
}

public class OrderInvoiceDto
{
    public Guid OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string InvoiceNumber { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "EGP";
    public DateTime IssuedAtUtc { get; set; }
    public string Status { get; set; } = string.Empty;
    public string ItemTitle { get; set; } = string.Empty;
}

public interface IPaymentService
{
    Task<ApiResponse<CheckoutResponseDto>> CheckoutCourseAsync(Guid userId, CheckoutCourseRequest request, CancellationToken ct = default);
    Task<ApiResponse<CheckoutResponseDto>> CheckoutSubscriptionAsync(Guid userId, CheckoutSubscriptionRequest request, CancellationToken ct = default);
    Task<ApiResponse<bool>> ProcessIncomingWebhookAsync(PaymentGatewayType gatewayType, string rawBody, IDictionary<string, string> headers, CancellationToken ct = default);
    Task<ApiResponse<PaymentVerificationResult>> CheckPaymentStatusAsync(Guid orderId, CancellationToken ct = default);
    Task<ApiResponse<IReadOnlyList<OrderInvoiceDto>>> GetUserInvoicesAsync(Guid userId, CancellationToken ct = default);
}
