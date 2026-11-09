using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NileLearn.Application.Common.Models;
using NileLearn.Application.Features.Learning;

namespace NileLearn.Api.Controllers;

public class LearningController : BaseApiController
{
    private readonly ILearningService _learningService;

    public LearningController(ILearningService learningService)
    {
        _learningService = learningService;
    }

    [Authorize]
    [HttpGet("my-courses")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<StudentCourseProgressDto>>>> GetMyCourses(CancellationToken ct)
    {
        var result = await _learningService.GetMyEnrolledCoursesAsync(CurrentUserId, ct);
        return Ok(result);
    }

    [Authorize]
    [HttpGet("courses/{courseId:guid}/progress")]
    public async Task<ActionResult<ApiResponse<StudentCourseProgressDto>>> GetCourseProgress(Guid courseId, CancellationToken ct)
    {
        var result = await _learningService.GetCourseProgressAsync(CurrentUserId, courseId, ct);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [Authorize]
    [HttpPost("progress")]
    public async Task<ActionResult<ApiResponse<bool>>> UpdateProgress([FromBody] UpdateProgressRequest request, CancellationToken ct)
    {
        var result = await _learningService.UpdateLessonProgressAsync(CurrentUserId, request, ct);
        return Ok(result);
    }

    [Authorize]
    [HttpPost("certificates/{courseId:guid}/claim")]
    public async Task<ActionResult<ApiResponse<CertificateDto>>> ClaimCertificate(Guid courseId, CancellationToken ct)
    {
        var result = await _learningService.IssueCertificateIfEligibleAsync(CurrentUserId, courseId, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("certificates/verify/{code}")]
    public async Task<ActionResult<ApiResponse<CertificateDto>>> VerifyCertificate(string code, CancellationToken ct)
    {
        var result = await _learningService.VerifyCertificateAsync(code, ct);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [Authorize]
    [HttpGet("certificates/my-certificates")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CertificateDto>>>> GetMyCertificates(CancellationToken ct)
    {
        var result = await _learningService.GetMyCertificatesAsync(CurrentUserId, ct);
        return Ok(result);
    }

    [Authorize]
    [HttpPost("quizzes/{quizId:guid}/start")]
    public async Task<ActionResult<ApiResponse<StartQuizAttemptResponse>>> StartQuiz(Guid quizId, CancellationToken ct)
    {
        var result = await _learningService.StartQuizAsync(CurrentUserId, quizId, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [Authorize]
    [HttpPost("quizzes/submit")]
    public async Task<ActionResult<ApiResponse<QuizAttemptResultDto>>> SubmitQuiz([FromBody] SubmitQuizAttemptRequest request, CancellationToken ct)
    {
        var result = await _learningService.SubmitQuizAsync(CurrentUserId, request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
