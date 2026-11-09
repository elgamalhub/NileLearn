using NileLearn.Domain.Common;
using NileLearn.Domain.Entities.Courses;
using NileLearn.Domain.Entities.Identity;
using NileLearn.Domain.Enums;

namespace NileLearn.Domain.Entities.Learning;

public class Enrollment : BaseEntity
{
    public Guid UserId { get; set; }
    public virtual ApplicationUser User { get; set; } = null!;

    public Guid CourseId { get; set; }
    public virtual Course Course { get; set; } = null!;

    public DateTime EnrolledAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }
    public int ProgressPercent { get; set; }
    public DateTime LastAccessedAtUtc { get; set; } = DateTime.UtcNow;
    public EnrollmentSource Source { get; set; } = EnrollmentSource.DirectPurchase;
}

public class LearningProgress : BaseEntity
{
    public Guid UserId { get; set; }
    public virtual ApplicationUser User { get; set; } = null!;

    public Guid CourseId { get; set; }
    public virtual Course Course { get; set; } = null!;

    public Guid LessonId { get; set; }
    public virtual Lesson Lesson { get; set; } = null!;

    public bool IsCompleted { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public int LastWatchedPositionSeconds { get; set; }
    public int TotalWatchSeconds { get; set; }
    public DateTime LastActiveAtUtc { get; set; } = DateTime.UtcNow;
}

public class LessonBookmark : BaseEntity
{
    public Guid UserId { get; set; }
    public virtual ApplicationUser User { get; set; } = null!;

    public Guid LessonId { get; set; }
    public virtual Lesson Lesson { get; set; } = null!;

    public int TimeOffsetSeconds { get; set; }
    public string Title { get; set; } = string.Empty;
}

public class LessonNote : BaseEntity
{
    public Guid UserId { get; set; }
    public virtual ApplicationUser User { get; set; } = null!;

    public Guid LessonId { get; set; }
    public virtual Lesson Lesson { get; set; } = null!;

    public int TimeOffsetSeconds { get; set; }
    public string Content { get; set; } = string.Empty;
}

public class Certificate : BaseEntity
{
    public string CertificateCode { get; set; } = string.Empty; // Unique public verification identifier
    public Guid UserId { get; set; }
    public virtual ApplicationUser User { get; set; } = null!;

    public Guid CourseId { get; set; }
    public virtual Course Course { get; set; } = null!;

    public string StudentFullName { get; set; } = string.Empty;
    public string CourseTitle { get; set; } = string.Empty;
    public double GradePercentage { get; set; } = 100.0;
    public DateTime IssuedAtUtc { get; set; } = DateTime.UtcNow;
    public string VerificationUrl { get; set; } = string.Empty;
    public string? PdfUrl { get; set; }
}
