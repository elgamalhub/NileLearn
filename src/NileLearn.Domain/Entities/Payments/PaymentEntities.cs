using NileLearn.Domain.Common;
using NileLearn.Domain.Entities.Billing;
using NileLearn.Domain.Entities.Courses;
using NileLearn.Domain.Entities.Identity;
using NileLearn.Domain.Enums;

namespace NileLearn.Domain.Entities.Payments;

public class Order : BaseEntity
{
    public string OrderNumber { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public virtual ApplicationUser User { get; set; } = null!;

    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "EGP";

    public Guid? CouponId { get; set; }
    public virtual Coupon? Coupon { get; set; }

    public DateTime? PaidAtUtc { get; set; }

    public virtual ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
}

public class OrderItem : BaseEntity
{
    public Guid OrderId { get; set; }
    public virtual Order Order { get; set; } = null!;

    public Guid? CourseId { get; set; }
    public virtual Course? Course { get; set; }

    public Guid? PlanId { get; set; }
    public virtual SubscriptionPlan? Plan { get; set; }

    public string ItemType { get; set; } = "CoursePurchase"; // "CoursePurchase", "SubscriptionPlan", "Bundle"
    public string Title { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; } = 1;
    public decimal TotalPrice { get; set; }
}

public class Payment : BaseEntity
{
    public Guid OrderId { get; set; }
    public virtual Order Order { get; set; } = null!;

    public Guid UserId { get; set; }
    public virtual ApplicationUser User { get; set; } = null!;

    public PaymentGatewayType GatewayType { get; set; }
    public PaymentMethodType PaymentMethod { get; set; } = PaymentMethodType.Card;
    public string? GatewayTransactionId { get; set; }
    public string? GatewayReferenceNumber { get; set; } // E.g. Fawry reference number (e.g. 912345678)
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "EGP";
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public string? ErrorMessage { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public string? RawWebhookPayload { get; set; }

    public DateTime? CompletedAtUtc { get; set; }
}

public class PaymentWebhook : BaseEntity
{
    public PaymentGatewayType GatewayType { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string TransactionReference { get; set; } = string.Empty;
    public string Signature { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = string.Empty;
    public bool IsProcessed { get; set; }
    public DateTime? ProcessedAtUtc { get; set; }
    public string? ErrorMessage { get; set; }
}

public class Invoice : BaseEntity
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public Guid OrderId { get; set; }
    public virtual Order Order { get; set; } = null!;

    public Guid UserId { get; set; }
    public virtual ApplicationUser User { get; set; } = null!;

    public decimal TotalAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public string Currency { get; set; } = "EGP";
    public DateTime IssuedAtUtc { get; set; } = DateTime.UtcNow;
    public string? PdfDownloadUrl { get; set; }
}

public class Coupon : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public decimal? DiscountPercent { get; set; }
    public decimal? FixedDiscountAmount { get; set; }
    public string Currency { get; set; } = "EGP";
    public DateTime ValidFromUtc { get; set; }
    public DateTime ValidToUtc { get; set; }
    public int MaxRedemptions { get; set; } = 100;
    public int TimesRedeemed { get; set; }
    public bool IsActive { get; set; } = true;
}
