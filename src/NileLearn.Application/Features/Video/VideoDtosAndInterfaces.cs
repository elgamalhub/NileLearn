using NileLearn.Application.Common.Models;
using NileLearn.Domain.Enums;

namespace NileLearn.Application.Features.Video;

public class InitUploadSessionRequest
{
    public Guid? LessonId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public long TotalSizeBytes { get; set; }
    public string? FileChecksumSha256 { get; set; }
    public int ChunkSizeBytes { get; set; } = 5 * 1024 * 1024; // 5MB default
}

public class UploadSessionStatusDto
{
    public Guid SessionId { get; set; }
    public Guid? LessonId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public long TotalSizeBytes { get; set; }
    public long UploadedSizeBytes { get; set; }
    public int TotalChunks { get; set; }
    public int UploadedChunks { get; set; }
    public double ProgressPercentage => TotalSizeBytes > 0 ? Math.Round((double)UploadedSizeBytes / TotalSizeBytes * 100, 2) : 0;
    public VideoProcessingStatus Status { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public Guid? ResultingVideoAssetId { get; set; }
}

public class UploadChunkRequest
{
    public Guid SessionId { get; set; }
    public int ChunkIndex { get; set; }
    public Stream ChunkStream { get; set; } = Stream.Null;
    public string? ChunkChecksum { get; set; }
}

public class PlaybackAuthorizationRequest
{
    public Guid LessonId { get; set; }
}

public class PlaybackAuthorizationResponse
{
    public Guid LessonId { get; set; }
    public string PlaybackToken { get; set; } = string.Empty;
    public string StreamUrl { get; set; } = string.Empty;
    public string StreamType { get; set; } = "HLS"; // "HLS", "DASH", "MP4"
    public List<VideoQualityDto> Qualities { get; set; } = new();
    public int LastWatchedPositionSeconds { get; set; }
    public int DurationSeconds { get; set; }
    public string WatermarkText { get; set; } = string.Empty;
    public DateTime TokenExpiresAtUtc { get; set; }
}

public class VideoQualityDto
{
    public string Label { get; set; } = "1080p";
    public int BitrateKbps { get; set; }
    public string Url { get; set; } = string.Empty;
}

public class PlaybackHeartbeatRequest
{
    public Guid LessonId { get; set; }
    public string PlaybackToken { get; set; } = string.Empty;
    public int CurrentPositionSeconds { get; set; }
    public int TotalDurationSeconds { get; set; }
    public bool MarkCompletedIfEligible { get; set; }
}

public class PlaybackHeartbeatResponse
{
    public bool IsValid { get; set; }
    public bool IsLessonCompleted { get; set; }
    public int ProgressPercent { get; set; }
}

public interface IVideoUploadService
{
    Task<ApiResponse<UploadSessionStatusDto>> InitializeUploadSessionAsync(InitUploadSessionRequest request, Guid userId, CancellationToken ct = default);
    Task<ApiResponse<UploadSessionStatusDto>> UploadChunkAsync(Guid sessionId, int chunkIndex, Stream chunkStream, string? checksum, CancellationToken ct = default);
    Task<ApiResponse<UploadSessionStatusDto>> CompleteUploadSessionAsync(Guid sessionId, CancellationToken ct = default);
    Task<ApiResponse<UploadSessionStatusDto>> GetUploadStatusAsync(Guid sessionId, CancellationToken ct = default);
    Task<ApiResponse<bool>> CancelUploadSessionAsync(Guid sessionId, CancellationToken ct = default);
}

public interface IVideoPlaybackService
{
    Task<ApiResponse<PlaybackAuthorizationResponse>> AuthorizePlaybackAsync(Guid userId, Guid lessonId, string ipAddress, string userAgent, CancellationToken ct = default);
    Task<ApiResponse<PlaybackHeartbeatResponse>> RecordHeartbeatAsync(Guid userId, PlaybackHeartbeatRequest request, string ipAddress, CancellationToken ct = default);
}

public interface IVideoStorageProvider
{
    VideoStorageProvider Provider { get; }
    Task<string> UploadRawChunkAsync(string storageKey, int chunkIndex, Stream data, CancellationToken ct = default);
    Task<string> FinalizeMultipartUploadAsync(string storageKey, int totalChunks, CancellationToken ct = default);
    Task<string> GenerateSignedPlaybackUrlAsync(string assetId, TimeSpan expiration, CancellationToken ct = default);
}
