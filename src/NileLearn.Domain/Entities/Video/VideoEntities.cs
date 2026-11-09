using NileLearn.Domain.Common;
using NileLearn.Domain.Entities.Identity;
using NileLearn.Domain.Enums;

namespace NileLearn.Domain.Entities.Video;

public class VideoAsset : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public VideoStorageProvider Provider { get; set; } = VideoStorageProvider.LocalSimulator;
    public string ProviderAssetId { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public long OriginalFileSizeBytes { get; set; }
    public int DurationSeconds { get; set; }
    public VideoProcessingStatus Status { get; set; } = VideoProcessingStatus.Created;

    // Delivery endpoints
    public string? HlsManifestUrl { get; set; }
    public string? DashManifestUrl { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string? ErrorMessage { get; set; }

    public DateTime? ProcessingCompletedAtUtc { get; set; }

    public virtual ICollection<VideoVariant> Variants { get; set; } = new List<VideoVariant>();
}

public class VideoVariant : BaseEntity
{
    public Guid VideoAssetId { get; set; }
    public virtual VideoAsset VideoAsset { get; set; } = null!;

    public string Resolution { get; set; } = "1080p"; // 1080p, 720p, 480p, 360p, 240p
    public int BitrateKbps { get; set; }
    public string StreamUrl { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
}

public class VideoUploadSession : BaseEntity
{
    public Guid? LessonId { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public long TotalSizeBytes { get; set; }
    public long UploadedSizeBytes { get; set; }
    public int ChunkSizeBytes { get; set; } = 5 * 1024 * 1024; // 5 MB chunks
    public int TotalChunks { get; set; }
    public int UploadedChunks { get; set; }
    public string? FileChecksumSha256 { get; set; }
    public VideoProcessingStatus Status { get; set; } = VideoProcessingStatus.Created;
    public string? UploadStorageKey { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }

    // Associated generated asset once finished
    public Guid? ResultingVideoAssetId { get; set; }
    public virtual VideoAsset? ResultingVideoAsset { get; set; }
}

public class VideoPlaybackSession : BaseEntity
{
    public Guid UserId { get; set; }
    public virtual ApplicationUser User { get; set; } = null!;

    public Guid LessonId { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public string PlaybackToken { get; set; } = string.Empty;
    public DateTime TokenExpiresAtUtc { get; set; }
    public DateTime LastHeartbeatAtUtc { get; set; } = DateTime.UtcNow;
    public int CurrentPositionSeconds { get; set; }
    public string WatermarkText { get; set; } = string.Empty;
}
