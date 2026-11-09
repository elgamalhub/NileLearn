using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using NileLearn.Application.Common.Interfaces;
using NileLearn.Application.Features.Payments;
using NileLearn.Domain.Entities.Billing;
using NileLearn.Domain.Entities.Courses;
using NileLearn.Domain.Entities.Learning;
using NileLearn.Domain.Enums;
using NileLearn.Infrastructure.Persistence;
using NileLearn.Infrastructure.Services.Auth;
using NileLearn.Infrastructure.Services.Billing;
using NileLearn.Infrastructure.Services.Payments;
using Xunit;

namespace NileLearn.UnitTests;

public class EntitlementServiceTests
{
    private readonly ApplicationDbContext _context;
    private readonly EntitlementService _entitlementService;

    public EntitlementServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _entitlementService = new EntitlementService(_context);
    }

    [Fact]
    public async Task HasAccessToCourse_WhenUserIsEnrolled_ReturnsTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var courseId = Guid.NewGuid();

        _context.Enrollments.Add(new Enrollment
        {
            UserId = userId,
            CourseId = courseId,
            Source = EnrollmentSource.DirectPurchase
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _entitlementService.HasAccessToCourseAsync(userId, courseId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasAccessToCourse_WhenCourseIsFree_ReturnsTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var course = new Course
        {
            TitleEn = "Free Introduction",
            TitleAr = "مقدمة مجانية",
            Slug = "free-intro",
            IsFree = true
        };
        _context.Courses.Add(course);
        await _context.SaveChangesAsync();

        // Act
        var result = await _entitlementService.HasAccessToCourseAsync(userId, course.Id);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasAccessToCourse_WhenUserHasActiveSubscription_ReturnsTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var category = new Category { NameEn = "Engineering", NameAr = "هندسة", Slug = "engineering" };
        var course = new Course
        {
            TitleEn = "Advanced .NET",
            TitleAr = "دوت نت متقدم",
            Slug = "adv-net",
            CategoryId = category.Id,
            Category = category,
            IsFree = false
        };
        _context.Courses.Add(course);

        var plan = new SubscriptionPlan
        {
            NameEn = "Pro Plan",
            NameAr = "باقة برو",
            Slug = "pro"
        };
        plan.Entitlements.Add(new SubscriptionEntitlement { Plan = plan, AppliesToAllCourses = true });

        var subscription = new Subscription
        {
            UserId = userId,
            Plan = plan,
            Status = SubscriptionStatus.Active,
            CurrentPeriodStartUtc = DateTime.UtcNow.AddDays(-5),
            CurrentPeriodEndUtc = DateTime.UtcNow.AddDays(25)
        };
        _context.Subscriptions.Add(subscription);
        await _context.SaveChangesAsync();

        // Act
        var result = await _entitlementService.HasAccessToCourseAsync(userId, course.Id);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasAccessToCourse_WhenNotEnrolledAndNoSubscription_ReturnsFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var course = new Course
        {
            TitleEn = "Paid Course",
            TitleAr = "دورة مدفوعة",
            Slug = "paid-course",
            IsFree = false
        };
        _context.Courses.Add(course);
        await _context.SaveChangesAsync();

        // Act
        var result = await _entitlementService.HasAccessToCourseAsync(userId, course.Id);

        // Assert
        result.Should().BeFalse();
    }
}

public class PaymentGatewaysTests
{
    [Fact]
    public async Task FawryPaymentGateway_GeneratesValidReferenceNumber()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string?>
        {
            {"PaymentGateways:Fawry:SecurityKey", "test_key"}
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();
        var gateway = new FawryPaymentGateway(config);

        var request = new PaymentInitiationRequest
        {
            OrderId = Guid.NewGuid(),
            OrderNumber = "ORD-TEST-100",
            Amount = 450.00m,
            Currency = "EGP",
            PaymentMethod = PaymentMethodType.FawryKioskCash
        };

        // Act
        var result = await gateway.InitiatePaymentAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.ReferenceNumber.Should().NotBeNullOrEmpty();
        result.ReferenceNumber!.Length.Should().Be(9);
        result.AdditionalData.Should().ContainKey("FawryReferenceNumber");
    }

    [Fact]
    public async Task PaymobPaymentGateway_InitiatesPayment_ReturnsClientSecretAndRedirect()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string?>
        {
            {"PaymentGateways:Paymob:ApiKey", "test_api_key"}
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();
        var gateway = new PaymobPaymentGateway(config);

        var request = new PaymentInitiationRequest
        {
            OrderId = Guid.NewGuid(),
            OrderNumber = "ORD-PMOB-200",
            Amount = 799.00m,
            Currency = "EGP",
            PaymentMethod = PaymentMethodType.Card
        };

        // Act
        var result = await gateway.InitiatePaymentAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.TransactionId.Should().StartWith("PMOB-");
        result.ClientSecret.Should().StartWith("pmob_sec_");
        result.RedirectUrl.Should().Contain("accept.paymob.com");
    }
}

public class JwtTokenGeneratorTests
{
    [Fact]
    public void GenerateAccessToken_ProducesValidJwt()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string?>
        {
            {"Jwt:SecretKey", "Very_Long_Super_Secret_Key_For_Unit_Testing_At_Least_32_Bytes_Long!"},
            {"Jwt:Issuer", "TestIssuer"},
            {"Jwt:Audience", "TestAudience"},
            {"Jwt:ExpiryMinutes", "60"}
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();
        var generator = new JwtTokenGenerator(config);

        var userId = Guid.NewGuid();
        var email = "test@nilelearn.com";
        var name = "Test User";
        var roles = new[] { "Student" };

        // Act
        var token = generator.GenerateAccessToken(userId, email, name, roles, out var expiresAtUtc);

        // Assert
        token.Should().NotBeNullOrWhiteSpace();
        token.Split('.').Should().HaveCount(3); // Header.Payload.Signature
        expiresAtUtc.Should().BeAfter(DateTime.UtcNow);
    }
}
