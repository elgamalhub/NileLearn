namespace NileLearn.Domain.Enums;

public enum UserRoleType
{
    SuperAdmin = 1,
    Admin = 2,
    Instructor = 3,
    TeachingAssistant = 4,
    Student = 5,
    CustomerSupport = 6,
    FinanceManager = 7
}

public enum CourseStatus
{
    Draft = 1,
    UnderReview = 2,
    Published = 3,
    Scheduled = 4,
    Archived = 5,
    Unpublished = 6
}

public enum CourseLevel
{
    AllLevels = 1,
    Beginner = 2,
    Intermediate = 3,
    Advanced = 4
}

public enum LessonType
{
    Video = 1,
    Article = 2,
    Quiz = 3,
    Assignment = 4,
    DownloadableResource = 5
}

public enum VideoStorageProvider
{
    CloudflareStream = 1,
    BunnyStream = 2,
    AwsS3 = 3,
    CloudflareR2 = 4,
    ExternalEmbed = 5,
    LocalSimulator = 6
}

public enum VideoProcessingStatus
{
    Created = 1,
    Uploading = 2,
    Uploaded = 3,
    Processing = 4,
    Ready = 5,
    Failed = 6
}

public enum BillingCycle
{
    Monthly = 1,
    Quarterly = 2,
    Annual = 3,
    Lifetime = 4
}

public enum SubscriptionStatus
{
    Trialing = 1,
    Active = 2,
    PastDue = 3,
    Paused = 4,
    Cancelled = 5,
    Expired = 6
}

public enum OrderStatus
{
    Pending = 1,
    Paid = 2,
    Failed = 3,
    Cancelled = 4,
    Refunded = 5,
    PartiallyRefunded = 6
}

public enum PaymentGatewayType
{
    Sandbox = 1,
    Paymob = 2,
    Fawry = 3,
    Kashier = 4,
    Stripe = 5
}

public enum PaymentMethodType
{
    Card = 1,
    Meeza = 2,
    MobileWallet = 3, // Vodafone Cash, Orange Money, Etisalat Cash, WE Pay
    FawryKioskCash = 4,
    BankTransfer = 5
}

public enum PaymentStatus
{
    Pending = 1,
    Authorized = 2,
    Paid = 3,
    Failed = 4,
    Expired = 5,
    Refunded = 6,
    PartiallyRefunded = 7
}

public enum QuestionType
{
    SingleChoice = 1,
    MultipleChoice = 2,
    TrueFalse = 3,
    ShortAnswer = 4
}

public enum EnrollmentSource
{
    DirectPurchase = 1,
    Subscription = 2,
    AdminGrant = 3,
    FreeCourse = 4
}
