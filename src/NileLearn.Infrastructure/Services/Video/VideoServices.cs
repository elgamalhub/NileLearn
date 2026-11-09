using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NileLearn.Application.Common.Interfaces;
using NileLearn.Application.Common.Models;
using NileLearn.Application.Features.Billing;
using NileLearn.Application.Features.Video;
using NileLearn.Domain.Entities.Learning;
using NileLearn.Domain.Entities.Video;
using NileLearn.Domain.Enums;

namespace NileLearn.Infrastructure.Services.Video;

public class VideoUploadService : IVideoUploadService
{
    private readonly IApplicationDbContext _context;
    private readonly string _uploadDirectory;

    public VideoUploadService(IApplicationDbContext context, IConfiguration config)
    {
        _context = context;
        _uploadDirectory = config["Storage:UploadPath"] ?? Path.Combine(Directory.GetCurrentDirectory(), "AppData", "Uploads");
        Directory.CreateDirectory(_uploadDirectory);
    }

    public async Task<ApiResponse<UploadSessionStatusDto>> InitializeUploadSessionAsync(InitUploadSessionRequest request, Guid userId, CancellationToken ct = default)
    {
        // Enforce max upload size (e.g. 5GB)
        const long maxSizeBytes = 5L * 1024 * 1024 * 1024;
        if (request.TotalSizeBytes <= 0 || request.TotalSizeBytes > maxSizeBytes)
        {
            return ApiResponse<UploadSessionStatusDto>.Fail("Invalid file size. Maximum size is 5GB.", "حجم الملف غير صالح. الحد الأقصى 5 جيجابايت.");
        }

        var chunkSizeBytes = request.ChunkSizeBytes > 0 ? request.ChunkSizeBytes : 5 * 1024 * 1024;
        var totalChunks = (int)Math.Ceiling((double)request.TotalSizeBytes / chunkSizeBytes);
        var storageKey = $"upl_{Guid.NewGuid():N}";

        var session = new VideoUploadSession
        {
            LessonId = request.LessonId,
            CreatedByUserId = userId,
            FileName = Path.GetFileName(request.FileName),
            TotalSizeBytes = request.TotalSizeBytes,
            ChunkSizeBytes = chunkSizeBytes,
            TotalChunks = totalChunks,
            UploadedChunks = 0,
            UploadedSizeBytes = 0,
            FileChecksumSha256 = request.FileChecksumSha256,
            Status = VideoProcessingStatus.Created,
            UploadStorageKey = storageKey,
            ExpiresAtUtc = DateTime.UtcNow.AddHours(24)
        };

        var sessionFolder = Path.Combine(_uploadDirectory, storageKey);
        Directory.CreateDirectory(sessionFolder);

        _context.VideoUploadSessions.Add(session);
        await _context.SaveChangesAsync(ct);

        return ApiResponse<UploadSessionStatusDto>.Ok(MapStatus(session));
    }

    public async Task<ApiResponse<UploadSessionStatusDto>> UploadChunkAsync(Guid sessionId, int chunkIndex, Stream chunkStream, string? checksum, CancellationToken ct = default)
    {
        var session = await _context.VideoUploadSessions.FirstOrDefaultAsync(s => s.Id == sessionId, ct);
        if (session == null) return ApiResponse<UploadSessionStatusDto>.Fail("Upload session not found.", "جلسة الرفع غير موجودة.");

        if (session.ExpiresAtUtc < DateTime.UtcNow)
        {
            session.Status = VideoProcessingStatus.Failed;
            await _context.SaveChangesAsync(ct);
            return ApiResponse<UploadSessionStatusDto>.Fail("Upload session expired.", "انتهت صلاحية جلسة الرفع.");
        }

        var sessionFolder = Path.Combine(_uploadDirectory, session.UploadStorageKey!);
        var chunkPath = Path.Combine(sessionFolder, $"chunk_{chunkIndex}.bin");

        using (var fileStream = new FileStream(chunkPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await chunkStream.CopyToAsync(fileStream, ct);
        }

        var chunkLength = new FileInfo(chunkPath).Length;
        session.UploadedSizeBytes += chunkLength;
        session.UploadedChunks++;
        session.Status = VideoProcessingStatus.Uploading;

        await _context.SaveChangesAsync(ct);

        return ApiResponse<UploadSessionStatusDto>.Ok(MapStatus(session));
    }

    public async Task<ApiResponse<UploadSessionStatusDto>> CompleteUploadSessionAsync(Guid sessionId, CancellationToken ct = default)
    {
        var session = await _context.VideoUploadSessions.FirstOrDefaultAsync(s => s.Id == sessionId, ct);
        if (session == null) return ApiResponse<UploadSessionStatusDto>.Fail("Upload session not found.", "جلسة الرفع غير موجودة.");

        var sessionFolder = Path.Combine(_uploadDirectory, session.UploadStorageKey!);
        var mergedFilePath = Path.Combine(sessionFolder, session.FileName);

        // Merge all chunks
        using (var outputStream = new FileStream(mergedFilePath, FileMode.Create, FileAccess.Write))
        {
            for (int i = 0; i < session.TotalChunks; i++)
            {
                var chunkPath = Path.Combine(sessionFolder, $"chunk_{i}.bin");
                if (File.Exists(chunkPath))
                {
                    using var inputStream = new FileStream(chunkPath, FileMode.Open, FileAccess.Read);
                    await inputStream.CopyToAsync(outputStream, ct);
                }
            }
        }

        // Clean up individual chunks
        for (int i = 0; i < session.TotalChunks; i++)
        {
            var chunkPath = Path.Combine(sessionFolder, $"chunk_{i}.bin");
            if (File.Exists(chunkPath)) File.Delete(chunkPath);
        }

        session.Status = VideoProcessingStatus.Processing;
        session.CompletedAtUtc = DateTime.UtcNow;

        // Create VideoAsset
        var asset = new VideoAsset
        {
            Title = Path.GetFileNameWithoutExtension(session.FileName),
            Provider = VideoStorageProvider.LocalSimulator,
            ProviderAssetId = session.UploadStorageKey!,
            OriginalFileName = session.FileName,
            OriginalFileSizeBytes = session.TotalSizeBytes,
            DurationSeconds = 1200, // E.g. 20 minutes estimated or probed
            Status = VideoProcessingStatus.Ready,
            HlsManifestUrl = $"/api/v1/videos/{session.UploadStorageKey}/master.m3u8",
            DashManifestUrl = $"/api/v1/videos/{session.UploadStorageKey}/manifest.mpd",
            ThumbnailUrl = "https://images.unsplash.com/photo-1516321318423-f06f85e504b3?w=800&auto=format&fit=crop",
            ProcessingCompletedAtUtc = DateTime.UtcNow
        };

        // Add standard resolutions
        asset.Variants.Add(new VideoVariant { Resolution = "1080p", BitrateKbps = 4500, StreamUrl = $"/api/v1/videos/{session.UploadStorageKey}/1080p.m3u8", FileSizeBytes = (long)(session.TotalSizeBytes * 1.0) });
        asset.Variants.Add(new VideoVariant { Resolution = "720p", BitrateKbps = 2500, StreamUrl = $"/api/v1/videos/{session.UploadStorageKey}/720p.m3u8", FileSizeBytes = (long)(session.TotalSizeBytes * 0.6) });
        asset.Variants.Add(new VideoVariant { Resolution = "480p", BitrateKbps = 1200, StreamUrl = $"/api/v1/videos/{session.UploadStorageKey}/480p.m3u8", FileSizeBytes = (long)(session.TotalSizeBytes * 0.35) });
        asset.Variants.Add(new VideoVariant { Resolution = "360p", BitrateKbps = 700, StreamUrl = $"/api/v1/videos/{session.UploadStorageKey}/360p.m3u8", FileSizeBytes = (long)(session.TotalSizeBytes * 0.2) });

        _context.VideoAssets.Add(asset);

        session.ResultingVideoAssetId = asset.Id;
        session.Status = VideoProcessingStatus.Ready;

        // If linked to lesson, attach
        if (session.LessonId.HasValue)
        {
            var lesson = await _context.Lessons.FindAsync(new object[] { session.LessonId.Value }, ct);
            if (lesson != null)
            {
                lesson.VideoAssetId = asset.Id;
                lesson.DurationSeconds = asset.DurationSeconds;
            }
        }

        await _context.SaveChangesAsync(ct);
        return ApiResponse<UploadSessionStatusDto>.Ok(MapStatus(session));
    }

    public async Task<ApiResponse<UploadSessionStatusDto>> GetUploadStatusAsync(Guid sessionId, CancellationToken ct = default)
    {
        var session = await _context.VideoUploadSessions.FirstOrDefaultAsync(s => s.Id == sessionId, ct);
        if (session == null) return ApiResponse<UploadSessionStatusDto>.Fail("Upload session not found.", "جلسة الرفع غير موجودة.");

        return ApiResponse<UploadSessionStatusDto>.Ok(MapStatus(session));
    }

    public async Task<ApiResponse<bool>> CancelUploadSessionAsync(Guid sessionId, CancellationToken ct = default)
    {
        var session = await _context.VideoUploadSessions.FirstOrDefaultAsync(s => s.Id == sessionId, ct);
        if (session == null) return ApiResponse<bool>.Fail("Upload session not found.", "جلسة الرفع غير موجودة.");

        var sessionFolder = Path.Combine(_uploadDirectory, session.UploadStorageKey!);
        if (Directory.Exists(sessionFolder))
        {
            Directory.Delete(sessionFolder, true);
        }

        session.Status = VideoProcessingStatus.Failed;
        await _context.SaveChangesAsync(ct);

        return ApiResponse<bool>.Ok(true, "Upload cancelled.", "تم إلغاء الرفع بنجاح.");
    }

    private static UploadSessionStatusDto MapStatus(VideoUploadSession session)
    {
        return new UploadSessionStatusDto
        {
            SessionId = session.Id,
            LessonId = session.LessonId,
            FileName = session.FileName,
            TotalSizeBytes = session.TotalSizeBytes,
            UploadedSizeBytes = session.UploadedSizeBytes,
            TotalChunks = session.TotalChunks,
            UploadedChunks = session.UploadedChunks,
            Status = session.Status,
            ExpiresAtUtc = session.ExpiresAtUtc,
            ResultingVideoAssetId = session.ResultingVideoAssetId
        };
    }
}

public class VideoPlaybackService : IVideoPlaybackService
{
    private readonly IApplicationDbContext _context;
    private readonly IEntitlementService _entitlementService;
    private readonly IConfiguration _config;

    public VideoPlaybackService(
        IApplicationDbContext context,
        IEntitlementService entitlementService,
        IConfiguration config)
    {
        _context = context;
        _entitlementService = entitlementService;
        _config = config;
    }

    public async Task<ApiResponse<PlaybackAuthorizationResponse>> AuthorizePlaybackAsync(
        Guid userId,
        Guid lessonId,
        string ipAddress,
        string userAgent,
        CancellationToken ct = default)
    {
        var lesson = await _context.Lessons
            .Include(l => l.VideoAsset).ThenInclude(v => v!.Variants)
            .Include(l => l.Section)
            .FirstOrDefaultAsync(l => l.Id == lessonId, ct);

        if (lesson == null) return ApiResponse<PlaybackAuthorizationResponse>.Fail("Lesson not found.", "الدرس غير موجود.");

        // Check entitlement
        var hasAccess = await _entitlementService.HasAccessToLessonAsync(userId, lessonId, ct);
        if (!hasAccess)
        {
            return ApiResponse<PlaybackAuthorizationResponse>.Fail("Access denied. Please purchase the course or subscribe to a plan.", "تم رفض الوصول. يرجى شراء الدورة أو الاشتراك في إحدى الباقات.");
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        var userEmail = user?.Email ?? "student@nilelearn.com";

        // Generate short-lived playback token (valid 20 minutes)
        var secret = _config["Jwt:SecretKey"] ?? "dev-only-playback-secret-min-32-characters";
        var expiresAtUtc = DateTime.UtcNow.AddMinutes(20);
        var rawPayload = $"{userId}|{lessonId}|{ipAddress}|{expiresAtUtc.Ticks}";

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var signature = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(rawPayload)));
        var playbackToken = $"{Convert.ToBase64String(Encoding.UTF8.GetBytes(rawPayload))}.{signature}";

        // Session watermark overlay text (student email + dynamic timestamp code)
        var watermarkSessionCode = Guid.NewGuid().ToString("N")[..8].ToUpper();
        var watermarkText = $"{userEmail} • Session #{watermarkSessionCode} • NileLearn";

        // Record playback session for device/concurrent streaming tracking
        var playbackSession = new VideoPlaybackSession
        {
            UserId = userId,
            LessonId = lessonId,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            PlaybackToken = playbackToken,
            TokenExpiresAtUtc = expiresAtUtc,
            WatermarkText = watermarkText
        };
        _context.VideoPlaybackSessions.Add(playbackSession);

        // Fetch last saved position
        var progress = await _context.LearningProgress
            .FirstOrDefaultAsync(p => p.UserId == userId && p.LessonId == lessonId, ct);
        var lastWatchedPosition = progress?.LastWatchedPositionSeconds ?? 0;

        await _context.SaveChangesAsync(ct);

        // Fallback demo video stream URL for playback test
        var streamUrl = lesson.VideoAsset?.HlsManifestUrl
            ?? "https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4";

        var qualities = lesson.VideoAsset?.Variants.Select(v => new VideoQualityDto
        {
            Label = v.Resolution,
            BitrateKbps = v.BitrateKbps,
            Url = v.StreamUrl
        }).ToList() ?? new List<VideoQualityDto>
        {
            new() { Label = "1080p", BitrateKbps = 4500, Url = streamUrl },
            new() { Label = "720p", BitrateKbps = 2500, Url = streamUrl },
            new() { Label = "480p", BitrateKbps = 1200, Url = streamUrl }
        };

        var response = new PlaybackAuthorizationResponse
        {
            LessonId = lessonId,
            PlaybackToken = playbackToken,
            StreamUrl = streamUrl,
            StreamType = streamUrl.EndsWith(".mp4") ? "MP4" : "HLS",
            Qualities = qualities,
            LastWatchedPositionSeconds = lastWatchedPosition,
            DurationSeconds = lesson.DurationSeconds > 0 ? lesson.DurationSeconds : 1200,
            WatermarkText = watermarkText,
            TokenExpiresAtUtc = expiresAtUtc
        };

        return ApiResponse<PlaybackAuthorizationResponse>.Ok(response);
    }

    public async Task<ApiResponse<PlaybackHeartbeatResponse>> RecordHeartbeatAsync(
        Guid userId,
        PlaybackHeartbeatRequest request,
        string ipAddress,
        CancellationToken ct = default)
    {
        var lesson = await _context.Lessons
            .Include(l => l.Section)
            .FirstOrDefaultAsync(l => l.Id == request.LessonId, ct);

        if (lesson == null) return ApiResponse<PlaybackHeartbeatResponse>.Fail("Lesson not found.", "الدرس غير موجود.");

        var progress = await _context.LearningProgress
            .FirstOrDefaultAsync(p => p.UserId == userId && p.LessonId == request.LessonId, ct);

        if (progress == null)
        {
            progress = new LearningProgress
            {
                UserId = userId,
                CourseId = lesson.Section.CourseId,
                LessonId = request.LessonId,
                LastWatchedPositionSeconds = request.CurrentPositionSeconds,
                TotalWatchSeconds = 10,
                LastActiveAtUtc = DateTime.UtcNow
            };
            _context.LearningProgress.Add(progress);
        }
        else
        {
            progress.LastWatchedPositionSeconds = request.CurrentPositionSeconds;
            progress.TotalWatchSeconds += 10;
            progress.LastActiveAtUtc = DateTime.UtcNow;
        }

        // Completion validation: watched >= 80% of total duration or marked complete
        var totalDuration = request.TotalDurationSeconds > 0 ? request.TotalDurationSeconds : lesson.DurationSeconds;
        var hasWatchedSufficiently = totalDuration > 0 && ((double)request.CurrentPositionSeconds / totalDuration >= 0.8 || progress.TotalWatchSeconds >= totalDuration * 0.7);

        if ((request.MarkCompletedIfEligible || hasWatchedSufficiently) && !progress.IsCompleted)
        {
            progress.IsCompleted = true;
            progress.CompletedAtUtc = DateTime.UtcNow;
        }

        // Update overall course progress
        var totalLessonsInCourse = await _context.Lessons
            .CountAsync(l => l.Section.CourseId == lesson.Section.CourseId, ct);

        var completedLessonsCount = await _context.LearningProgress
            .CountAsync(p => p.UserId == userId && p.CourseId == lesson.Section.CourseId && p.IsCompleted, ct);

        var progressPercent = totalLessonsInCourse > 0
            ? (int)Math.Round((double)completedLessonsCount / totalLessonsInCourse * 100)
            : 0;

        var enrollment = await _context.Enrollments
            .FirstOrDefaultAsync(e => e.UserId == userId && e.CourseId == lesson.Section.CourseId, ct);

        if (enrollment != null)
        {
            enrollment.ProgressPercent = progressPercent;
            enrollment.LastAccessedAtUtc = DateTime.UtcNow;
            if (progressPercent >= 100 && !enrollment.CompletedAtUtc.HasValue)
            {
                enrollment.CompletedAtUtc = DateTime.UtcNow;
            }
        }

        await _context.SaveChangesAsync(ct);

        return ApiResponse<PlaybackHeartbeatResponse>.Ok(new PlaybackHeartbeatResponse
        {
            IsValid = true,
            IsLessonCompleted = progress.IsCompleted,
            ProgressPercent = progressPercent
        });
    }
}
