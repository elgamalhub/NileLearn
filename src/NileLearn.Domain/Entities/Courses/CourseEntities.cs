using NileLearn.Domain.Common;
using NileLearn.Domain.Entities.Identity;
using NileLearn.Domain.Entities.Video;
using NileLearn.Domain.Enums;

namespace NileLearn.Domain.Entities.Courses;

public class Category : BaseEntity
{
    public string NameEn { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? DescriptionEn { get; set; }
    public string? DescriptionAr { get; set; }
    public string? IconUrl { get; set; }
    public Guid? ParentCategoryId { get; set; }
    public virtual Category? ParentCategory { get; set; }
    public virtual ICollection<Category> SubCategories { get; set; } = new List<Category>();
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public virtual ICollection<Course> Courses { get; set; } = new List<Course>();
}

public class Course : BaseEntity
{
    public string TitleEn { get; set; } = string.Empty;
    public string TitleAr { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string ShortDescriptionEn { get; set; } = string.Empty;
    public string ShortDescriptionAr { get; set; } = string.Empty;
    public string FullDescriptionEn { get; set; } = string.Empty;
    public string FullDescriptionAr { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public string? CoverImageUrl { get; set; }

    public Guid? PreviewVideoAssetId { get; set; }
    public virtual VideoAsset? PreviewVideoAsset { get; set; }

    public CourseLevel Level { get; set; } = CourseLevel.AllLevels;
    public CourseStatus Status { get; set; } = CourseStatus.Draft;
    public string PrimaryLanguage { get; set; } = "ar"; // "ar" or "en"

    public decimal Price { get; set; }
    public string Currency { get; set; } = "EGP";
    public decimal? DiscountPrice { get; set; }
    public DateTime? DiscountExpiresAtUtc { get; set; }
    public bool IsFree { get; set; }

    // Structured JSON metadata
    public string? PrerequisitesJson { get; set; } // JSON array of strings
    public string? LearningOutcomesJson { get; set; } // JSON array of strings
    public string? RequirementsJson { get; set; } // JSON array of strings
    public string? TargetAudienceJson { get; set; } // JSON array of strings

    public bool DripEnabled { get; set; }
    public int DripIntervalDays { get; set; } = 0;

    public Guid CategoryId { get; set; }
    public virtual Category Category { get; set; } = null!;

    public double AverageRating { get; set; }
    public int TotalReviews { get; set; }
    public int TotalEnrollments { get; set; }
    public int TotalDurationSeconds { get; set; }
    public int Version { get; set; } = 1;

    public DateTime? PublishedAtUtc { get; set; }

    public virtual ICollection<CourseInstructor> Instructors { get; set; } = new List<CourseInstructor>();
    public virtual ICollection<CourseSection> Sections { get; set; } = new List<CourseSection>();
}

public class CourseInstructor
{
    public Guid CourseId { get; set; }
    public virtual Course Course { get; set; } = null!;

    public Guid InstructorId { get; set; }
    public virtual ApplicationUser Instructor { get; set; } = null!;

    public bool IsPrimaryInstructor { get; set; } = true;
}

public class CourseSection : BaseEntity
{
    public Guid CourseId { get; set; }
    public virtual Course Course { get; set; } = null!;

    public string TitleEn { get; set; } = string.Empty;
    public string TitleAr { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsPublished { get; set; } = true;
    public int DripDelayDays { get; set; }

    public virtual ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();
}

public class Lesson : BaseEntity
{
    public Guid SectionId { get; set; }
    public virtual CourseSection Section { get; set; } = null!;

    public string TitleEn { get; set; } = string.Empty;
    public string TitleAr { get; set; } = string.Empty;
    public string? DescriptionEn { get; set; }
    public string? DescriptionAr { get; set; }

    public LessonType Type { get; set; } = LessonType.Video;
    public int SortOrder { get; set; }
    public int DurationSeconds { get; set; }
    public bool IsFreePreview { get; set; }
    public bool IsPublished { get; set; } = true;
    public int DripDelayDays { get; set; }

    public Guid? VideoAssetId { get; set; }
    public virtual VideoAsset? VideoAsset { get; set; }

    public string? ArticleContentHtmlEn { get; set; }
    public string? ArticleContentHtmlAr { get; set; }

    public virtual ICollection<LessonResource> Resources { get; set; } = new List<LessonResource>();
}

public class LessonResource : BaseEntity
{
    public Guid LessonId { get; set; }
    public virtual Lesson Lesson { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public string FileKey { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string MimeType { get; set; } = "application/pdf";
    public bool IsDownloadable { get; set; } = true;
}
