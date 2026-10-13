using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkillLoop.Api.Data;
using SkillLoop.Api.DTOs;
using SkillLoop.Api.Models;

namespace SkillLoop.Api.Controllers;

[ApiController]
[Route("api/instructors")]
[Authorize]
public class InstructorsController : ControllerBase
{
    private readonly AppDbContext _db;

    public InstructorsController(AppDbContext db) => _db = db;

    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    // GET /api/instructors/popular?limit=10
    // Teachers with at least one active course, ranked by distinct students.
    [HttpGet("popular")]
    public async Task<ActionResult<List<InstructorDto>>> Popular([FromQuery] int limit = 10)
    {
        limit = Math.Clamp(limit, 1, 30);
        var userId = CurrentUserId;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var teachers = await _db.Courses
            .AsNoTracking()
            .Where(c => c.Status == CourseStatus.Active && (c.EndDate == null || c.EndDate >= today) && c.TeacherId != userId)
            .GroupBy(c => c.TeacherId)
            .Select(g => new
            {
                TeacherId = g.Key,
                MinPrice = g.Min(c => c.PricePerSession),
                CoursesCount = g.Count()
            })
            .ToListAsync();

        if (teachers.Count == 0)
            return Ok(new List<InstructorDto>());

        var teacherIds = teachers.Select(t => t.TeacherId).ToList();

        // Distinct (teacher, student) pairs, then counted per teacher
        var pairs = await _db.Bookings
            .AsNoTracking()
            .Where(b => b.Status != BookingStatus.Cancelled && teacherIds.Contains(b.Course.TeacherId))
            .Select(b => new { b.Course.TeacherId, b.StudentId })
            .Distinct()
            .ToListAsync();
        var students = pairs.GroupBy(p => p.TeacherId).ToDictionary(g => g.Key, g => g.Count());

        var users = await _db.Users
            .AsNoTracking()
            .Where(u => teacherIds.Contains(u.Id))
            .Select(u => new { u.Id, u.FullName, u.AvatarUrl, u.JobTitle })
            .ToListAsync();

        var items = teachers
            .Join(users, t => t.TeacherId, u => u.Id, (t, u) => new InstructorDto
            {
                UserId = u.Id,
                Name = u.FullName,
                AvatarUrl = u.AvatarUrl,
                JobTitle = u.JobTitle,
                AverageRating = null,   // TODO: from A's Review table
                StudentsCount = students.GetValueOrDefault(t.TeacherId),
                CoursesCount = t.CoursesCount,
                PricePerSession = t.MinPrice
            })
            .OrderByDescending(i => i.StudentsCount)
            .ThenByDescending(i => i.CoursesCount)
            .ThenBy(i => i.Name)
            .Take(limit)
            .ToList();

        return Ok(items);
    }
}