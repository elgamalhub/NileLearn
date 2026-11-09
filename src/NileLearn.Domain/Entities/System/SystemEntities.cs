using NileLearn.Domain.Common;
using NileLearn.Domain.Entities.Courses;
using NileLearn.Domain.Entities.Identity;

namespace NileLearn.Domain.Entities.System;

public class Review : BaseEntity
{
    public Guid CourseId { get; set; }
    public virtual Course Course { get; set; } = null!;

    public Guid UserId { get; set; }
    public virtual ApplicationUser User { get; set; } = null!;

    public int Rating { get; set; } // 1 - 5
    public string? Title { get; set; }
    public string Comment { get; set; } = string.Empty;
    public bool IsApproved { get; set; } = true;
}

public class Discussion : BaseEntity
{
    public Guid CourseId { get; set; }
    public virtual Course Course { get; set; } = null!;

    public Guid? LessonId { get; set; }
    public virtual Lesson? Lesson { get; set; }

    public Guid UserId { get; set; }
    public virtual ApplicationUser User { get; set; } = null!;

    public Guid? ParentDiscussionId { get; set; }
    public virtual Discussion? ParentDiscussion { get; set; }
    public virtual ICollection<Discussion> Replies { get; set; } = new List<Discussion>();

    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool IsResolved { get; set; }
}

public class Notification : BaseEntity
{
    public Guid UserId { get; set; }
    public virtual ApplicationUser User { get; set; } = null!;

    public string TitleEn { get; set; } = string.Empty;
    public string TitleAr { get; set; } = string.Empty;
    public string MessageEn { get; set; } = string.Empty;
    public string MessageAr { get; set; } = string.Empty;
    public string Type { get; set; } = "System"; // System, Course, Payment, Certificate, Security
    public string? ActionUrl { get; set; }
    public bool IsRead { get; set; }
}

public class AuditLog : BaseEntity
{
    public Guid? UserId { get; set; }
    public string? UserEmail { get; set; }
    public string Action { get; set; } = string.Empty; // Create, Update, Delete, Login, Payment
    public string EntityName { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string? OldValuesJson { get; set; }
    public string? NewValuesJson { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}

public class ApplicationSetting : BaseEntity
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsEncrypted { get; set; }
    public string Category { get; set; } = "General"; // General, Payment, Video, Security, Email
}
