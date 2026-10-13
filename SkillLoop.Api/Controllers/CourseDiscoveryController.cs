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
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

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
               && (c.EndDate == null || c.EndDate >= today)
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

    // Badge rules (tweak the numbers freely)
    private const int TrendingWindowDays = 7;
    private const int TrendingMinBookings = 3;
    private const int NewWindowDays = 14;
    private const int PopularMinBookings = 5;

    private static int BadgeRank(string? badge) => badge switch
    {
        "Trending" => 0,
        "New" => 1,
        "Popular" => 2,
        _ => 3
    };

    // GET /api/courses/discover?limit=10   ("Discover New Skills" on Home)
    // Trending = several bookings this week, New = created recently, Popular = booked a lot overall.
    [HttpGet("discover")]
    public async Task<ActionResult<List<DiscoverCourseDto>>> Discover([FromQuery] int limit = 10)
    {
        limit = Math.Clamp(limit, 1, 30);
        var userId = CurrentUserId;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var trendingSince = DateTimeOffset.UtcNow.AddDays(-TrendingWindowDays);
        var newSince = DateTimeOffset.UtcNow.AddDays(-NewWindowDays);

        var rows = await (
            from c in _db.Courses.AsNoTracking()
            where c.Status == CourseStatus.Active && (c.EndDate == null || c.EndDate >= today) && c.TeacherId != userId
            join u in _db.Users on c.TeacherId equals u.Id
            select new
            {
                Dto = new DiscoverCourseDto
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
                },
                c.CreatedAt,
                Total = c.Bookings.Count(b => b.Status != BookingStatus.Cancelled),
                Recent = c.Bookings.Count(b => b.Status != BookingStatus.Cancelled && b.CreatedAt >= trendingSince)
            })
            .ToListAsync();

        var items = rows
            .Select(r =>
            {
                r.Dto.Badge = r.Recent >= TrendingMinBookings ? "Trending"
                            : r.CreatedAt >= newSince ? "New"
                            : r.Total >= PopularMinBookings ? "Popular"
                            : null;
                return r;
            })
            .OrderBy(r => BadgeRank(r.Dto.Badge))
            .ThenByDescending(r => r.Recent)
            .ThenByDescending(r => r.Total)
            .ThenByDescending(r => r.CreatedAt)
            .Take(limit)
            .Select(r => r.Dto)
            .ToList();

        return Ok(items);
    }
}