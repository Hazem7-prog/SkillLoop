using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillLoop.Api.DTOs;
using SkillLoop.Api.Services;

namespace SkillLoop.Api.Controllers;

[ApiController]
[Route("api/courses")]
[Authorize]
public class CourseAvailabilityController : ControllerBase
{
    private readonly IAvailabilityService _availability;

    public CourseAvailabilityController(IAvailabilityService availability) => _availability = availability;

    // GET /api/courses/{id}/availability?date=2026-10-14   (date is a UTC date)
    [HttpGet("{id:int}/availability")]
    public async Task<IActionResult> Get(int id, [FromQuery] string? date)
    {
        if (!DateOnly.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var parsed))
            return BadRequest(new ApiError("date is required, format yyyy-MM-dd.", "VALIDATION_ERROR"));

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var result = await _availability.GetAsync(id, parsed, userId);

        return result is null
            ? NotFound(new ApiError("Course not found.", "COURSE_NOT_FOUND"))
            : Ok(result);
    }
}