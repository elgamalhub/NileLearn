using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NileLearn.Application.Common.Interfaces;
using NileLearn.Domain.Entities.Assessments;
using NileLearn.Domain.Entities.Billing;
using NileLearn.Domain.Entities.Courses;
using NileLearn.Domain.Entities.Identity;
using NileLearn.Domain.Entities.Learning;
using NileLearn.Domain.Entities.Payments;
using NileLearn.Domain.Entities.System;
using NileLearn.Domain.Entities.Video;

namespace NileLearn.Infrastructure.Persistence;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<UserSession> UserSessions => Set<UserSession>();
    public DbSet<InstructorProfile> InstructorProfiles => Set<InstructorProfile>();

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<CourseInstructor> CourseInstructors => Set<CourseInstructor>();
    public DbSet<CourseSection> CourseSections => Set<CourseSection>();
    public DbSet<Lesson> Lessons => Set<Lesson>();
    public DbSet<LessonResource> LessonResources => Set<LessonResource>();

    public DbSet<VideoAsset> VideoAssets => Set<VideoAsset>();
    public DbSet<VideoVariant> VideoVariants => Set<VideoVariant>();
    public DbSet<VideoUploadSession> VideoUploadSessions => Set<VideoUploadSession>();
    public DbSet<VideoPlaybackSession> VideoPlaybackSessions => Set<VideoPlaybackSession>();

    public DbSet<SubscriptionPlan> SubscriptionPlans => Set<SubscriptionPlan>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<SubscriptionEntitlement> SubscriptionEntitlements => Set<SubscriptionEntitlement>();

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PaymentWebhook> PaymentWebhooks => Set<PaymentWebhook>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<Coupon> Coupons => Set<Coupon>();

    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<LearningProgress> LearningProgress => Set<LearningProgress>();
    public DbSet<LessonBookmark> LessonBookmarks => Set<LessonBookmark>();
    public DbSet<LessonNote> LessonNotes => Set<LessonNote>();
    public DbSet<Certificate> Certificates => Set<Certificate>();

    public DbSet<Quiz> Quizzes => Set<Quiz>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<AnswerOption> AnswerOptions => Set<AnswerOption>();
    public DbSet<QuizAttempt> QuizAttempts => Set<QuizAttempt>();
    public DbSet<StudentAnswer> StudentAnswers => Set<StudentAnswer>();

    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<Discussion> Discussions => Set<Discussion>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<ApplicationSetting> ApplicationSettings => Set<ApplicationSetting>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Course Instructor Many-to-Many
        builder.Entity<CourseInstructor>()
            .HasKey(ci => new { ci.CourseId, ci.InstructorId });

        builder.Entity<CourseInstructor>()
            .HasOne(ci => ci.Course)
            .WithMany(c => c.Instructors)
            .HasForeignKey(ci => ci.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<CourseInstructor>()
            .HasOne(ci => ci.Instructor)
            .WithMany()
            .HasForeignKey(ci => ci.InstructorId)
            .OnDelete(DeleteBehavior.Restrict);

        // Decimal configurations
        builder.Entity<Course>(b =>
        {
            b.Property(c => c.Price).HasPrecision(18, 2);
            b.Property(c => c.DiscountPrice).HasPrecision(18, 2);
            b.HasIndex(c => c.Slug).IsUnique();
        });

        builder.Entity<Category>(b =>
        {
            b.HasIndex(c => c.Slug).IsUnique();
        });

        builder.Entity<SubscriptionPlan>(b =>
        {
            b.Property(p => p.MonthlyPriceEgp).HasPrecision(18, 2);
            b.Property(p => p.AnnualPriceEgp).HasPrecision(18, 2);
            b.HasIndex(p => p.Slug).IsUnique();
        });

        builder.Entity<Order>(b =>
        {
            b.Property(o => o.Subtotal).HasPrecision(18, 2);
            b.Property(o => o.DiscountAmount).HasPrecision(18, 2);
            b.Property(o => o.TaxAmount).HasPrecision(18, 2);
            b.Property(o => o.TotalAmount).HasPrecision(18, 2);
            b.HasIndex(o => o.OrderNumber).IsUnique();
        });

        builder.Entity<OrderItem>(b =>
        {
            b.Property(oi => oi.UnitPrice).HasPrecision(18, 2);
            b.Property(oi => oi.TotalPrice).HasPrecision(18, 2);
        });

        builder.Entity<Payment>(b =>
        {
            b.Property(p => p.Amount).HasPrecision(18, 2);
            b.HasIndex(p => p.IdempotencyKey);
            b.HasIndex(p => p.GatewayTransactionId);
        });

        builder.Entity<Invoice>(b =>
        {
            b.Property(i => i.TotalAmount).HasPrecision(18, 2);
            b.Property(i => i.TaxAmount).HasPrecision(18, 2);
            b.HasIndex(i => i.InvoiceNumber).IsUnique();
        });

        builder.Entity<Coupon>(b =>
        {
            b.Property(c => c.DiscountPercent).HasPrecision(5, 2);
            b.Property(c => c.FixedDiscountAmount).HasPrecision(18, 2);
            b.HasIndex(c => c.Code).IsUnique();
        });

        builder.Entity<InstructorProfile>(b =>
        {
            b.Property(ip => ip.RevenueSharePercentage).HasPrecision(5, 2);
        });

        builder.Entity<Certificate>(b =>
        {
            b.HasIndex(c => c.CertificateCode).IsUnique();
        });

        builder.Entity<ApplicationSetting>(b =>
        {
            b.HasIndex(s => s.Key).IsUnique();
        });

        // Global query filters for soft delete where applicable
        builder.Entity<Course>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<Lesson>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<Category>().HasQueryFilter(e => !e.IsDeleted);
    }
}
