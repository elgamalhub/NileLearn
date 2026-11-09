using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using NileLearn.Application.Common.Interfaces;
using NileLearn.Application.Features.Admin;
using NileLearn.Application.Features.Auth;
using NileLearn.Application.Features.Billing;
using NileLearn.Application.Features.Courses;
using NileLearn.Application.Features.Learning;
using NileLearn.Application.Features.Payments;
using NileLearn.Application.Features.Video;
using NileLearn.Domain.Entities.Identity;
using NileLearn.Infrastructure.Persistence;
using NileLearn.Infrastructure.Services.Admin;
using NileLearn.Infrastructure.Services.Auth;
using NileLearn.Infrastructure.Services.Billing;
using NileLearn.Infrastructure.Services.Courses;
using NileLearn.Infrastructure.Services.Learning;
using NileLearn.Infrastructure.Services.Payments;
using NileLearn.Infrastructure.Services.Video;

namespace NileLearn.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. Database Configuration
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        var useSqlite = configuration.GetValue<bool>("UseSqlite") || string.IsNullOrEmpty(connectionString);

        if (useSqlite)
        {
            var sqliteDbPath = Path.Combine(Directory.GetCurrentDirectory(), "AppData", "nilelearn.db");
            var dir = Path.GetDirectoryName(sqliteDbPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlite($"Data Source={sqliteDbPath}"));
        }
        else
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString, b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));
        }

        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

        // 2. Identity Configuration
        services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = false;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequiredLength = 6;
            options.User.RequireUniqueEmail = true;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.Lockout.MaxFailedAccessAttempts = 5;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        // 3. JWT Authentication Configuration
        var secretKey = configuration["Jwt:SecretKey"] ?? "dev-only-change-this-secret-key-min-32-chars-long";
        var issuer = configuration["Jwt:Issuer"] ?? "NileLearnApi";
        var audience = configuration["Jwt:Audience"] ?? "NileLearnApp";

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                ValidateIssuer = true,
                ValidIssuer = issuer,
                ValidateAudience = true,
                ValidAudience = audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30)
            };
        });

        // 4. Register Services
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICourseService, CourseService>();
        services.AddScoped<IEntitlementService, EntitlementService>();
        services.AddScoped<ISubscriptionService, SubscriptionService>();
        services.AddScoped<IVideoUploadService, VideoUploadService>();
        services.AddScoped<IVideoPlaybackService, VideoPlaybackService>();
        services.AddScoped<ILearningService, LearningService>();
        services.AddScoped<IAdminService, AdminService>();

        // 5. Register Payment Gateways
        services.AddScoped<IPaymentGateway, SandboxPaymentGateway>();
        services.AddScoped<IPaymentGateway, PaymobPaymentGateway>();
        services.AddScoped<IPaymentGateway, FawryPaymentGateway>();
        services.AddScoped<IPaymentGateway, KashierPaymentGateway>();
        services.AddScoped<IPaymentGateway, StripePaymentGateway>();
        services.AddScoped<IPaymentService, PaymentService>();

        return services;
    }
}
