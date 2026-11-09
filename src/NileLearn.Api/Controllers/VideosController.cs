using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NileLearn.Application.Common.Models;
using NileLearn.Application.Features.Video;

namespace NileLearn.Api.Controllers;

public class VideosController : BaseApiController
{
    private readonly IVideoUploadService _uploadService;
    private readonly IVideoPlaybackService _playbackService;

    public VideosController(IVideoUploadService uploadService, IVideoPlaybackService playbackService)
    {
        _uploadService = uploadService;
        _playbackService = playbackService;
    }

    [Authorize(Roles = "Instructor,Admin,SuperAdmin")]
    [HttpPost("upload/init")]
    public async Task<ActionResult<ApiResponse<UploadSessionStatusDto>>> InitUpload([FromBody] InitUploadSessionRequest request, CancellationToken ct)
    {
        var result = await _uploadService.InitializeUploadSessionAsync(request, CurrentUserId, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [Authorize(Roles = "Instructor,Admin,SuperAdmin")]
    [HttpPost("upload/{sessionId:guid}/chunk/{chunkIndex:int}")]
    [DisableRequestSizeLimit]
    public async Task<ActionResult<ApiResponse<UploadSessionStatusDto>>> UploadChunk(Guid sessionId, int chunkIndex, CancellationToken ct)
    {
        var checksum = Request.Headers["X-Chunk-Checksum"].ToString();
        var result = await _uploadService.UploadChunkAsync(sessionId, chunkIndex, Request.Body, checksum, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [Authorize(Roles = "Instructor,Admin,SuperAdmin")]
    [HttpPost("upload/{sessionId:guid}/complete")]
    public async Task<ActionResult<ApiResponse<UploadSessionStatusDto>>> CompleteUpload(Guid sessionId, CancellationToken ct)
    {
        var result = await _uploadService.CompleteUploadSessionAsync(sessionId, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [Authorize]
    [HttpGet("upload/{sessionId:guid}/status")]
    public async Task<ActionResult<ApiResponse<UploadSessionStatusDto>>> GetUploadStatus(Guid sessionId, CancellationToken ct)
    {
        var result = await _uploadService.GetUploadStatusAsync(sessionId, ct);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [Authorize]
    [HttpDelete("upload/{sessionId:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> CancelUpload(Guid sessionId, CancellationToken ct)
    {
        var result = await _uploadService.CancelUploadSessionAsync(sessionId, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [Authorize]
    [HttpPost("playback/authorize")]
    public async Task<ActionResult<ApiResponse<PlaybackAuthorizationResponse>>> AuthorizePlayback([FromBody] PlaybackAuthorizationRequest request, CancellationToken ct)
    {
        var result = await _playbackService.AuthorizePlaybackAsync(CurrentUserId, request.LessonId, ClientIpAddress, ClientUserAgent, ct);
        return result.Success ? Ok(result) : StatusCode(403, result);
    }

    [Authorize]
    [HttpPost("playback/heartbeat")]
    public async Task<ActionResult<ApiResponse<PlaybackHeartbeatResponse>>> RecordHeartbeat([FromBody] PlaybackHeartbeatRequest request, CancellationToken ct)
    {
        var result = await _playbackService.RecordHeartbeatAsync(CurrentUserId, request, ClientIpAddress, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
