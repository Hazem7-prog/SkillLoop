using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkillLoop.Api.Data;
using SkillLoop.Api.DTOs;
using SkillLoop.Api.Models;

namespace SkillLoop.Api.Controllers;

// Student side: the courses I am learning, with progress.
[ApiController]
[Route("api/courses")]
[Authorize]
public class CourseLearningController : ControllerBase
{
    private readonly AppDbContext _db;

    public CourseLearningController(AppDbContext db) => _db = db;

    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    // GET /api/courses/mine/learning
    // A course is "being learned" when I have at least one non-cancelled booking on it.
    // Progress = Completed bookings / TotalSessions (computed, never stored).
    [HttpGet("mine/learning")]
    public async Task<ActionResult<List<LearningCourseDto>>> MineLearning()
    {
        var userId = CurrentUserId;

        // My bookings are few, so grouping them in memory is simple and safe
        var bookings = await _db.Bookings
            .AsNoTracking()
            .Where(b => b.StudentId == userId && b.Status != BookingStatus.Cancelled)
            .Select(b => new { b.CourseId, b.Status, b.StartTime })
            .ToListAsync();

        if (bookings.Count == 0)
            return Ok(new List<LearningCourseDto>());

        var courseIds = bookings.Select(b => b.CourseId).Distinct().ToList();

        // Archived courses still show here: the student already booked them
        var courses = await (
            from c in _db.Courses.AsNoTracking()
            where courseIds.Contains(c.Id)
            join u in _db.Users on c.TeacherId equals u.Id
            select new
            {
                c.Id,
                c.Title,
                c.CoverImageUrl,
                c.TeacherId,
                TeacherName = u.FullName,
                TeacherAvatarUrl = u.AvatarUrl,
                c.TotalSessions,
                c.DurationMinutes
            })
            .ToListAsync();

        var items = courses.Select(c =>
        {
            var mine = bookings.Where(b => b.CourseId == c.Id).ToList();
            var completed = mine.Count(b => b.Status == BookingStatus.Completed);
            var remaining = Math.Max(c.TotalSessions - completed, 0);
            var progress = c.TotalSessions == 0
                ? 0
                : Math.Min(100, (int)Math.Round(completed * 100.0 / c.TotalSessions));

            return new LearningCourseDto
            {
                CourseId = c.Id,
                Title = c.Title,
                CoverImageUrl = c.CoverImageUrl,
                TeacherId = c.TeacherId,
                TeacherName = c.TeacherName,
                TeacherAvatarUrl = c.TeacherAvatarUrl,
                TotalSessions = c.TotalSessions,
                CompletedSessions = completed,
                ProgressPercent = progress,
                RemainingMinutes = remaining * c.DurationMinutes,
                IsCompleted = completed >= c.TotalSessions,
                NextSessionStart = mine
                    .Where(b => b.Status == BookingStatus.Upcoming)
                    .OrderBy(b => b.StartTime)
                    .Select(b => (DateTimeOffset?)b.StartTime)
                    .FirstOrDefault()
            };
        })
        // In progress first (soonest next session on top), finished courses last
        .OrderBy(x => x.IsCompleted)
        .ThenBy(x => x.NextSessionStart ?? DateTimeOffset.MaxValue)
        .ThenBy(x => x.Title)
        .ToList();

        return Ok(items);
    }
}