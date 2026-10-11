using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkillLoop.Api.Data;
using SkillLoop.Api.DTOs;
using SkillLoop.Api.Models;

namespace SkillLoop.Api.Controllers;

[ApiController]
[Route("api/courses")]
[Authorize]
public class SavedCoursesController : ControllerBase
{
    private readonly AppDbContext _db;

    public SavedCoursesController(AppDbContext db) => _db = db;

    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    // POST /api/courses/{id}/save   (safe to call twice)
    [HttpPost("{id:int}/save")]
    public async Task<IActionResult> Save(int id)
    {
        var userId = CurrentUserId;

        var exists = await _db.Courses.AnyAsync(c => c.Id == id && c.Status == CourseStatus.Active);
        if (!exists)
            return NotFound(new ApiError("Course not found.", "COURSE_NOT_FOUND"));

        var alreadySaved = await _db.SavedCourses.AnyAsync(s => s.UserId == userId && s.CourseId == id);
        if (!alreadySaved)
        {
            _db.SavedCourses.Add(new SavedCourse { UserId = userId, CourseId = id });
            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // A parallel request saved it first: the result is the same, so nothing to do
            }
        }

        return NoContent();
    }

    // DELETE /api/courses/{id}/save   (safe to call twice)
    [HttpDelete("{id:int}/save")]
    public async Task<IActionResult> Unsave(int id)
    {
        var saved = await _db.SavedCourses.FindAsync(CurrentUserId, id);
        if (saved is not null)
        {
            _db.SavedCourses.Remove(saved);
            await _db.SaveChangesAsync();
        }

        return NoContent();
    }

    // GET /api/courses/saved
    [HttpGet("saved")]
    public async Task<ActionResult<List<CourseListItemDto>>> GetSaved()
    {
        var userId = CurrentUserId;

        var items = await (
            from s in _db.SavedCourses.AsNoTracking()
            where s.UserId == userId && s.Course.Status == CourseStatus.Active
            join u in _db.Users on s.Course.TeacherId equals u.Id
            orderby s.Course.CreatedAt descending
            select new CourseListItemDto
            {
                Id = s.Course.Id,
                Title = s.Course.Title,
                CategoryId = s.Course.CategoryId,
                CategoryName = s.Course.Category.Name,
                CoverImageUrl = s.Course.CoverImageUrl,
                Level = s.Course.Level,
                PricePerSession = s.Course.PricePerSession,
                DurationMinutes = s.Course.DurationMinutes,
                TotalSessions = s.Course.TotalSessions,
                TeacherId = s.Course.TeacherId,
                TeacherName = u.FullName,
                TeacherAvatarUrl = u.AvatarUrl
            })
            .ToListAsync();

        return Ok(items);
    }
}