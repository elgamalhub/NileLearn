using Microsoft.EntityFrameworkCore;
using NileLearn.Domain.Entities.Assessments;
using NileLearn.Domain.Entities.Billing;
using NileLearn.Domain.Entities.Courses;
using NileLearn.Domain.Entities.Identity;
using NileLearn.Domain.Entities.Learning;
using NileLearn.Domain.Entities.Payments;
using NileLearn.Domain.Entities.System;
using NileLearn.Domain.Entities.Video;

namespace NileLearn.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<ApplicationUser> Users { get; }
    DbSet<ApplicationRole> Roles { get; }
    DbSet<UserSession> UserSessions { get; }
    DbSet<InstructorProfile> InstructorProfiles { get; }

    DbSet<Category> Categories { get; }
    DbSet<Course> Courses { get; }
    DbSet<CourseInstructor> CourseInstructors { get; }
    DbSet<CourseSection> CourseSections { get; }
    DbSet<Lesson> Lessons { get; }
    DbSet<LessonResource> LessonResources { get; }

    DbSet<VideoAsset> VideoAssets { get; }
    DbSet<VideoVariant> VideoVariants { get; }
    DbSet<VideoUploadSession> VideoUploadSessions { get; }
    DbSet<VideoPlaybackSession> VideoPlaybackSessions { get; }

    DbSet<SubscriptionPlan> SubscriptionPlans { get; }
    DbSet<Subscription> Subscriptions { get; }
    DbSet<SubscriptionEntitlement> SubscriptionEntitlements { get; }

    DbSet<Order> Orders { get; }
    DbSet<OrderItem> OrderItems { get; }
    DbSet<Payment> Payments { get; }
    DbSet<PaymentWebhook> PaymentWebhooks { get; }
    DbSet<Invoice> Invoices { get; }
    DbSet<Coupon> Coupons { get; }

    DbSet<Enrollment> Enrollments { get; }
    DbSet<LearningProgress> LearningProgress { get; }
    DbSet<LessonBookmark> LessonBookmarks { get; }
    DbSet<LessonNote> LessonNotes { get; }
    DbSet<Certificate> Certificates { get; }

    DbSet<Quiz> Quizzes { get; }
    DbSet<Question> Questions { get; }
    DbSet<AnswerOption> AnswerOptions { get; }
    DbSet<QuizAttempt> QuizAttempts { get; }
    DbSet<StudentAnswer> StudentAnswers { get; }

    DbSet<Review> Reviews { get; }
    DbSet<Discussion> Discussions { get; }
    DbSet<Notification> Notifications { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<ApplicationSetting> ApplicationSettings { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
