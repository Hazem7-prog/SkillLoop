using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkillLoop.Api.Data;
using SkillLoop.Api.DTOs;
using SkillLoop.Api.Models;

namespace SkillLoop.Api.Controllers;

// Explore / Home suggestions
[ApiController]
[Route("api/courses")]
[Authorize]
public class CourseDiscoveryController : ControllerBase
{
    private readonly AppDbContext _db;

    public CourseDiscoveryController(AppDbContext db) => _db = db;

    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    // GET /api/courses/recommended?limit=10
    // 1) courses in categories I have already learned from come first
    // 2) then the most booked courses, then the newest
    // Never my own courses, and never ones I already booked.
    [HttpGet("recommended")]
    public async Task<ActionResult<List<CourseListItemDto>>> Recommended([FromQuery] int limit = 10)
    {
        limit = Math.Clamp(limit, 1, 30);
        var userId = CurrentUserId;

        var myBookings = await _db.Bookings
            .AsNoTracking()
            .Where(b => b.StudentId == userId && b.Status != BookingStatus.Cancelled)
            .Select(b => new { b.CourseId, b.Course.CategoryId })
            .ToListAsync();

        var bookedCourseIds = myBookings.Select(b => b.CourseId).Distinct().ToList();
        var myCategoryIds = myBookings.Select(b => b.CategoryId).Distinct().ToList();

        var items = await (
            from c in _db.Courses.AsNoTracking()
            where c.Status == CourseStatus.Active
               && c.TeacherId != userId
               && !bookedCourseIds.Contains(c.Id)
            join u in _db.Users on c.TeacherId equals u.Id
            orderby (myCategoryIds.Contains(c.CategoryId) ? 0 : 1),
                    c.Bookings.Count(b => b.Status != BookingStatus.Cancelled) descending,
                    c.CreatedAt descending
            select new CourseListItemDto
            {
                Id = c.Id,
                Title = c.Title,
                CategoryId = c.CategoryId,
                CategoryName = c.Category.Name,
                CoverImageUrl = c.CoverImageUrl,
                Level = c.Level,
                PricePerSession = c.PricePerSession,
                DurationMinutes = c.DurationMinutes,
                TotalSessions = c.TotalSessions,
                TeacherId = c.TeacherId,
                TeacherName = u.FullName,
                TeacherAvatarUrl = u.AvatarUrl
            })
            .Take(limit)
            .ToListAsync();

        return Ok(items);
    }
}