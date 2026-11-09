using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NileLearn.Application.Common.Interfaces;
using NileLearn.Application.Common.Models;
using NileLearn.Application.Features.Courses;
using NileLearn.Domain.Entities.Courses;
using NileLearn.Domain.Enums;

namespace NileLearn.Infrastructure.Services.Courses;

public class CourseService : ICourseService
{
    private readonly IApplicationDbContext _context;

    public CourseService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<PagedResult<CourseSummaryDto>>> GetCoursesAsync(CourseFilterParams filter, Guid? currentUserId, CancellationToken ct = default)
    {
        var query = _context.Courses
            .Include(c => c.Category)
            .Include(c => c.Instructors).ThenInclude(ci => ci.Instructor)
            .Include(c => c.Sections).ThenInclude(s => s.Lessons)
            .Where(c => c.Status == CourseStatus.Published)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var term = filter.SearchTerm.Trim().ToLower();
            query = query.Where(c =>
                c.TitleEn.ToLower().Contains(term) ||
                c.TitleAr.ToLower().Contains(term) ||
                c.ShortDescriptionEn.ToLower().Contains(term) ||
                c.ShortDescriptionAr.ToLower().Contains(term));
        }

        if (filter.CategoryId.HasValue)
        {
            query = query.Where(c => c.CategoryId == filter.CategoryId.Value);
        }

        if (filter.Level.HasValue)
        {
            query = query.Where(c => c.Level == filter.Level.Value);
        }

        if (filter.IsFree.HasValue)
        {
            query = query.Where(c => c.IsFree == filter.IsFree.Value);
        }

        // Sorting
        query = filter.SortBy switch
        {
            "popular" => query.OrderByDescending(c => c.TotalEnrollments),
            "rating" => query.OrderByDescending(c => c.AverageRating),
            "price_asc" => query.OrderBy(c => c.DiscountPrice ?? c.Price),
            "price_desc" => query.OrderByDescending(c => c.DiscountPrice ?? c.Price),
            _ => query.OrderByDescending(c => c.CreatedAtUtc)
        };

        var totalCount = await query.CountAsync(ct);
        var pageIndex = Math.Max(1, filter.PageIndex);
        var pageSize = Math.Max(1, Math.Min(100, filter.PageSize));

        var courses = await query
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        // Check enrollments for current user
        var enrolledCourseIds = new HashSet<Guid>();
        if (currentUserId.HasValue)
        {
            var userEnrolled = await _context.Enrollments
                .Where(e => e.UserId == currentUserId.Value)
                .Select(e => e.CourseId)
                .ToListAsync(ct);
            enrolledCourseIds = new HashSet<Guid>(userEnrolled);
        }

        var dtos = courses.Select(c =>
        {
            var primaryInstructor = c.Instructors.FirstOrDefault(ci => ci.IsPrimaryInstructor)?.Instructor
                                    ?? c.Instructors.FirstOrDefault()?.Instructor;

            return new CourseSummaryDto
            {
                Id = c.Id,
                TitleEn = c.TitleEn,
                TitleAr = c.TitleAr,
                Slug = c.Slug,
                ShortDescriptionEn = c.ShortDescriptionEn,
                ShortDescriptionAr = c.ShortDescriptionAr,
                ThumbnailUrl = c.ThumbnailUrl,
                Level = c.Level,
                Status = c.Status,
                Price = c.Price,
                Currency = c.Currency,
                DiscountPrice = c.DiscountPrice,
                IsFree = c.IsFree,
                CategoryNameEn = c.Category.NameEn,
                CategoryNameAr = c.Category.NameAr,
                InstructorName = primaryInstructor?.FullName ?? "NileLearn Instructor",
                InstructorPictureUrl = primaryInstructor?.ProfilePictureUrl,
                AverageRating = c.AverageRating,
                TotalReviews = c.TotalReviews,
                TotalEnrollments = c.TotalEnrollments,
                TotalDurationSeconds = c.Sections.SelectMany(s => s.Lessons).Sum(l => l.DurationSeconds),
                TotalLessonsCount = c.Sections.SelectMany(s => s.Lessons).Count(),
                IsEnrolled = enrolledCourseIds.Contains(c.Id)
            };
        }).ToList();

        var result = PagedResult<CourseSummaryDto>.Create(dtos, pageIndex, pageSize, totalCount);
        return ApiResponse<PagedResult<CourseSummaryDto>>.Ok(result);
    }

    public async Task<ApiResponse<CourseDetailDto>> GetCourseBySlugAsync(string slug, Guid? currentUserId, CancellationToken ct = default)
    {
        var course = await _context.Courses
            .Include(c => c.Category)
            .Include(c => c.PreviewVideoAsset)
            .Include(c => c.Instructors).ThenInclude(ci => ci.Instructor).ThenInclude(i => i.InstructorProfile)
            .Include(c => c.Sections.OrderBy(s => s.SortOrder)).ThenInclude(s => s.Lessons.OrderBy(l => l.SortOrder)).ThenInclude(l => l.Resources)
            .FirstOrDefaultAsync(c => c.Slug == slug, ct);

        if (course == null)
        {
            return ApiResponse<CourseDetailDto>.Fail("Course not found.", "الدورة التدريبية غير موجودة.");
        }

        return ApiResponse<CourseDetailDto>.Ok(await MapCourseDetailDto(course, currentUserId, ct));
    }

    public async Task<ApiResponse<CourseDetailDto>> GetCourseByIdAsync(Guid courseId, Guid? currentUserId, CancellationToken ct = default)
    {
        var course = await _context.Courses
            .Include(c => c.Category)
            .Include(c => c.PreviewVideoAsset)
            .Include(c => c.Instructors).ThenInclude(ci => ci.Instructor).ThenInclude(i => i.InstructorProfile)
            .Include(c => c.Sections.OrderBy(s => s.SortOrder)).ThenInclude(s => s.Lessons.OrderBy(l => l.SortOrder)).ThenInclude(l => l.Resources)
            .FirstOrDefaultAsync(c => c.Id == courseId, ct);

        if (course == null)
        {
            return ApiResponse<CourseDetailDto>.Fail("Course not found.", "الدورة التدريبية غير موجودة.");
        }

        return ApiResponse<CourseDetailDto>.Ok(await MapCourseDetailDto(course, currentUserId, ct));
    }

    public async Task<ApiResponse<IReadOnlyList<CategoryDto>>> GetCategoriesAsync(CancellationToken ct = default)
    {
        var categories = await _context.Categories
            .Where(c => c.IsActive)
            .OrderBy(c => c.DisplayOrder)
            .Select(c => new CategoryDto
            {
                Id = c.Id,
                NameEn = c.NameEn,
                NameAr = c.NameAr,
                Slug = c.Slug,
                DescriptionEn = c.DescriptionEn,
                DescriptionAr = c.DescriptionAr,
                IconUrl = c.IconUrl,
                DisplayOrder = c.DisplayOrder,
                CoursesCount = c.Courses.Count(crs => crs.Status == CourseStatus.Published)
            })
            .ToListAsync(ct);

        return ApiResponse<IReadOnlyList<CategoryDto>>.Ok(categories);
    }

    public async Task<ApiResponse<Guid>> CreateCourseAsync(CreateCourseRequest request, Guid instructorId, CancellationToken ct = default)
    {
        var slug = GenerateSlug(request.TitleEn);
        var baseSlug = slug;
        var counter = 1;
        while (await _context.Courses.AnyAsync(c => c.Slug == slug, ct))
        {
            slug = $"{baseSlug}-{counter++}";
        }

        var course = new Course
        {
            TitleEn = request.TitleEn,
            TitleAr = request.TitleAr,
            Slug = slug,
            ShortDescriptionEn = request.ShortDescriptionEn,
            ShortDescriptionAr = request.ShortDescriptionAr,
            FullDescriptionEn = request.FullDescriptionEn,
            FullDescriptionAr = request.FullDescriptionAr,
            CategoryId = request.CategoryId,
            Level = request.Level,
            Price = request.Price,
            DiscountPrice = request.DiscountPrice,
            IsFree = request.IsFree,
            ThumbnailUrl = request.ThumbnailUrl,
            Status = CourseStatus.Draft,
            PrerequisitesJson = request.Prerequisites != null ? JsonSerializer.Serialize(request.Prerequisites) : null,
            LearningOutcomesJson = request.LearningOutcomes != null ? JsonSerializer.Serialize(request.LearningOutcomes) : null,
            RequirementsJson = request.Requirements != null ? JsonSerializer.Serialize(request.Requirements) : null,
            TargetAudienceJson = request.TargetAudience != null ? JsonSerializer.Serialize(request.TargetAudience) : null
        };

        course.Instructors.Add(new CourseInstructor
        {
            CourseId = course.Id,
            InstructorId = instructorId,
            IsPrimaryInstructor = true
        });

        _context.Courses.Add(course);
        await _context.SaveChangesAsync(ct);

        return ApiResponse<Guid>.Ok(course.Id, "Course created successfully.", "تم إنشاء الدورة بنجاح.");
    }

    public async Task<ApiResponse<bool>> UpdateCourseAsync(Guid courseId, CreateCourseRequest request, Guid userId, CancellationToken ct = default)
    {
        var course = await _context.Courses.FindAsync(new object[] { courseId }, ct);
        if (course == null) return ApiResponse<bool>.Fail("Course not found.", "الدورة غير موجودة.");

        course.TitleEn = request.TitleEn;
        course.TitleAr = request.TitleAr;
        course.ShortDescriptionEn = request.ShortDescriptionEn;
        course.ShortDescriptionAr = request.ShortDescriptionAr;
        course.FullDescriptionEn = request.FullDescriptionEn;
        course.FullDescriptionAr = request.FullDescriptionAr;
        course.CategoryId = request.CategoryId;
        course.Level = request.Level;
        course.Price = request.Price;
        course.DiscountPrice = request.DiscountPrice;
        course.IsFree = request.IsFree;
        course.ThumbnailUrl = request.ThumbnailUrl;
        course.PrerequisitesJson = request.Prerequisites != null ? JsonSerializer.Serialize(request.Prerequisites) : null;
        course.LearningOutcomesJson = request.LearningOutcomes != null ? JsonSerializer.Serialize(request.LearningOutcomes) : null;
        course.RequirementsJson = request.Requirements != null ? JsonSerializer.Serialize(request.Requirements) : null;
        course.TargetAudienceJson = request.TargetAudience != null ? JsonSerializer.Serialize(request.TargetAudience) : null;
        course.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);
        return ApiResponse<bool>.Ok(true, "Course updated successfully.", "تم تحديث الدورة بنجاح.");
    }

    public async Task<ApiResponse<bool>> PublishCourseAsync(Guid courseId, Guid userId, CancellationToken ct = default)
    {
        var course = await _context.Courses.FindAsync(new object[] { courseId }, ct);
        if (course == null) return ApiResponse<bool>.Fail("Course not found.", "الدورة غير موجودة.");

        course.Status = CourseStatus.Published;
        course.PublishedAtUtc = DateTime.UtcNow;
        course.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);
        return ApiResponse<bool>.Ok(true, "Course published successfully.", "تم نشر الدورة بنجاح.");
    }

    public async Task<ApiResponse<Guid>> AddSectionAsync(Guid courseId, CreateSectionRequest request, Guid userId, CancellationToken ct = default)
    {
        var course = await _context.Courses.FindAsync(new object[] { courseId }, ct);
        if (course == null) return ApiResponse<Guid>.Fail("Course not found.", "الدورة غير موجودة.");

        var section = new CourseSection
        {
            CourseId = courseId,
            TitleEn = request.TitleEn,
            TitleAr = request.TitleAr,
            SortOrder = request.SortOrder
        };

        _context.CourseSections.Add(section);
        await _context.SaveChangesAsync(ct);
        return ApiResponse<Guid>.Ok(section.Id, "Section added successfully.", "تمت إضافة القسم بنجاح.");
    }

    public async Task<ApiResponse<Guid>> AddLessonAsync(Guid sectionId, CreateLessonRequest request, Guid userId, CancellationToken ct = default)
    {
        var section = await _context.CourseSections.FindAsync(new object[] { sectionId }, ct);
        if (section == null) return ApiResponse<Guid>.Fail("Section not found.", "القسم غير موجود.");

        var lesson = new Lesson
        {
            SectionId = sectionId,
            TitleEn = request.TitleEn,
            TitleAr = request.TitleAr,
            DescriptionEn = request.DescriptionEn,
            DescriptionAr = request.DescriptionAr,
            Type = request.Type,
            SortOrder = request.SortOrder,
            DurationSeconds = request.DurationSeconds,
            IsFreePreview = request.IsFreePreview,
            VideoAssetId = request.VideoAssetId,
            ArticleContentHtmlEn = request.ArticleContentHtmlEn,
            ArticleContentHtmlAr = request.ArticleContentHtmlAr
        };

        _context.Lessons.Add(lesson);
        await _context.SaveChangesAsync(ct);
        return ApiResponse<Guid>.Ok(lesson.Id, "Lesson added successfully.", "تمت إضافة الدرس بنجاح.");
    }

    public async Task<ApiResponse<bool>> DeleteCourseAsync(Guid courseId, Guid userId, CancellationToken ct = default)
    {
        var course = await _context.Courses.FindAsync(new object[] { courseId }, ct);
        if (course == null) return ApiResponse<bool>.Fail("Course not found.", "الدورة غير موجودة.");

        course.IsDeleted = true;
        course.DeletedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);

        return ApiResponse<bool>.Ok(true, "Course deleted successfully.", "تم حذف الدورة بنجاح.");
    }

    private async Task<CourseDetailDto> MapCourseDetailDto(Course course, Guid? currentUserId, CancellationToken ct)
    {
        var isEnrolled = false;
        var completedLessonIds = new HashSet<Guid>();
        var lessonPositions = new Dictionary<Guid, int>();

        if (currentUserId.HasValue)
        {
            isEnrolled = await _context.Enrollments.AnyAsync(e => e.CourseId == course.Id && e.UserId == currentUserId.Value, ct);

            var progress = await _context.LearningProgress
                .Where(p => p.CourseId == course.Id && p.UserId == currentUserId.Value)
                .ToListAsync(ct);

            foreach (var p in progress)
            {
                if (p.IsCompleted) completedLessonIds.Add(p.LessonId);
                lessonPositions[p.LessonId] = p.LastWatchedPositionSeconds;
            }
        }

        var primaryInstructor = course.Instructors.FirstOrDefault(ci => ci.IsPrimaryInstructor)?.Instructor
                                ?? course.Instructors.FirstOrDefault()?.Instructor;

        return new CourseDetailDto
        {
            Id = course.Id,
            TitleEn = course.TitleEn,
            TitleAr = course.TitleAr,
            Slug = course.Slug,
            ShortDescriptionEn = course.ShortDescriptionEn,
            ShortDescriptionAr = course.ShortDescriptionAr,
            FullDescriptionEn = course.FullDescriptionEn,
            FullDescriptionAr = course.FullDescriptionAr,
            ThumbnailUrl = course.ThumbnailUrl,
            CoverImageUrl = course.CoverImageUrl,
            PreviewVideoAssetId = course.PreviewVideoAssetId,
            PreviewVideoUrl = course.PreviewVideoAsset?.HlsManifestUrl,
            Level = course.Level,
            Status = course.Status,
            Price = course.Price,
            Currency = course.Currency,
            DiscountPrice = course.DiscountPrice,
            IsFree = course.IsFree,
            CategoryNameEn = course.Category.NameEn,
            CategoryNameAr = course.Category.NameAr,
            InstructorName = primaryInstructor?.FullName ?? "NileLearn Instructor",
            InstructorPictureUrl = primaryInstructor?.ProfilePictureUrl,
            AverageRating = course.AverageRating,
            TotalReviews = course.TotalReviews,
            TotalEnrollments = course.TotalEnrollments,
            TotalDurationSeconds = course.Sections.SelectMany(s => s.Lessons).Sum(l => l.DurationSeconds),
            TotalLessonsCount = course.Sections.SelectMany(s => s.Lessons).Count(),
            IsEnrolled = isEnrolled,
            Prerequisites = DeserializeList(course.PrerequisitesJson),
            LearningOutcomes = DeserializeList(course.LearningOutcomesJson),
            Requirements = DeserializeList(course.RequirementsJson),
            TargetAudience = DeserializeList(course.TargetAudienceJson),
            Sections = course.Sections.OrderBy(s => s.SortOrder).Select(s => new CourseSectionDto
            {
                Id = s.Id,
                TitleEn = s.TitleEn,
                TitleAr = s.TitleAr,
                SortOrder = s.SortOrder,
                Lessons = s.Lessons.OrderBy(l => l.SortOrder).Select(l => new LessonDto
                {
                    Id = l.Id,
                    SectionId = l.SectionId,
                    TitleEn = l.TitleEn,
                    TitleAr = l.TitleAr,
                    DescriptionEn = l.DescriptionEn,
                    DescriptionAr = l.DescriptionAr,
                    Type = l.Type,
                    SortOrder = l.SortOrder,
                    DurationSeconds = l.DurationSeconds,
                    IsFreePreview = l.IsFreePreview,
                    IsCompleted = completedLessonIds.Contains(l.Id),
                    LastWatchedPositionSeconds = lessonPositions.TryGetValue(l.Id, out var pos) ? pos : 0,
                    VideoAssetId = l.VideoAssetId,
                    Resources = l.Resources.Select(r => new LessonResourceDto
                    {
                        Id = r.Id,
                        Title = r.Title,
                        FileUrl = r.FileUrl,
                        FileSizeBytes = r.FileSizeBytes,
                        MimeType = r.MimeType,
                        IsDownloadable = r.IsDownloadable
                    }).ToList()
                }).ToList()
            }).ToList(),
            Instructors = course.Instructors.Select(ci => new InstructorSummaryDto
            {
                Id = ci.Instructor.Id,
                FullName = ci.Instructor.FullName,
                ProfilePictureUrl = ci.Instructor.ProfilePictureUrl,
                TitleEn = ci.Instructor.InstructorProfile?.TitleEn,
                TitleAr = ci.Instructor.InstructorProfile?.TitleAr,
                BioEn = ci.Instructor.InstructorProfile?.BiographyEn,
                BioAr = ci.Instructor.InstructorProfile?.BiographyAr,
                AverageRating = ci.Instructor.InstructorProfile?.AverageRating ?? 5.0,
                TotalStudentsCount = ci.Instructor.InstructorProfile?.TotalStudentsCount ?? 0,
                TotalCoursesCount = 1
            }).ToList()
        };
    }

    private static List<string> DeserializeList(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new List<string>();
        try { return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>(); }
        catch { return new List<string>(); }
    }

    private static string GenerateSlug(string title)
    {
        var slug = title.ToLowerInvariant().Trim();
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[^a-z0-9\s-]", "");
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"\s+", "-").Trim('-');
        return string.IsNullOrEmpty(slug) ? Guid.NewGuid().ToString("N")[..8] : slug;
    }
}
