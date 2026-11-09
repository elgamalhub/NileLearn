using NileLearn.Application.Common.Models;
using NileLearn.Domain.Enums;

namespace NileLearn.Application.Features.Learning;

public class StudentCourseProgressDto
{
    public Guid CourseId { get; set; }
    public string CourseTitleEn { get; set; } = string.Empty;
    public string CourseTitleAr { get; set; } = string.Empty;
    public int ProgressPercent { get; set; }
    public int CompletedLessonsCount { get; set; }
    public int TotalLessonsCount { get; set; }
    public Guid? LastWatchedLessonId { get; set; }
    public string? LastWatchedLessonTitleEn { get; set; }
    public string? LastWatchedLessonTitleAr { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public bool HasCertificate { get; set; }
    public string? CertificateCode { get; set; }
}

public class UpdateProgressRequest
{
    public Guid LessonId { get; set; }
    public int PositionSeconds { get; set; }
    public bool MarkCompleted { get; set; }
}

public class CertificateDto
{
    public string CertificateCode { get; set; } = string.Empty;
    public string StudentFullName { get; set; } = string.Empty;
    public string CourseTitle { get; set; } = string.Empty;
    public double GradePercentage { get; set; }
    public DateTime IssuedAtUtc { get; set; }
    public string VerificationUrl { get; set; } = string.Empty;
}

public class QuizDto
{
    public Guid Id { get; set; }
    public Guid LessonId { get; set; }
    public string TitleEn { get; set; } = string.Empty;
    public string TitleAr { get; set; } = string.Empty;
    public string? DescriptionEn { get; set; }
    public string? DescriptionAr { get; set; }
    public int TimeLimitMinutes { get; set; }
    public int PassingScorePercentage { get; set; }
    public int MaxAttempts { get; set; }
    public int RemainingAttempts { get; set; }
    public List<QuestionDto> Questions { get; set; } = new();
}

public class QuestionDto
{
    public Guid Id { get; set; }
    public QuestionType Type { get; set; }
    public string TextEn { get; set; } = string.Empty;
    public string TextAr { get; set; } = string.Empty;
    public int Points { get; set; }
    public List<AnswerOptionDto> Options { get; set; } = new();
}

public class AnswerOptionDto
{
    public Guid Id { get; set; }
    public string TextEn { get; set; } = string.Empty;
    public string TextAr { get; set; } = string.Empty;
}

public class StartQuizAttemptResponse
{
    public Guid AttemptId { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime DeadlineUtc { get; set; }
    public QuizDto Quiz { get; set; } = null!;
}

public class SubmitQuizAttemptRequest
{
    public Guid AttemptId { get; set; }
    public List<StudentAnswerSubmission> Answers { get; set; } = new();
}

public class StudentAnswerSubmission
{
    public Guid QuestionId { get; set; }
    public List<Guid>? SelectedOptionIds { get; set; }
    public string? TextResponse { get; set; }
}

public class QuizAttemptResultDto
{
    public Guid AttemptId { get; set; }
    public double ScorePercentage { get; set; }
    public bool Passed { get; set; }
    public int TotalScore { get; set; }
    public int MaxPossibleScore { get; set; }
    public int TimeSpentSeconds { get; set; }
    public bool CertificateIssued { get; set; }
    public string? CertificateCode { get; set; }
}

public interface ILearningService
{
    Task<ApiResponse<StudentCourseProgressDto>> GetCourseProgressAsync(Guid userId, Guid courseId, CancellationToken ct = default);
    Task<ApiResponse<IReadOnlyList<StudentCourseProgressDto>>> GetMyEnrolledCoursesAsync(Guid userId, CancellationToken ct = default);
    Task<ApiResponse<bool>> UpdateLessonProgressAsync(Guid userId, UpdateProgressRequest request, CancellationToken ct = default);
    Task<ApiResponse<CertificateDto>> IssueCertificateIfEligibleAsync(Guid userId, Guid courseId, CancellationToken ct = default);
    Task<ApiResponse<CertificateDto>> VerifyCertificateAsync(string certificateCode, CancellationToken ct = default);
    Task<ApiResponse<IReadOnlyList<CertificateDto>>> GetMyCertificatesAsync(Guid userId, CancellationToken ct = default);

    // Quizzes
    Task<ApiResponse<StartQuizAttemptResponse>> StartQuizAsync(Guid userId, Guid quizId, CancellationToken ct = default);
    Task<ApiResponse<QuizAttemptResultDto>> SubmitQuizAsync(Guid userId, SubmitQuizAttemptRequest request, CancellationToken ct = default);
}
