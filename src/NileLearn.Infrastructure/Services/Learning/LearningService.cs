using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NileLearn.Application.Common.Interfaces;
using NileLearn.Application.Common.Models;
using NileLearn.Application.Features.Learning;
using NileLearn.Domain.Entities.Assessments;
using NileLearn.Domain.Entities.Learning;
using NileLearn.Domain.Enums;

namespace NileLearn.Infrastructure.Services.Learning;

public class LearningService : ILearningService
{
    private readonly IApplicationDbContext _context;

    public LearningService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<StudentCourseProgressDto>> GetCourseProgressAsync(Guid userId, Guid courseId, CancellationToken ct = default)
    {
        var course = await _context.Courses
            .Include(c => c.Sections).ThenInclude(s => s.Lessons)
            .FirstOrDefaultAsync(c => c.Id == courseId, ct);

        if (course == null) return ApiResponse<StudentCourseProgressDto>.Fail("Course not found.", "الدورة غير موجودة.");

        var enrollment = await _context.Enrollments
            .FirstOrDefaultAsync(e => e.UserId == userId && e.CourseId == courseId, ct);

        var allLessons = course.Sections.SelectMany(s => s.Lessons).ToList();
        var totalLessons = allLessons.Count;

        var progressList = await _context.LearningProgress
            .Where(p => p.UserId == userId && p.CourseId == courseId)
            .ToListAsync(ct);

        var completedCount = progressList.Count(p => p.IsCompleted);
        var lastWatched = progressList.OrderByDescending(p => p.LastActiveAtUtc).FirstOrDefault();
        var lastLesson = lastWatched != null ? allLessons.FirstOrDefault(l => l.Id == lastWatched.LessonId) : null;

        var certificate = await _context.Certificates
            .FirstOrDefaultAsync(c => c.UserId == userId && c.CourseId == courseId, ct);

        var percent = totalLessons > 0 ? (int)Math.Round((double)completedCount / totalLessons * 100) : 0;

        return ApiResponse<StudentCourseProgressDto>.Ok(new StudentCourseProgressDto
        {
            CourseId = courseId,
            CourseTitleEn = course.TitleEn,
            CourseTitleAr = course.TitleAr,
            ProgressPercent = percent,
            CompletedLessonsCount = completedCount,
            TotalLessonsCount = totalLessons,
            LastWatchedLessonId = lastLesson?.Id,
            LastWatchedLessonTitleEn = lastLesson?.TitleEn,
            LastWatchedLessonTitleAr = lastLesson?.TitleAr,
            CompletedAtUtc = enrollment?.CompletedAtUtc,
            HasCertificate = certificate != null,
            CertificateCode = certificate?.CertificateCode
        });
    }

    public async Task<ApiResponse<IReadOnlyList<StudentCourseProgressDto>>> GetMyEnrolledCoursesAsync(Guid userId, CancellationToken ct = default)
    {
        var enrollments = await _context.Enrollments
            .Include(e => e.Course).ThenInclude(c => c.Sections).ThenInclude(s => s.Lessons)
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.LastAccessedAtUtc)
            .ToListAsync(ct);

        var certificates = await _context.Certificates
            .Where(c => c.UserId == userId)
            .ToDictionaryAsync(c => c.CourseId, c => c.CertificateCode, ct);

        var dtos = new List<StudentCourseProgressDto>();
        foreach (var e in enrollments)
        {
            var allLessons = e.Course.Sections.SelectMany(s => s.Lessons).ToList();
            var totalLessons = allLessons.Count;

            certificates.TryGetValue(e.CourseId, out var certCode);

            dtos.Add(new StudentCourseProgressDto
            {
                CourseId = e.CourseId,
                CourseTitleEn = e.Course.TitleEn,
                CourseTitleAr = e.Course.TitleAr,
                ProgressPercent = e.ProgressPercent,
                CompletedLessonsCount = (int)Math.Round((double)e.ProgressPercent * totalLessons / 100),
                TotalLessonsCount = totalLessons,
                CompletedAtUtc = e.CompletedAtUtc,
                HasCertificate = certCode != null,
                CertificateCode = certCode
            });
        }

        return ApiResponse<IReadOnlyList<StudentCourseProgressDto>>.Ok(dtos);
    }

    public async Task<ApiResponse<bool>> UpdateLessonProgressAsync(Guid userId, UpdateProgressRequest request, CancellationToken ct = default)
    {
        var lesson = await _context.Lessons
            .Include(l => l.Section)
            .FirstOrDefaultAsync(l => l.Id == request.LessonId, ct);

        if (lesson == null) return ApiResponse<bool>.Fail("Lesson not found.", "الدرس غير موجود.");

        var progress = await _context.LearningProgress
            .FirstOrDefaultAsync(p => p.UserId == userId && p.LessonId == request.LessonId, ct);

        if (progress == null)
        {
            progress = new Domain.Entities.Learning.LearningProgress
            {
                UserId = userId,
                CourseId = lesson.Section.CourseId,
                LessonId = request.LessonId,
                LastWatchedPositionSeconds = request.PositionSeconds,
                IsCompleted = request.MarkCompleted,
                CompletedAtUtc = request.MarkCompleted ? DateTime.UtcNow : null,
                LastActiveAtUtc = DateTime.UtcNow
            };
            _context.LearningProgress.Add(progress);
        }
        else
        {
            progress.LastWatchedPositionSeconds = request.PositionSeconds;
            if (request.MarkCompleted && !progress.IsCompleted)
            {
                progress.IsCompleted = true;
                progress.CompletedAtUtc = DateTime.UtcNow;
            }
            progress.LastActiveAtUtc = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(ct);
        return ApiResponse<bool>.Ok(true);
    }

    public async Task<ApiResponse<CertificateDto>> IssueCertificateIfEligibleAsync(Guid userId, Guid courseId, CancellationToken ct = default)
    {
        var existingCert = await _context.Certificates
            .FirstOrDefaultAsync(c => c.UserId == userId && c.CourseId == courseId, ct);

        if (existingCert != null)
        {
            return ApiResponse<CertificateDto>.Ok(MapCertificateDto(existingCert));
        }

        var course = await _context.Courses
            .Include(c => c.Sections).ThenInclude(s => s.Lessons)
            .FirstOrDefaultAsync(c => c.Id == courseId, ct);

        if (course == null) return ApiResponse<CertificateDto>.Fail("Course not found.", "الدورة غير موجودة.");

        var user = await _context.Users.FindAsync(new object[] { userId }, ct);
        if (user == null) return ApiResponse<CertificateDto>.Fail("User not found.", "المستخدم غير موجود.");

        var totalLessons = course.Sections.SelectMany(s => s.Lessons).Count();
        var completedLessons = await _context.LearningProgress
            .CountAsync(p => p.UserId == userId && p.CourseId == courseId && p.IsCompleted, ct);

        // Require at least 80% progress or all lessons
        var completionRate = totalLessons > 0 ? (double)completedLessons / totalLessons : 0;
        if (completionRate < 0.8)
        {
            return ApiResponse<CertificateDto>.Fail("You must complete at least 80% of the course to receive a certificate.", "يجب إكمال 80% على الأقل من الدورة للحصول على الشهادة.");
        }

        var certCode = $"NL-{DateTime.UtcNow.Year}-{Guid.NewGuid():N}"[..18].ToUpper();
        var cert = new Certificate
        {
            CertificateCode = certCode,
            UserId = userId,
            CourseId = courseId,
            StudentFullName = user.FullName,
            CourseTitle = course.TitleEn,
            GradePercentage = 95.0,
            IssuedAtUtc = DateTime.UtcNow,
            VerificationUrl = $"/verify-certificate/{certCode}"
        };

        _context.Certificates.Add(cert);
        await _context.SaveChangesAsync(ct);

        return ApiResponse<CertificateDto>.Ok(MapCertificateDto(cert), "Certificate issued successfully!", "تم إصدار الشهادة بنجاح!");
    }

    public async Task<ApiResponse<CertificateDto>> VerifyCertificateAsync(string certificateCode, CancellationToken ct = default)
    {
        var cert = await _context.Certificates
            .FirstOrDefaultAsync(c => c.CertificateCode.ToUpper() == certificateCode.Trim().ToUpper(), ct);

        if (cert == null)
        {
            return ApiResponse<CertificateDto>.Fail("Certificate not found or invalid.", "الشهادة غير صالحة أو غير موجودة.");
        }

        return ApiResponse<CertificateDto>.Ok(MapCertificateDto(cert));
    }

    public async Task<ApiResponse<IReadOnlyList<CertificateDto>>> GetMyCertificatesAsync(Guid userId, CancellationToken ct = default)
    {
        var certs = await _context.Certificates
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.IssuedAtUtc)
            .ToListAsync(ct);

        return ApiResponse<IReadOnlyList<CertificateDto>>.Ok(certs.Select(MapCertificateDto).ToList());
    }

    public async Task<ApiResponse<StartQuizAttemptResponse>> StartQuizAsync(Guid userId, Guid quizId, CancellationToken ct = default)
    {
        var quiz = await _context.Quizzes
            .Include(q => q.Questions).ThenInclude(qn => qn.AnswerOptions)
            .FirstOrDefaultAsync(q => q.Id == quizId && q.IsActive, ct);

        if (quiz == null) return ApiResponse<StartQuizAttemptResponse>.Fail("Quiz not found.", "الاختبار غير موجود.");

        // Check attempts limit
        var previousAttemptsCount = await _context.QuizAttempts
            .CountAsync(a => a.QuizId == quizId && a.UserId == userId, ct);

        if (previousAttemptsCount >= quiz.MaxAttempts)
        {
            return ApiResponse<StartQuizAttemptResponse>.Fail($"You have reached the maximum allowed attempts ({quiz.MaxAttempts}).", $"لقد استنفدت الحد الأقصى للمحاولات ({quiz.MaxAttempts}).");
        }

        var startedAt = DateTime.UtcNow;
        var deadline = startedAt.AddMinutes(quiz.TimeLimitMinutes);

        var attempt = new QuizAttempt
        {
            QuizId = quizId,
            UserId = userId,
            StartedAtUtc = startedAt,
            MaxPossibleScore = quiz.Questions.Sum(q => q.Points)
        };

        _context.QuizAttempts.Add(attempt);
        await _context.SaveChangesAsync(ct);

        var quizDto = new QuizDto
        {
            Id = quiz.Id,
            LessonId = quiz.LessonId,
            TitleEn = quiz.TitleEn,
            TitleAr = quiz.TitleAr,
            DescriptionEn = quiz.DescriptionEn,
            DescriptionAr = quiz.DescriptionAr,
            TimeLimitMinutes = quiz.TimeLimitMinutes,
            PassingScorePercentage = quiz.PassingScorePercentage,
            MaxAttempts = quiz.MaxAttempts,
            RemainingAttempts = quiz.MaxAttempts - previousAttemptsCount - 1,
            Questions = quiz.Questions.OrderBy(q => q.SortOrder).Select(q => new QuestionDto
            {
                Id = q.Id,
                Type = q.Type,
                TextEn = q.TextEn,
                TextAr = q.TextAr,
                Points = q.Points,
                Options = q.AnswerOptions.OrderBy(o => o.SortOrder).Select(o => new AnswerOptionDto
                {
                    Id = o.Id,
                    TextEn = o.TextEn,
                    TextAr = o.TextAr
                }).ToList()
            }).ToList()
        };

        return ApiResponse<StartQuizAttemptResponse>.Ok(new StartQuizAttemptResponse
        {
            AttemptId = attempt.Id,
            StartedAtUtc = startedAt,
            DeadlineUtc = deadline,
            Quiz = quizDto
        });
    }

    public async Task<ApiResponse<QuizAttemptResultDto>> SubmitQuizAsync(Guid userId, SubmitQuizAttemptRequest request, CancellationToken ct = default)
    {
        var attempt = await _context.QuizAttempts
            .Include(a => a.Quiz).ThenInclude(q => q.Questions).ThenInclude(qn => qn.AnswerOptions)
            .Include(a => a.Quiz).ThenInclude(q => q.Lesson).ThenInclude(l => l.Section)
            .FirstOrDefaultAsync(a => a.Id == request.AttemptId && a.UserId == userId, ct);

        if (attempt == null) return ApiResponse<QuizAttemptResultDto>.Fail("Attempt not found.", "المحاولة غير موجودة.");

        var submittedAt = DateTime.UtcNow;
        var timeSpent = (int)(submittedAt - attempt.StartedAtUtc).TotalSeconds;

        var questions = attempt.Quiz.Questions.ToList();
        var totalPoints = questions.Sum(q => q.Points);
        var earnedPoints = 0;

        foreach (var answerSubmission in request.Answers)
        {
            var question = questions.FirstOrDefault(q => q.Id == answerSubmission.QuestionId);
            if (question == null) continue;

            var correctOptionIds = question.AnswerOptions.Where(o => o.IsCorrect).Select(o => o.Id).ToHashSet();
            var selectedOptionIds = answerSubmission.SelectedOptionIds?.ToHashSet() ?? new HashSet<Guid>();

            var isCorrect = correctOptionIds.SetEquals(selectedOptionIds);
            var pointsAwarded = isCorrect ? question.Points : 0;
            earnedPoints += pointsAwarded;

            var studentAnswer = new StudentAnswer
            {
                QuizAttemptId = attempt.Id,
                QuestionId = question.Id,
                SelectedOptionIdsJson = JsonSerializer.Serialize(selectedOptionIds.ToList()),
                TextResponse = answerSubmission.TextResponse,
                IsCorrect = isCorrect,
                ScoreAwarded = pointsAwarded
            };
            _context.StudentAnswers.Add(studentAnswer);
        }

        var scorePercentage = totalPoints > 0 ? Math.Round((double)earnedPoints / totalPoints * 100, 2) : 0;
        var passed = scorePercentage >= attempt.Quiz.PassingScorePercentage;

        attempt.SubmittedAtUtc = submittedAt;
        attempt.TimeSpentSeconds = timeSpent;
        attempt.TotalScore = earnedPoints;
        attempt.MaxPossibleScore = totalPoints;
        attempt.ScorePercentage = scorePercentage;
        attempt.Passed = passed;

        // If passed, mark lesson completed
        if (passed)
        {
            await UpdateLessonProgressAsync(userId, new UpdateProgressRequest
            {
                LessonId = attempt.Quiz.LessonId,
                PositionSeconds = 0,
                MarkCompleted = true
            }, ct);
        }

        await _context.SaveChangesAsync(ct);

        return ApiResponse<QuizAttemptResultDto>.Ok(new QuizAttemptResultDto
        {
            AttemptId = attempt.Id,
            ScorePercentage = scorePercentage,
            Passed = passed,
            TotalScore = earnedPoints,
            MaxPossibleScore = totalPoints,
            TimeSpentSeconds = timeSpent
        });
    }

    private static CertificateDto MapCertificateDto(Certificate c)
    {
        return new CertificateDto
        {
            CertificateCode = c.CertificateCode,
            StudentFullName = c.StudentFullName,
            CourseTitle = c.CourseTitle,
            GradePercentage = c.GradePercentage,
            IssuedAtUtc = c.IssuedAtUtc,
            VerificationUrl = c.VerificationUrl
        };
    }
}
