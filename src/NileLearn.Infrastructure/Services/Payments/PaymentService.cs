using Microsoft.EntityFrameworkCore;
using NileLearn.Application.Common.Interfaces;
using NileLearn.Application.Common.Models;
using NileLearn.Application.Features.Payments;
using NileLearn.Domain.Entities.Billing;
using NileLearn.Domain.Entities.Courses;
using NileLearn.Domain.Entities.Identity;
using NileLearn.Domain.Entities.Learning;
using NileLearn.Domain.Entities.Payments;
using NileLearn.Domain.Enums;

namespace NileLearn.Infrastructure.Services.Payments;

public class PaymentService : IPaymentService
{
    private readonly IApplicationDbContext _context;
    private readonly IEnumerable<IPaymentGateway> _gateways;

    public PaymentService(IApplicationDbContext context, IEnumerable<IPaymentGateway> gateways)
    {
        _context = context;
        _gateways = gateways;
    }

    public async Task<ApiResponse<CheckoutResponseDto>> CheckoutCourseAsync(Guid userId, CheckoutCourseRequest request, CancellationToken ct = default)
    {
        var course = await _context.Courses.FirstOrDefaultAsync(c => c.Id == request.CourseId, ct);
        if (course == null) return ApiResponse<CheckoutResponseDto>.Fail("Course not found.", "الدورة غير موجودة.");

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user == null) return ApiResponse<CheckoutResponseDto>.Fail("User not found.", "المستخدم غير موجود.");

        // Check if already enrolled
        var alreadyEnrolled = await _context.Enrollments.AnyAsync(e => e.UserId == userId && e.CourseId == request.CourseId, ct);
        if (alreadyEnrolled)
        {
            return ApiResponse<CheckoutResponseDto>.Fail("You are already enrolled in this course.", "أنت مشترك في هذه الدورة بالفعل.");
        }

        var basePrice = course.DiscountPrice ?? course.Price;
        decimal discount = 0m;
        Guid? couponId = null;

        if (!string.IsNullOrWhiteSpace(request.CouponCode))
        {
            var coupon = await _context.Coupons.FirstOrDefaultAsync(c => c.Code.ToUpper() == request.CouponCode.Trim().ToUpper() && c.IsActive && c.ValidToUtc >= DateTime.UtcNow, ct);
            if (coupon != null && coupon.TimesRedeemed < coupon.MaxRedemptions)
            {
                couponId = coupon.Id;
                if (coupon.DiscountPercent.HasValue)
                {
                    discount = Math.Round(basePrice * (coupon.DiscountPercent.Value / 100m), 2);
                }
                else if (coupon.FixedDiscountAmount.HasValue)
                {
                    discount = Math.Min(basePrice, coupon.FixedDiscountAmount.Value);
                }
                coupon.TimesRedeemed++;
            }
        }

        var totalAmount = Math.Max(0m, basePrice - discount);
        var orderNumber = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}"[..20].ToUpper();

        var order = new Order
        {
            OrderNumber = orderNumber,
            UserId = userId,
            Subtotal = basePrice,
            DiscountAmount = discount,
            TaxAmount = 0m,
            TotalAmount = totalAmount,
            Currency = course.Currency,
            CouponId = couponId,
            Status = totalAmount == 0m ? OrderStatus.Paid : OrderStatus.Pending,
            PaidAtUtc = totalAmount == 0m ? DateTime.UtcNow : null
        };

        order.Items.Add(new OrderItem
        {
            OrderId = order.Id,
            CourseId = course.Id,
            ItemType = "CoursePurchase",
            Title = course.TitleEn,
            UnitPrice = basePrice,
            Quantity = 1,
            TotalPrice = totalAmount
        });

        _context.Orders.Add(order);

        // If completely free course or 100% coupon:
        if (totalAmount == 0m)
        {
            await EnrollUserInCourseAsync(userId, course.Id, EnrollmentSource.FreeCourse, ct);
            await GenerateInvoiceForOrderAsync(order, ct);
            await _context.SaveChangesAsync(ct);

            return ApiResponse<CheckoutResponseDto>.Ok(new CheckoutResponseDto
            {
                OrderId = order.Id,
                OrderNumber = orderNumber,
                Amount = 0m,
                Currency = course.Currency,
                Status = PaymentStatus.Paid
            }, "Enrolled in course successfully.", "تم الاشتراك في الدورة بنجاح.");
        }

        // Initialize payment with selected gateway
        var gateway = GetGateway(request.GatewayType);
        var initResult = await gateway.InitiatePaymentAsync(new PaymentInitiationRequest
        {
            OrderId = order.Id,
            OrderNumber = orderNumber,
            Amount = totalAmount,
            Currency = course.Currency,
            UserId = userId,
            UserEmail = user.Email ?? "",
            UserFullName = user.FullName,
            UserPhoneNumber = user.PhoneNumber,
            GatewayType = request.GatewayType,
            PaymentMethod = request.PaymentMethod
        }, ct);

        var payment = new Payment
        {
            OrderId = order.Id,
            UserId = userId,
            GatewayType = request.GatewayType,
            PaymentMethod = request.PaymentMethod,
            GatewayTransactionId = initResult.TransactionId,
            GatewayReferenceNumber = initResult.ReferenceNumber,
            Amount = totalAmount,
            Currency = course.Currency,
            Status = PaymentStatus.Pending,
            IdempotencyKey = Guid.NewGuid().ToString("N")
        };

        _context.Payments.Add(payment);
        await _context.SaveChangesAsync(ct);

        return ApiResponse<CheckoutResponseDto>.Ok(new CheckoutResponseDto
        {
            OrderId = order.Id,
            OrderNumber = orderNumber,
            Amount = totalAmount,
            Currency = course.Currency,
            RedirectUrl = initResult.RedirectUrl,
            ReferenceNumber = initResult.ReferenceNumber,
            ClientSecret = initResult.ClientSecret,
            Status = PaymentStatus.Pending
        });
    }

    public async Task<ApiResponse<CheckoutResponseDto>> CheckoutSubscriptionAsync(Guid userId, CheckoutSubscriptionRequest request, CancellationToken ct = default)
    {
        var plan = await _context.SubscriptionPlans.FirstOrDefaultAsync(p => p.Id == request.PlanId, ct);
        if (plan == null) return ApiResponse<CheckoutResponseDto>.Fail("Subscription plan not found.", "خطة الاشتراك غير موجودة.");

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user == null) return ApiResponse<CheckoutResponseDto>.Fail("User not found.", "المستخدم غير موجود.");

        var price = request.BillingCycle == BillingCycle.Annual ? plan.AnnualPriceEgp : plan.MonthlyPriceEgp;
        var orderNumber = $"SUB-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}"[..20].ToUpper();

        var order = new Order
        {
            OrderNumber = orderNumber,
            UserId = userId,
            Subtotal = price,
            DiscountAmount = 0m,
            TaxAmount = 0m,
            TotalAmount = price,
            Currency = plan.Currency,
            Status = OrderStatus.Pending
        };

        order.Items.Add(new OrderItem
        {
            OrderId = order.Id,
            PlanId = plan.Id,
            ItemType = "SubscriptionPlan",
            Title = plan.NameEn,
            UnitPrice = price,
            Quantity = 1,
            TotalPrice = price
        });

        _context.Orders.Add(order);

        var gateway = GetGateway(request.GatewayType);
        var initResult = await gateway.InitiatePaymentAsync(new PaymentInitiationRequest
        {
            OrderId = order.Id,
            OrderNumber = orderNumber,
            Amount = price,
            Currency = plan.Currency,
            UserId = userId,
            UserEmail = user.Email ?? "",
            UserFullName = user.FullName,
            UserPhoneNumber = user.PhoneNumber,
            GatewayType = request.GatewayType,
            PaymentMethod = request.PaymentMethod
        }, ct);

        var payment = new Payment
        {
            OrderId = order.Id,
            UserId = userId,
            GatewayType = request.GatewayType,
            PaymentMethod = request.PaymentMethod,
            GatewayTransactionId = initResult.TransactionId,
            GatewayReferenceNumber = initResult.ReferenceNumber,
            Amount = price,
            Currency = plan.Currency,
            Status = PaymentStatus.Pending,
            IdempotencyKey = Guid.NewGuid().ToString("N")
        };

        _context.Payments.Add(payment);
        await _context.SaveChangesAsync(ct);

        return ApiResponse<CheckoutResponseDto>.Ok(new CheckoutResponseDto
        {
            OrderId = order.Id,
            OrderNumber = orderNumber,
            Amount = price,
            Currency = plan.Currency,
            RedirectUrl = initResult.RedirectUrl,
            ReferenceNumber = initResult.ReferenceNumber,
            ClientSecret = initResult.ClientSecret,
            Status = PaymentStatus.Pending
        });
    }

    public async Task<ApiResponse<bool>> ProcessIncomingWebhookAsync(PaymentGatewayType gatewayType, string rawBody, IDictionary<string, string> headers, CancellationToken ct = default)
    {
        var gateway = GetGateway(gatewayType);
        var webhookResult = await gateway.ProcessWebhookAsync(rawBody, headers, ct);

        if (!webhookResult.IsValid)
        {
            return ApiResponse<bool>.Fail("Invalid webhook signature or payload.", "توقيع الويب هوك غير صالح.");
        }

        // Idempotency check: prevent processing the same webhook twice
        var existingWebhook = await _context.PaymentWebhooks
            .AnyAsync(w => w.GatewayType == gatewayType && w.TransactionReference == webhookResult.TransactionReference && w.IsProcessed, ct);

        if (existingWebhook)
        {
            return ApiResponse<bool>.Ok(true, "Webhook already processed (Idempotent).", "تم معالجة الإشعار مسبقاً.");
        }

        var webhookLog = new PaymentWebhook
        {
            GatewayType = gatewayType,
            EventName = webhookResult.EventType,
            TransactionReference = webhookResult.TransactionReference ?? Guid.NewGuid().ToString("N"),
            Signature = headers.TryGetValue("signature", out var s) ? s : "",
            PayloadJson = rawBody,
            IsProcessed = true,
            ProcessedAtUtc = DateTime.UtcNow
        };
        _context.PaymentWebhooks.Add(webhookLog);

        if (webhookResult.Status == PaymentStatus.Paid)
        {
            // Find payment by gateway transaction reference
            var payment = await _context.Payments
                .Include(p => p.Order).ThenInclude(o => o.Items)
                .FirstOrDefaultAsync(p => p.GatewayTransactionId == webhookResult.GatewayTransactionId ||
                                          p.Order.OrderNumber == webhookResult.TransactionReference, ct);

            if (payment != null && payment.Status != PaymentStatus.Paid)
            {
                payment.Status = PaymentStatus.Paid;
                payment.CompletedAtUtc = DateTime.UtcNow;
                payment.Order.Status = OrderStatus.Paid;
                payment.Order.PaidAtUtc = DateTime.UtcNow;

                // Grant fulfillment
                foreach (var item in payment.Order.Items)
                {
                    if (item.CourseId.HasValue)
                    {
                        await EnrollUserInCourseAsync(payment.UserId, item.CourseId.Value, EnrollmentSource.DirectPurchase, ct);
                    }
                    else if (item.PlanId.HasValue)
                    {
                        await ActivateSubscriptionForUserAsync(payment.UserId, item.PlanId.Value, payment.Id, ct);
                    }
                }

                await GenerateInvoiceForOrderAsync(payment.Order, ct);
            }
        }

        await _context.SaveChangesAsync(ct);
        return ApiResponse<bool>.Ok(true, "Webhook processed successfully.", "تمت معالجة الإشعار بنجاح.");
    }

    public async Task<ApiResponse<PaymentVerificationResult>> CheckPaymentStatusAsync(Guid orderId, CancellationToken ct = default)
    {
        var order = await _context.Orders
            .Include(o => o.Payments)
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);

        if (order == null) return ApiResponse<PaymentVerificationResult>.Fail("Order not found.", "الطلب غير موجود.");

        var payment = order.Payments.OrderByDescending(p => p.CreatedAtUtc).FirstOrDefault();
        var isPaid = order.Status == OrderStatus.Paid || (payment != null && payment.Status == PaymentStatus.Paid);

        return ApiResponse<PaymentVerificationResult>.Ok(new PaymentVerificationResult
        {
            Success = true,
            IsPaid = isPaid,
            GatewayTransactionId = payment?.GatewayTransactionId,
            Amount = order.TotalAmount,
            Currency = order.Currency
        });
    }

    public async Task<ApiResponse<IReadOnlyList<OrderInvoiceDto>>> GetUserInvoicesAsync(Guid userId, CancellationToken ct = default)
    {
        var invoices = await _context.Invoices
            .Include(i => i.Order).ThenInclude(o => o.Items)
            .Where(i => i.UserId == userId)
            .OrderByDescending(i => i.IssuedAtUtc)
            .Select(i => new OrderInvoiceDto
            {
                OrderId = i.OrderId,
                OrderNumber = i.Order.OrderNumber,
                InvoiceNumber = i.InvoiceNumber,
                TotalAmount = i.TotalAmount,
                Currency = i.Currency,
                IssuedAtUtc = i.IssuedAtUtc,
                Status = i.Order.Status.ToString(),
                ItemTitle = i.Order.Items.FirstOrDefault() != null ? i.Order.Items.First().Title : "LMS Service"
            })
            .ToListAsync(ct);

        return ApiResponse<IReadOnlyList<OrderInvoiceDto>>.Ok(invoices);
    }

    private async Task EnrollUserInCourseAsync(Guid userId, Guid courseId, EnrollmentSource source, CancellationToken ct)
    {
        var existing = await _context.Enrollments.AnyAsync(e => e.UserId == userId && e.CourseId == courseId, ct);
        if (!existing)
        {
            var enrollment = new Enrollment
            {
                UserId = userId,
                CourseId = courseId,
                EnrolledAtUtc = DateTime.UtcNow,
                Source = source
            };
            _context.Enrollments.Add(enrollment);

            var course = await _context.Courses.FindAsync(new object[] { courseId }, ct);
            if (course != null)
            {
                course.TotalEnrollments++;
            }
        }
    }

    private async Task ActivateSubscriptionForUserAsync(Guid userId, Guid planId, Guid paymentId, CancellationToken ct)
    {
        var plan = await _context.SubscriptionPlans.FindAsync(new object[] { planId }, ct);
        if (plan == null) return;

        var existingSub = await _context.Subscriptions
            .FirstOrDefaultAsync(s => s.UserId == userId && s.Status == SubscriptionStatus.Active, ct);

        if (existingSub != null)
        {
            existingSub.PlanId = planId;
            existingSub.CurrentPeriodEndUtc = DateTime.UtcNow.AddMonths(1);
            existingSub.LastPaymentId = paymentId;
        }
        else
        {
            var newSub = new Subscription
            {
                UserId = userId,
                PlanId = planId,
                Status = SubscriptionStatus.Active,
                CurrentPeriodStartUtc = DateTime.UtcNow,
                CurrentPeriodEndUtc = DateTime.UtcNow.AddMonths(1),
                LastPaymentId = paymentId
            };
            _context.Subscriptions.Add(newSub);
        }
    }

    private Task GenerateInvoiceForOrderAsync(Order order, CancellationToken ct)
    {
        var invoiceNumber = $"INV-{DateTime.UtcNow:yyyyMM}-{Guid.NewGuid():N}"[..18].ToUpper();
        var invoice = new Invoice
        {
            OrderId = order.Id,
            UserId = order.UserId,
            InvoiceNumber = invoiceNumber,
            TotalAmount = order.TotalAmount,
            TaxAmount = order.TaxAmount,
            Currency = order.Currency,
            IssuedAtUtc = DateTime.UtcNow,
            PdfDownloadUrl = $"/api/v1/invoices/{order.Id}/download"
        };
        _context.Invoices.Add(invoice);
        return Task.CompletedTask;
    }

    private IPaymentGateway GetGateway(PaymentGatewayType type)
    {
        var gateway = _gateways.FirstOrDefault(g => g.GatewayType == type);
        return gateway ?? _gateways.First(g => g.GatewayType == PaymentGatewayType.Sandbox);
    }
}
