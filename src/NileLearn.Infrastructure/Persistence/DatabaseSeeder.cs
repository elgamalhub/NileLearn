using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NileLearn.Domain.Entities.Assessments;
using NileLearn.Domain.Entities.Billing;
using NileLearn.Domain.Entities.Courses;
using NileLearn.Domain.Entities.Identity;
using NileLearn.Domain.Entities.Payments;
using NileLearn.Domain.Entities.System;
using NileLearn.Domain.Entities.Video;
using NileLearn.Domain.Enums;

namespace NileLearn.Infrastructure.Persistence;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager)
    {
        // 1. Seed Roles
        var roles = Enum.GetValues<UserRoleType>();
        foreach (var role in roles)
        {
            var roleName = role.ToString();
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new ApplicationRole(roleName)
                {
                    Description = $"Role for {roleName}",
                    DescriptionAr = $"دور {roleName}"
                });
            }
        }

        // 2. Seed Default Users
        // Super Admin
        var adminEmail = "superadmin@nilelearn.com";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FullName = "NileLearn Super Admin",
                PhoneNumber = "+201001234567",
                EmailConfirmed = true,
                PreferredLanguage = "ar",
                ProfilePictureUrl = "https://images.unsplash.com/photo-1534528741775-53994a69daeb?w=400&auto=format&fit=crop"
            };
            await userManager.CreateAsync(adminUser, "AdminPassword123!");
            await userManager.AddToRoleAsync(adminUser, UserRoleType.SuperAdmin.ToString());
            await userManager.AddToRoleAsync(adminUser, UserRoleType.Admin.ToString());
        }

        // Instructor
        var instructorEmail = "instructor@nilelearn.com";
        var instructorUser = await userManager.FindByEmailAsync(instructorEmail);
        if (instructorUser == null)
        {
            instructorUser = new ApplicationUser
            {
                UserName = instructorEmail,
                Email = instructorEmail,
                FullName = "د. طارق مصطفى (Dr. Tarek Mostafa)",
                PhoneNumber = "+201112345678",
                EmailConfirmed = true,
                PreferredLanguage = "ar",
                Headline = "Senior Software Architect & .NET Specialist",
                Bio = "15+ years experience building mission-critical distributed systems and enterprise architectures across the Middle East.",
                ProfilePictureUrl = "https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?w=400&auto=format&fit=crop"
            };
            await userManager.CreateAsync(instructorUser, "Instructor123!");
            await userManager.AddToRoleAsync(instructorUser, UserRoleType.Instructor.ToString());

            var instructorProfile = new InstructorProfile
            {
                UserId = instructorUser.Id,
                TitleEn = "Principal Architect & Microsoft MVP",
                TitleAr = "مهندس معماري برمجيات وخبير مايكروسوفت",
                BiographyEn = "Senior Consultant specializing in high-load cloud architectures, ASP.NET Core, and distributed systems.",
                BiographyAr = "استشاري أول متخصص في معمارية الأنظمة السحابية عالية التحمل و ASP.NET Core.",
                AverageRating = 4.95,
                TotalStudentsCount = 14500,
                TotalReviewsCount = 3800,
                RevenueSharePercentage = 75.0m
            };
            context.InstructorProfiles.Add(instructorProfile);
            await context.SaveChangesAsync();
        }

        // Student
        var studentEmail = "student@nilelearn.com";
        var studentUser = await userManager.FindByEmailAsync(studentEmail);
        if (studentUser == null)
        {
            studentUser = new ApplicationUser
            {
                UserName = studentEmail,
                Email = studentEmail,
                FullName = "أحمد محمود (Ahmed Mahmoud)",
                PhoneNumber = "+201223456789",
                EmailConfirmed = true,
                PreferredLanguage = "ar",
                Headline = "Full-Stack Software Engineering Enthusiast",
                ProfilePictureUrl = "https://images.unsplash.com/photo-1500648767791-00dcc994a43e?w=400&auto=format&fit=crop"
            };
            await userManager.CreateAsync(studentUser, "Student123!");
            await userManager.AddToRoleAsync(studentUser, UserRoleType.Student.ToString());
        }

        // 3. Seed Categories
        if (!await context.Categories.AnyAsync())
        {
            var catNet = new Category
            {
                NameEn = "Backend & Cloud Architecture",
                NameAr = "تطوير الواجهات الخلفية والأنظمة السحابية",
                Slug = "backend-cloud",
                DescriptionEn = "Master modern C#, ASP.NET Core Web API, Microservices, and SQL Server.",
                DescriptionAr = "احترف لغة سي شارب وأحدث تقنيات ASP.NET Core وقواعد البيانات المتقدمة.",
                IconUrl = "terminal",
                DisplayOrder = 1
            };

            var catFrontend = new Category
            {
                NameEn = "Frontend & Angular Development",
                NameAr = "تطوير واجهات المستخدم وأنجولار",
                Slug = "frontend-angular",
                DescriptionEn = "Build fast, reactive web apps with Angular, TypeScript, and modern design systems.",
                DescriptionAr = "بناء تطبيقات ويب فائقة الأداء باستخدام إطار عمل Angular وأحدث أنماط التصميم.",
                IconUrl = "code",
                DisplayOrder = 2
            };

            var catDevOps = new Category
            {
                NameEn = "DevOps & Cloud Infrastructure",
                NameAr = "الديف أوبس والبنية التحتية السحابية",
                Slug = "devops-cloud",
                DescriptionEn = "Docker, Kubernetes, CI/CD pipelines, and cloud monitoring at scale.",
                DescriptionAr = "دوكر، كوبرنيتس، خطوط التكامل والنشر المستمر وإدارة الخوادم السحابية.",
                IconUrl = "cloud",
                DisplayOrder = 3
            };

            context.Categories.AddRange(catNet, catFrontend, catDevOps);
            await context.SaveChangesAsync();

            // 4. Seed Courses
            // Course 1: ASP.NET Core 8 & Clean Architecture
            var sampleVideoAsset = new VideoAsset
            {
                Title = "ASP.NET Core Architecture Masterclass Demo",
                Provider = VideoStorageProvider.LocalSimulator,
                ProviderAssetId = "demo_asset_001",
                OriginalFileName = "aspnet_masterclass.mp4",
                OriginalFileSizeBytes = 125 * 1024 * 1024,
                DurationSeconds = 1200,
                Status = VideoProcessingStatus.Ready,
                HlsManifestUrl = "https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4",
                ThumbnailUrl = "https://images.unsplash.com/photo-1517694712202-14dd9538aa97?w=800&auto=format&fit=crop"
            };
            sampleVideoAsset.Variants.Add(new VideoVariant { Resolution = "1080p", BitrateKbps = 4500, StreamUrl = sampleVideoAsset.HlsManifestUrl, FileSizeBytes = 125 * 1024 * 1024 });
            sampleVideoAsset.Variants.Add(new VideoVariant { Resolution = "720p", BitrateKbps = 2500, StreamUrl = sampleVideoAsset.HlsManifestUrl, FileSizeBytes = 75 * 1024 * 1024 });
            context.VideoAssets.Add(sampleVideoAsset);
            await context.SaveChangesAsync();

            var course1 = new Course
            {
                TitleEn = "Enterprise ASP.NET Core 8 & Clean Architecture",
                TitleAr = "هندسة البرمجيات المتقدمة و ASP.NET Core 8 بنظام Clean Architecture",
                Slug = "enterprise-aspnet-core-clean-architecture",
                ShortDescriptionEn = "Build scalable, production-ready enterprise backends with .NET 8 LTS, EF Core, and Docker.",
                ShortDescriptionAr = "دورة احترافية لبناء أنظمة ويب خلفية عملاقة وقابلة للتوسع باستخدام أحدث معايير هندسة البرمجيات.",
                FullDescriptionEn = "In this comprehensive masterclass, you will learn how to design, architect, and implement high-performance web APIs with clean separation of concerns, CQRS patterns, entity validation, and Egyptian payment integrations.",
                FullDescriptionAr = "دليل عملي شامل لتصميم وتطوير واجهات برمجة التطبيقات الاحترافية، بدءاً من هيكلة الطبقات وصولاً إلى بوابات الدفع الإلكترونية والحماية المتقدمة.",
                ThumbnailUrl = "https://images.unsplash.com/photo-1517694712202-14dd9538aa97?w=800&auto=format&fit=crop",
                CoverImageUrl = "https://images.unsplash.com/photo-1526374965328-7f61d4dc18c5?w=1600&auto=format&fit=crop",
                Level = CourseLevel.Advanced,
                Status = CourseStatus.Published,
                Price = 1200.00m,
                Currency = "EGP",
                DiscountPrice = 799.00m,
                DiscountExpiresAtUtc = DateTime.UtcNow.AddDays(30),
                IsFree = false,
                CategoryId = catNet.Id,
                PreviewVideoAssetId = sampleVideoAsset.Id,
                AverageRating = 4.9,
                TotalReviews = 420,
                TotalEnrollments = 1850,
                TotalDurationSeconds = 7200,
                PublishedAtUtc = DateTime.UtcNow.AddMonths(-2),
                PrerequisitesJson = JsonSerializer.Serialize(new[] { "Basic C# knowledge", "Familiarity with Object-Oriented Programming" }),
                LearningOutcomesJson = JsonSerializer.Serialize(new[] { "Architect enterprise .NET solutions", "Implement Egyptian payment gateways (Paymob, Fawry)", "Deploy with Docker & CI/CD" })
            };

            course1.Instructors.Add(new CourseInstructor { Course = course1, InstructorId = instructorUser.Id, IsPrimaryInstructor = true });

            var sec1 = new CourseSection { Course = course1, TitleEn = "1. Architectural Foundations", TitleAr = "1. الأسس المعمارية ومبادئ التصميم", SortOrder = 1 };
            sec1.Lessons.Add(new Lesson
            {
                Section = sec1,
                TitleEn = "Welcome & Architecture Overview",
                TitleAr = "مقدمة الدورة ونظرة عامة على المعمارية",
                Type = LessonType.Video,
                SortOrder = 1,
                DurationSeconds = 600,
                IsFreePreview = true,
                VideoAssetId = sampleVideoAsset.Id
            });
            sec1.Lessons.Add(new Lesson
            {
                Section = sec1,
                TitleEn = "Domain-Driven Design (DDD) Core Principles",
                TitleAr = "مبادئ التصميم القائم على نطاق العمل (DDD)",
                Type = LessonType.Video,
                SortOrder = 2,
                DurationSeconds = 1200,
                IsFreePreview = false,
                VideoAssetId = sampleVideoAsset.Id
            });

            var sec2 = new CourseSection { Course = course1, TitleEn = "2. Security & Payment Processing", TitleAr = "2. أمن المعلومات وتكامل بوابات الدفع", SortOrder = 2 };
            var lessonQuiz = new Lesson
            {
                Section = sec2,
                TitleEn = "Knowledge Check: Security & Architecture",
                TitleAr = "اختبار قياس المعرفة: الأمان والمعمارية",
                Type = LessonType.Quiz,
                SortOrder = 1,
                DurationSeconds = 900
            };
            sec2.Lessons.Add(lessonQuiz);

            course1.Sections.Add(sec1);
            course1.Sections.Add(sec2);
            context.Courses.Add(course1);
            await context.SaveChangesAsync();

            // Seed Quiz on lessonQuiz
            var quiz = new Quiz
            {
                LessonId = lessonQuiz.Id,
                TitleEn = "Architecture & Clean Code Certification Quiz",
                TitleAr = "اختبار التحقق من فهم المعمارية والبرمجة النظيفة",
                TimeLimitMinutes = 20,
                PassingScorePercentage = 70,
                MaxAttempts = 3
            };

            var q1 = new Question
            {
                Quiz = quiz,
                Type = QuestionType.SingleChoice,
                TextEn = "In Clean Architecture, which layer should NEVER have references to external libraries or databases?",
                TextAr = "في معمارية Clean Architecture، أي طبقة يجب ألا تحتوي إطلاقاً على أي مراجع لقواعد البيانات أو المكتبات الخارجية؟",
                Points = 5,
                SortOrder = 1
            };
            q1.AnswerOptions.Add(new AnswerOption { Question = q1, TextEn = "Domain Layer", TextAr = "طبقة النطاق (Domain)", IsCorrect = true, SortOrder = 1 });
            q1.AnswerOptions.Add(new AnswerOption { Question = q1, TextEn = "Infrastructure Layer", TextAr = "طبقة البنية التحتية (Infrastructure)", IsCorrect = false, SortOrder = 2 });
            q1.AnswerOptions.Add(new AnswerOption { Question = q1, TextEn = "API Layer", TextAr = "طبقة واجهة البرمجة (API)", IsCorrect = false, SortOrder = 3 });

            var q2 = new Question
            {
                Quiz = quiz,
                Type = QuestionType.SingleChoice,
                TextEn = "Why must payment gateway webhooks use HMAC cryptographic signatures?",
                TextAr = "لماذا يجب على إشعارات الويب هوك لبوابات الدفع استخدام توقيعات HMAC المشفرة؟",
                Points = 5,
                SortOrder = 2
            };
            q2.AnswerOptions.Add(new AnswerOption { Question = q2, TextEn = "To prevent tampering and verify authenticity of origin", TextAr = "لمنع التلاعب والتأكد التام من هوية المرسل ومصداقية المعاملة", IsCorrect = true, SortOrder = 1 });
            q2.AnswerOptions.Add(new AnswerOption { Question = q2, TextEn = "To make the HTTP request run faster", TextAr = "لتسريع استجابة الخادم", IsCorrect = false, SortOrder = 2 });

            quiz.Questions.Add(q1);
            quiz.Questions.Add(q2);
            context.Quizzes.Add(quiz);
            await context.SaveChangesAsync();

            // Course 2: Full-Stack Angular with Standalone Components
            var course2 = new Course
            {
                TitleEn = "Production Angular 19/20: From Zero to Hero",
                TitleAr = "احتراف تطوير واجهات الويب مع أنجولار الحديثة: من الصفر حتى الاحتراف",
                Slug = "production-angular-zero-to-hero",
                ShortDescriptionEn = "Build high-performance, accessible Angular SPAs with Standalone Components and Signals.",
                ShortDescriptionAr = "تعلم بناء تطبيقات ويب تفاعلية حديثة بأعلى معايير السرعة وسهولة الاستخدام مع أنجولار وسيجنالز.",
                FullDescriptionEn = "Comprehensive deep dive into modern Angular architecture, reactive state, internationalization with RTL support, and custom video player integration.",
                FullDescriptionAr = "دورة تطبيقية شاملة تتناول إدارة الحالة التفاعلية، دعم اللغتين العربية والإنجليزية بشكل كامل، وبناء مشغل فيديو مخصص.",
                ThumbnailUrl = "https://images.unsplash.com/photo-1555066931-4365d14bab8c?w=800&auto=format&fit=crop",
                CoverImageUrl = "https://images.unsplash.com/photo-1518770660439-4636190af475?w=1600&auto=format&fit=crop",
                Level = CourseLevel.Intermediate,
                Status = CourseStatus.Published,
                Price = 950.00m,
                Currency = "EGP",
                DiscountPrice = 649.00m,
                IsFree = false,
                CategoryId = catFrontend.Id,
                AverageRating = 4.88,
                TotalReviews = 310,
                TotalEnrollments = 1240,
                TotalDurationSeconds = 5400,
                PublishedAtUtc = DateTime.UtcNow.AddMonths(-1)
            };
            course2.Instructors.Add(new CourseInstructor { Course = course2, InstructorId = instructorUser.Id, IsPrimaryInstructor = true });

            var secA = new CourseSection { Course = course2, TitleEn = "1. Angular Architecture & Signals", TitleAr = "1. معمارية أنجولار ونظام الإشارات", SortOrder = 1 };
            secA.Lessons.Add(new Lesson
            {
                Section = secA,
                TitleEn = "Introduction to Standalone Components",
                TitleAr = "مقدمة إلى المكونات المستقلة (Standalone Components)",
                Type = LessonType.Video,
                SortOrder = 1,
                DurationSeconds = 800,
                IsFreePreview = true,
                VideoAssetId = sampleVideoAsset.Id
            });
            course2.Sections.Add(secA);
            context.Courses.Add(course2);
            await context.SaveChangesAsync();
        }

        // 5. Seed Subscription Plans
        if (!await context.SubscriptionPlans.AnyAsync())
        {
            var planFree = new SubscriptionPlan
            {
                NameEn = "Free Starter",
                NameAr = "الباقة المجانية",
                Slug = "free-starter",
                DescriptionEn = "Access free introductory courses and platform preview.",
                DescriptionAr = "وصول مجاني للدورات التمهيدية ومحتوى المنصة الأساسي.",
                MonthlyPriceEgp = 0.00m,
                AnnualPriceEgp = 0.00m,
                Currency = "EGP",
                MaxSimultaneousDevices = 1,
                AllowDownloads = false,
                TrialPeriodDays = 0,
                DisplayOrder = 1,
                FeaturesJsonEn = JsonSerializer.Serialize(new[] { "Access to all Free preview courses", "Community forum access", "1 simultaneous device" }),
                FeaturesJsonAr = JsonSerializer.Serialize(new[] { "وصول مجاني لجميع الدورات المفتوحة", "المشاركة في مجتمع النقاشات", "جهاز واحد نشط" })
            };

            var planPro = new SubscriptionPlan
            {
                NameEn = "Pro Learner",
                NameAr = "الباقة الاحترافية (Pro)",
                Slug = "pro-learner",
                DescriptionEn = "Unlimited access to all backend, frontend, and cloud courses.",
                DescriptionAr = "وصول غير محدود لجميع دورات المنصة والمسارات التعليمية المتقدمة.",
                MonthlyPriceEgp = 299.00m,
                AnnualPriceEgp = 2990.00m,
                Currency = "EGP",
                MaxSimultaneousDevices = 2,
                AllowDownloads = true,
                TrialPeriodDays = 7,
                DisplayOrder = 2,
                FeaturesJsonEn = JsonSerializer.Serialize(new[] { "Unlimited access to 50+ courses", "Full HD video streaming with zero ads", "Downloadable source code & slides", "Official verified completion certificates", "2 simultaneous devices" }),
                FeaturesJsonAr = JsonSerializer.Serialize(new[] { "وصول غير محدود لأكثر من 50 دورة تدريبية", "مشاهدة فائقة الجودة بدون إعلانات", "تحميل الأكواد والملفات والمشاريع الكاملة", "شهادات إتمام معتمدة برمز تحقق فريد", "جهازين في نفس الوقت" })
            };

            var planUltimate = new SubscriptionPlan
            {
                NameEn = "Ultimate Master",
                NameAr = "الباقة الشاملة (Master)",
                Slug = "ultimate-master",
                DescriptionEn = "The ultimate package with direct instructor mentorship and career coaching.",
                DescriptionAr = "الباقة المتكاملة مع إرشاد مهني مباشر وجلسات مراجعة للمشاريع البرمجية.",
                MonthlyPriceEgp = 599.00m,
                AnnualPriceEgp = 5990.00m,
                Currency = "EGP",
                MaxSimultaneousDevices = 4,
                AllowDownloads = true,
                TrialPeriodDays = 14,
                DisplayOrder = 3,
                FeaturesJsonEn = JsonSerializer.Serialize(new[] { "Everything in Pro Plan", "Direct 1-on-1 code review with senior architects", "Exclusive Discord masterclass community", "Resume review & interview preparation", "4 simultaneous devices" }),
                FeaturesJsonAr = JsonSerializer.Serialize(new[] { "جميع مميزات باقة Pro الاحترافية", "مراجعة كود فردية مع كبار مهندسي البرمجيات", "مجتمع ديسكورد حصري للخبراء", "تأهيل للمقابلات الفنية ومراجعة السيرة الذاتية", "4 أجهزة في نفس الوقت" })
            };

            context.SubscriptionPlans.AddRange(planFree, planPro, planUltimate);
            await context.SaveChangesAsync();

            // Give the seeded student an active Pro subscription for instant testing
            if (studentUser != null)
            {
                var studentSub = new Subscription
                {
                    UserId = studentUser.Id,
                    PlanId = planPro.Id,
                    Status = SubscriptionStatus.Active,
                    CurrentPeriodStartUtc = DateTime.UtcNow.AddDays(-10),
                    CurrentPeriodEndUtc = DateTime.UtcNow.AddDays(20),
                    AutoRenew = true
                };
                context.Subscriptions.Add(studentSub);
                await context.SaveChangesAsync();
            }
        }

        // 6. Seed Coupons
        if (!await context.Coupons.AnyAsync())
        {
            var coupon1 = new Coupon
            {
                Code = "EGYPT2026",
                DiscountPercent = 25.00m,
                ValidFromUtc = DateTime.UtcNow.AddDays(-30),
                ValidToUtc = DateTime.UtcNow.AddMonths(12),
                MaxRedemptions = 500,
                IsActive = true
            };

            var coupon2 = new Coupon
            {
                Code = "WELCOME100",
                FixedDiscountAmount = 100.00m,
                ValidFromUtc = DateTime.UtcNow.AddDays(-30),
                ValidToUtc = DateTime.UtcNow.AddMonths(12),
                MaxRedemptions = 1000,
                IsActive = true
            };

            context.Coupons.AddRange(coupon1, coupon2);
            await context.SaveChangesAsync();
        }

        // 7. Seed Default Application Settings
        if (!await context.ApplicationSettings.AnyAsync())
        {
            context.ApplicationSettings.AddRange(
                new ApplicationSetting { Key = "SiteNameEn", Value = "NileLearn LMS", Category = "General" },
                new ApplicationSetting { Key = "SiteNameAr", Value = "منصة نيل ليرن التعليمية", Category = "General" },
                new ApplicationSetting { Key = "PrimaryCurrency", Value = "EGP", Category = "Payment" },
                new ApplicationSetting { Key = "PaymobEnabled", Value = "true", Category = "Payment" },
                new ApplicationSetting { Key = "FawryEnabled", Value = "true", Category = "Payment" },
                new ApplicationSetting { Key = "SandboxEnabled", Value = "true", Category = "Payment" },
                new ApplicationSetting { Key = "WatermarkOverlayEnabled", Value = "true", Category = "Video" }
            );
            await context.SaveChangesAsync();
        }
    }
}
