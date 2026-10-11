using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkillLoop.Api.Data;
using SkillLoop.Api.DTOs;
using SkillLoop.Api.Models;

namespace SkillLoop.Api.Controllers;

[ApiController]
[Route("api/teaching")]
[Authorize]
public class TeachingController : ControllerBase
{
    private readonly AppDbContext _db;

    public TeachingController(AppDbContext db) => _db = db;

    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    // GET /api/teaching/overview
    [HttpGet("overview")]
    public async Task<ActionResult<TeachingOverviewDto>> Overview()
    {
        var userId = CurrentUserId;

        // Every non-cancelled booking made on one of my courses
        var onMyCourses = _db.Bookings
            .AsNoTracking()
            .Where(b => b.Course.TeacherId == userId && b.Status != BookingStatus.Cancelled);

        var completed = onMyCourses.Where(b => b.Status == BookingStatus.Completed);

        var activeSessions = await _db.Courses
            .CountAsync(c => c.TeacherId == userId && c.Status == CourseStatus.Active);
        var sessionsCompleted = await completed.CountAsync();
        // Teacher is paid the booking price when a session completes (no platform fee for now)
        var creditsEarned = await completed.SumAsync(b => (int?)b.PricePaid) ?? 0;
        var upcoming = await onMyCourses.CountAsync(b => b.Status == BookingStatus.Upcoming);
        var students = await onMyCourses.Select(b => b.StudentId).Distinct().CountAsync();

        return Ok(new TeachingOverviewDto
        {
            ActiveSessions = activeSessions,
            SessionsCompleted = sessionsCompleted,
            AverageRating = null,   // TODO: average of A's Review ratings for my courses
            CreditsEarned = creditsEarned,
            UpcomingSessions = upcoming,
            StudentsCount = students
        });
    }
}