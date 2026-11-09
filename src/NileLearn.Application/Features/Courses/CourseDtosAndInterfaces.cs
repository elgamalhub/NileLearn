using NileLearn.Application.Common.Models;
using NileLearn.Domain.Enums;

namespace NileLearn.Application.Features.Courses;

public class CategoryDto
{
    public Guid Id { get; set; }
    public string NameEn { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? DescriptionEn { get; set; }
    public string? DescriptionAr { get; set; }
    public string? IconUrl { get; set; }
    public int DisplayOrder { get; set; }
    public int CoursesCount { get; set; }
}

public class CourseFilterParams
{
    public string? SearchTerm { get; set; }
    public Guid? CategoryId { get; set; }
    public CourseLevel? Level { get; set; }
    public bool? IsFree { get; set; }
    public string? SortBy { get; set; } // "popular", "newest", "price_asc", "price_desc", "rating"
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 12;
}

public class CourseSummaryDto
{
    public Guid Id { get; set; }
    public string TitleEn { get; set; } = string.Empty;
    public string TitleAr { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string ShortDescriptionEn { get; set; } = string.Empty;
    public string ShortDescriptionAr { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public CourseLevel Level { get; set; }
    public CourseStatus Status { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = "EGP";
    public decimal? DiscountPrice { get; set; }
    public bool IsFree { get; set; }
    public string CategoryNameEn { get; set; } = string.Empty;
    public string CategoryNameAr { get; set; } = string.Empty;
    public string InstructorName { get; set; } = string.Empty;
    public string? InstructorPictureUrl { get; set; }
    public double AverageRating { get; set; }
    public int TotalReviews { get; set; }
    public int TotalEnrollments { get; set; }
    public int TotalDurationSeconds { get; set; }
    public int TotalLessonsCount { get; set; }
    public bool IsEnrolled { get; set; }
}

public class CourseDetailDto : CourseSummaryDto
{
    public string FullDescriptionEn { get; set; } = string.Empty;
    public string FullDescriptionAr { get; set; } = string.Empty;
    public string? CoverImageUrl { get; set; }
    public Guid? PreviewVideoAssetId { get; set; }
    public string? PreviewVideoUrl { get; set; }
    public List<string> Prerequisites { get; set; } = new();
    public List<string> LearningOutcomes { get; set; } = new();
    public List<string> Requirements { get; set; } = new();
    public List<string> TargetAudience { get; set; } = new();
    public List<CourseSectionDto> Sections { get; set; } = new();
    public List<InstructorSummaryDto> Instructors { get; set; } = new();
}

public class InstructorSummaryDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }
    public string? TitleEn { get; set; }
    public string? TitleAr { get; set; }
    public string? BioEn { get; set; }
    public string? BioAr { get; set; }
    public double AverageRating { get; set; }
    public int TotalStudentsCount { get; set; }
    public int TotalCoursesCount { get; set; }
}

public class CourseSectionDto
{
    public Guid Id { get; set; }
    public string TitleEn { get; set; } = string.Empty;
    public string TitleAr { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public List<LessonDto> Lessons { get; set; } = new();
}

public class LessonDto
{
    public Guid Id { get; set; }
    public Guid SectionId { get; set; }
    public string TitleEn { get; set; } = string.Empty;
    public string TitleAr { get; set; } = string.Empty;
    public string? DescriptionEn { get; set; }
    public string? DescriptionAr { get; set; }
    public LessonType Type { get; set; }
    public int SortOrder { get; set; }
    public int DurationSeconds { get; set; }
    public bool IsFreePreview { get; set; }
    public bool IsCompleted { get; set; }
    public int LastWatchedPositionSeconds { get; set; }
    public Guid? VideoAssetId { get; set; }
    public List<LessonResourceDto> Resources { get; set; } = new();
}

public class LessonResourceDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string MimeType { get; set; } = string.Empty;
    public bool IsDownloadable { get; set; }
}

public class CreateCourseRequest
{
    public string TitleEn { get; set; } = string.Empty;
    public string TitleAr { get; set; } = string.Empty;
    public string ShortDescriptionEn { get; set; } = string.Empty;
    public string ShortDescriptionAr { get; set; } = string.Empty;
    public string FullDescriptionEn { get; set; } = string.Empty;
    public string FullDescriptionAr { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public CourseLevel Level { get; set; } = CourseLevel.AllLevels;
    public decimal Price { get; set; }
    public decimal? DiscountPrice { get; set; }
    public bool IsFree { get; set; }
    public string? ThumbnailUrl { get; set; }
    public List<string>? Prerequisites { get; set; }
    public List<string>? LearningOutcomes { get; set; }
    public List<string>? Requirements { get; set; }
    public List<string>? TargetAudience { get; set; }
}

public class CreateSectionRequest
{
    public string TitleEn { get; set; } = string.Empty;
    public string TitleAr { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}

public class CreateLessonRequest
{
    public string TitleEn { get; set; } = string.Empty;
    public string TitleAr { get; set; } = string.Empty;
    public string? DescriptionEn { get; set; }
    public string? DescriptionAr { get; set; }
    public LessonType Type { get; set; } = LessonType.Video;
    public int SortOrder { get; set; }
    public int DurationSeconds { get; set; }
    public bool IsFreePreview { get; set; }
    public Guid? VideoAssetId { get; set; }
    public string? ArticleContentHtmlEn { get; set; }
    public string? ArticleContentHtmlAr { get; set; }
}

public interface ICourseService
{
    Task<ApiResponse<PagedResult<CourseSummaryDto>>> GetCoursesAsync(CourseFilterParams filter, Guid? currentUserId, CancellationToken ct = default);
    Task<ApiResponse<CourseDetailDto>> GetCourseBySlugAsync(string slug, Guid? currentUserId, CancellationToken ct = default);
    Task<ApiResponse<CourseDetailDto>> GetCourseByIdAsync(Guid courseId, Guid? currentUserId, CancellationToken ct = default);
    Task<ApiResponse<IReadOnlyList<CategoryDto>>> GetCategoriesAsync(CancellationToken ct = default);
    Task<ApiResponse<Guid>> CreateCourseAsync(CreateCourseRequest request, Guid instructorId, CancellationToken ct = default);
    Task<ApiResponse<bool>> UpdateCourseAsync(Guid courseId, CreateCourseRequest request, Guid userId, CancellationToken ct = default);
    Task<ApiResponse<bool>> PublishCourseAsync(Guid courseId, Guid userId, CancellationToken ct = default);
    Task<ApiResponse<Guid>> AddSectionAsync(Guid courseId, CreateSectionRequest request, Guid userId, CancellationToken ct = default);
    Task<ApiResponse<Guid>> AddLessonAsync(Guid sectionId, CreateLessonRequest request, Guid userId, CancellationToken ct = default);
    Task<ApiResponse<bool>> DeleteCourseAsync(Guid courseId, Guid userId, CancellationToken ct = default);
}
