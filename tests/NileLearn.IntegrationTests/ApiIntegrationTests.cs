using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NileLearn.Application.Common.Models;
using NileLearn.Application.Features.Auth;
using NileLearn.Application.Features.Billing;
using NileLearn.Application.Features.Courses;
using NileLearn.Application.Features.Payments;
using NileLearn.Application.Features.Learning;
using NileLearn.Domain.Enums;
using Xunit;

namespace NileLearn.IntegrationTests;

public class ApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task HealthEndpoint_ReturnsHealthyStatus()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Healthy");
        content.Should().Contain("NileLearn LMS 2026");
    }

    [Fact]
    public async Task GetCategories_ReturnsSeededCategories()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/courses/categories");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<CategoryDto>>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeEmpty();
        result.Data!.Any(c => c.Slug == "backend-cloud").Should().BeTrue();
    }

    [Fact]
    public async Task GetSubscriptionPlans_ReturnsSeededPlansWithEgpCurrency()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/subscriptions/plans");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<SubscriptionPlanDto>>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeEmpty();
        result.Data!.Should().Contain(p => p.Currency == "EGP" && p.MonthlyPriceEgp == 299.00m);
    }

    [Fact]
    public async Task RegisterLoginAndCheckout_Flow_SucceedsEndToEnd()
    {
        // 1. Register a new student
        var testEmail = $"student_{Guid.NewGuid():N}@nilelearn.com";
        var registerRequest = new RegisterRequest
        {
            FullName = "Tariq Test Student",
            Email = testEmail,
            Password = "TestPassword123!",
            Role = UserRoleType.Student,
            PreferredLanguage = "ar"
        };

        var regResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);
        regResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var regResult = await regResponse.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        regResult.Should().NotBeNull();
        regResult!.Success.Should().BeTrue();
        regResult.Data!.AccessToken.Should().NotBeNullOrEmpty();

        var token = regResult.Data.AccessToken;
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // 2. Fetch courses list to get a course ID
        var coursesResponse = await _client.GetAsync("/api/v1/courses");
        coursesResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var coursesResult = await coursesResponse.Content.ReadFromJsonAsync<ApiResponse<PagedResult<CourseSummaryDto>>>();
        coursesResult!.Data!.Items.Should().NotBeEmpty();

        var targetCourse = coursesResult.Data.Items.First();

        // 3. Checkout course with Sandbox simulator
        var checkoutRequest = new CheckoutCourseRequest
        {
            CourseId = targetCourse.Id,
            GatewayType = PaymentGatewayType.Sandbox,
            PaymentMethod = PaymentMethodType.Card,
            CouponCode = "EGYPT2026" // Apply 25% discount coupon!
        };

        var checkoutResponse = await _client.PostAsJsonAsync("/api/v1/payments/checkout/course", checkoutRequest);
        checkoutResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var checkoutResult = await checkoutResponse.Content.ReadFromJsonAsync<ApiResponse<CheckoutResponseDto>>();
        checkoutResult.Should().NotBeNull();
        checkoutResult!.Success.Should().BeTrue();
        checkoutResult.Data!.Amount.Should().BeLessThan(targetCourse.Price); // Discount applied!

        // 4. Trigger simulated successful webhook for this order
        var webhookPayload = JsonSerializer.Serialize(new
        {
            orderId = checkoutResult.Data.OrderId,
            merchantRefNumber = checkoutResult.Data.OrderNumber,
            orderStatus = "PAID",
            paymentAmount = checkoutResult.Data.Amount
        });

        var webhookReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/payments/webhook/sandbox")
        {
            Content = new StringContent(webhookPayload, System.Text.Encoding.UTF8, "application/json")
        };
        webhookReq.Headers.Add("signature", "valid_simulated_sig");

        var webhookResponse = await _client.SendAsync(webhookReq);
        webhookResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // 5. Verify payment status
        var statusResponse = await _client.GetAsync($"/api/v1/payments/status/{checkoutResult.Data.OrderId}");
        statusResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var statusResult = await statusResponse.Content.ReadFromJsonAsync<ApiResponse<PaymentVerificationResult>>();
        statusResult!.Data!.IsPaid.Should().BeTrue();

        // 6. Verify enrolled courses for this student
        var myCoursesResponse = await _client.GetAsync("/api/v1/learning/my-courses");
        myCoursesResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var myCoursesResult = await myCoursesResponse.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<StudentCourseProgressDto>>>();
        myCoursesResult!.Data.Should().Contain(c => c.CourseId == targetCourse.Id);
    }
}
