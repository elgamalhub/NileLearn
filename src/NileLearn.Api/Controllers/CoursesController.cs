using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NileLearn.Application.Common.Models;
using NileLearn.Application.Features.Courses;

namespace NileLearn.Api.Controllers;

public class CoursesController : BaseApiController
{
    private readonly ICourseService _courseService;

    public CoursesController(ICourseService courseService)
    {
        _courseService = courseService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<CourseSummaryDto>>>> GetCourses([FromQuery] CourseFilterParams filter, CancellationToken ct)
    {
        var result = await _courseService.GetCoursesAsync(filter, CurrentUserId == Guid.Empty ? null : CurrentUserId, ct);
        return Ok(result);
    }

    [HttpGet("{slug}")]
    public async Task<ActionResult<ApiResponse<CourseDetailDto>>> GetCourseBySlug(string slug, CancellationToken ct)
    {
        var result = await _courseService.GetCourseBySlugAsync(slug, CurrentUserId == Guid.Empty ? null : CurrentUserId, ct);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpGet("id/{id:guid}")]
    public async Task<ActionResult<ApiResponse<CourseDetailDto>>> GetCourseById(Guid id, CancellationToken ct)
    {
        var result = await _courseService.GetCourseByIdAsync(id, CurrentUserId == Guid.Empty ? null : CurrentUserId, ct);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpGet("categories")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CategoryDto>>>> GetCategories(CancellationToken ct)
    {
        var result = await _courseService.GetCategoriesAsync(ct);
        return Ok(result);
    }

    [Authorize(Roles = "Instructor,Admin,SuperAdmin")]
    [HttpPost]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateCourse([FromBody] CreateCourseRequest request, CancellationToken ct)
    {
        var result = await _courseService.CreateCourseAsync(request, CurrentUserId, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [Authorize(Roles = "Instructor,Admin,SuperAdmin")]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> UpdateCourse(Guid id, [FromBody] CreateCourseRequest request, CancellationToken ct)
    {
        var result = await _courseService.UpdateCourseAsync(id, request, CurrentUserId, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [Authorize(Roles = "Instructor,Admin,SuperAdmin")]
    [HttpPost("{id:guid}/publish")]
    public async Task<ActionResult<ApiResponse<bool>>> PublishCourse(Guid id, CancellationToken ct)
    {
        var result = await _courseService.PublishCourseAsync(id, CurrentUserId, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [Authorize(Roles = "Instructor,Admin,SuperAdmin")]
    [HttpPost("{id:guid}/sections")]
    public async Task<ActionResult<ApiResponse<Guid>>> AddSection(Guid id, [FromBody] CreateSectionRequest request, CancellationToken ct)
    {
        var result = await _courseService.AddSectionAsync(id, request, CurrentUserId, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [Authorize(Roles = "Instructor,Admin,SuperAdmin")]
    [HttpPost("sections/{sectionId:guid}/lessons")]
    public async Task<ActionResult<ApiResponse<Guid>>> AddLesson(Guid sectionId, [FromBody] CreateLessonRequest request, CancellationToken ct)
    {
        var result = await _courseService.AddLessonAsync(sectionId, request, CurrentUserId, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [Authorize(Roles = "Instructor,Admin,SuperAdmin")]
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteCourse(Guid id, CancellationToken ct)
    {
        var result = await _courseService.DeleteCourseAsync(id, CurrentUserId, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
