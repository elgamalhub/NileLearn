using NileLearn.Domain.Common;
using NileLearn.Domain.Entities.Courses;
using NileLearn.Domain.Entities.Identity;
using NileLearn.Domain.Enums;

namespace NileLearn.Domain.Entities.Assessments;

public class Quiz : BaseEntity
{
    public Guid LessonId { get; set; }
    public virtual Lesson Lesson { get; set; } = null!;

    public string TitleEn { get; set; } = string.Empty;
    public string TitleAr { get; set; } = string.Empty;
    public string? DescriptionEn { get; set; }
    public string? DescriptionAr { get; set; }

    public int TimeLimitMinutes { get; set; } = 30;
    public int PassingScorePercentage { get; set; } = 70;
    public int MaxAttempts { get; set; } = 3;
    public bool RandomizeQuestions { get; set; } = true;
    public bool IsActive { get; set; } = true;

    public virtual ICollection<Question> Questions { get; set; } = new List<Question>();
    public virtual ICollection<QuizAttempt> Attempts { get; set; } = new List<QuizAttempt>();
}

public class Question : BaseEntity
{
    public Guid QuizId { get; set; }
    public virtual Quiz Quiz { get; set; } = null!;

    public QuestionType Type { get; set; } = QuestionType.SingleChoice;
    public string TextEn { get; set; } = string.Empty;
    public string TextAr { get; set; } = string.Empty;
    public int Points { get; set; } = 1;
    public string? ExplanationEn { get; set; }
    public string? ExplanationAr { get; set; }
    public int SortOrder { get; set; }

    public virtual ICollection<AnswerOption> AnswerOptions { get; set; } = new List<AnswerOption>();
}

public class AnswerOption : BaseEntity
{
    public Guid QuestionId { get; set; }
    public virtual Question Question { get; set; } = null!;

    public string TextEn { get; set; } = string.Empty;
    public string TextAr { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public int SortOrder { get; set; }
}

public class QuizAttempt : BaseEntity
{
    public Guid QuizId { get; set; }
    public virtual Quiz Quiz { get; set; } = null!;

    public Guid UserId { get; set; }
    public virtual ApplicationUser User { get; set; } = null!;

    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? SubmittedAtUtc { get; set; }
    public int TimeSpentSeconds { get; set; }
    public double ScorePercentage { get; set; }
    public bool Passed { get; set; }
    public int TotalScore { get; set; }
    public int MaxPossibleScore { get; set; }

    public virtual ICollection<StudentAnswer> StudentAnswers { get; set; } = new List<StudentAnswer>();
}

public class StudentAnswer : BaseEntity
{
    public Guid QuizAttemptId { get; set; }
    public virtual QuizAttempt QuizAttempt { get; set; } = null!;

    public Guid QuestionId { get; set; }
    public virtual Question Question { get; set; } = null!;

    public string? SelectedOptionIdsJson { get; set; } // JSON array of selected GUIDs
    public string? TextResponse { get; set; }
    public bool IsCorrect { get; set; }
    public int ScoreAwarded { get; set; }
}
