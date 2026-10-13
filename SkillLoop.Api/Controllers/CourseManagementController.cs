using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkillLoop.Api.Data;
using SkillLoop.Api.DTOs;
using SkillLoop.Api.Models;
using SkillLoop.Api.Services;

namespace SkillLoop.Api.Controllers;

// Teacher-side endpoints. Shares the "api/courses" route with CoursesController.
[ApiController]
[Route("api/courses")]
[Authorize]
public class CourseManagementController : ControllerBase
{
    private readonly AppDbContext _db;

    private readonly IBookingService _bookings;

    public CourseManagementController(AppDbContext db, IBookingService bookings)
    {
        _db = db;
        _bookings = bookings;
    }

    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    private static ApiError Err(string message, string code = "VALIDATION_ERROR") => new(message, code);

    // POST /api/courses
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCourseRequest req)
    {
        var title = req.Title?.Trim() ?? "";
        var description = req.Description?.Trim() ?? "";

        if (title.Length < 3 || title.Length > 100)
            return BadRequest(Err("Title must be between 3 and 100 characters."));
        if (description.Length < 10 || description.Length > 2000)
            return BadRequest(Err("Description must be between 10 and 2000 characters."));
        if (req.Level is null || !Enum.IsDefined(req.Level.Value))
            return BadRequest(Err("Level is required (Beginner, Intermediate or Advanced)."));
        if (req.TotalSessions is < 1 or > 100)
            return BadRequest(Err("TotalSessions must be between 1 and 100."));
        if (req.PricePerSession is < 1 or > 1000)
            return BadRequest(Err("PricePerSession must be between 1 and 1000 credits."));
        if (req.DurationMinutes is < 15 or > 240)
            return BadRequest(Err("DurationMinutes must be between 15 and 240."));

        var tags = (req.Tags ?? new())
            .Select(t => t?.Trim() ?? "")
            .Where(t => t.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (tags.Count > 10)
            return BadRequest(Err("A course can have at most 10 tags."));
        if (tags.Any(t => t.Length > 30 || t.Contains(',')))
            return BadRequest(Err("Each tag must be 30 characters or fewer and cannot contain commas."));

        if (req.EndDate is not null && req.EndDate < DateOnly.FromDateTime(DateTime.UtcNow))
            return BadRequest(Err("EndDate cannot be in the past."));

        var capacity = req.MaxStudentsPerSession ?? 1;
        if (capacity is < 1 or > 50)
            return BadRequest(Err("MaxStudentsPerSession must be between 1 and 50."));

        var allowsOnline = req.AllowsOnline ?? true;
        var allowsOffline = req.AllowsOffline ?? false;
        if (!allowsOnline && !allowsOffline)
            return BadRequest(Err("The course must allow at least one session type (online or offline)."));

        var offlineLocation = req.OfflineLocation?.Trim();
        if (allowsOffline && string.IsNullOrWhiteSpace(offlineLocation))
            return BadRequest(Err("OfflineLocation is required when offline sessions are allowed."));
        if (offlineLocation?.Length > 100)
            return BadRequest(Err("OfflineLocation must be 100 characters or fewer."));

        var outcomes = (req.LearningOutcomes ?? new())
            .Select(o => o?.Trim() ?? "")
            .Where(o => o.Length > 0)
            .ToList();
        if (outcomes.Count > 10)
            return BadRequest(Err("A course can have at most 10 learning outcomes."));
        if (outcomes.Any(o => o.Length > 150))
            return BadRequest(Err("Each learning outcome must be 150 characters or fewer."));

        var cover = req.CoverImageUrl?.Trim();
        if (!string.IsNullOrEmpty(cover) &&
            (cover.Length > 500 ||
             !(cover.StartsWith('/') ||
               (Uri.TryCreate(cover, UriKind.Absolute, out var uri) &&
                (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)))))
            return BadRequest(Err("CoverImageUrl must be an http(s) URL or a path starting with '/'."));

        if (!await _db.Categories.AnyAsync(c => c.Id == req.CategoryId))
            return BadRequest(Err("Category does not exist.", "CATEGORY_NOT_FOUND"));

        var (slots, scheduleError) = ParseSchedule(req.Schedule, req.DurationMinutes);
        if (scheduleError is not null)
            return BadRequest(Err(scheduleError));

        var course = new Course
        {
            TeacherId = CurrentUserId,
            CategoryId = req.CategoryId,
            Title = title,
            Description = description,
            Level = req.Level.Value,
            TotalSessions = req.TotalSessions,
            PricePerSession = req.PricePerSession,
            DurationMinutes = req.DurationMinutes,
            Tags = string.Join(",", tags),
            LearningOutcomes = string.Join("\n", outcomes),
            CoverImageUrl = string.IsNullOrEmpty(cover) ? null : cover,
            MaxStudentsPerSession = capacity,
            EndDate = req.EndDate,
            AllowsOnline = allowsOnline,
            AllowsOffline = allowsOffline,
            OfflineLocation = allowsOffline ? offlineLocation : null,
            Status = CourseStatus.Active,
            Availabilities = slots!
        };

        _db.Courses.Add(course);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(CoursesController.GetById), "Courses",
            new { id = course.Id }, new CreateCourseResponse(course.Id));
    }

    // GET /api/courses/mine/teaching?tab=All|Active|Upcoming|Completed|Cancelled
    [HttpGet("mine/teaching")]
    public async Task<ActionResult<List<TeachingCourseDto>>> MineTeaching([FromQuery] TeachingTab tab = TeachingTab.All)
    {
        var userId = CurrentUserId;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var now = DateTimeOffset.UtcNow;

        var query = _db.Courses.AsNoTracking().Where(c => c.TeacherId == userId);

        query = tab switch
        {
            TeachingTab.Active => query.Where(c => c.Status == CourseStatus.Active
                                                && (c.EndDate == null || c.EndDate >= today)),
            TeachingTab.Upcoming => query.Where(c => c.Status == CourseStatus.Active
                                                  && (c.EndDate == null || c.EndDate >= today)
                                                  && c.Bookings.Any(b => b.Status == BookingStatus.Upcoming)),
            TeachingTab.Completed => query.Where(c => c.Status == CourseStatus.Active
                                                   && c.EndDate != null && c.EndDate < today),
            TeachingTab.Cancelled => query.Where(c => c.Status == CourseStatus.Archived),
            _ => query
        };

        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new TeachingCourseDto
            {
                Id = c.Id,
                Title = c.Title,
                CategoryName = c.Category.Name,
                CoverImageUrl = c.CoverImageUrl,
                Level = c.Level,
                Status = c.Status,
                PricePerSession = c.PricePerSession,
                DurationMinutes = c.DurationMinutes,
                TotalSessions = c.TotalSessions,
                EndDate = c.EndDate,
                UpcomingBookings = c.Bookings.Count(b => b.Status == BookingStatus.Upcoming),
                CompletedSessions = c.Bookings.Count(b => b.Status == BookingStatus.Completed),
                StudentsCount = c.Bookings
                    .Where(b => b.Status != BookingStatus.Cancelled)
                    .Select(b => b.StudentId)
                    .Distinct()
                    .Count()
            })
            .ToListAsync();

        // Next session date per course (grouped in memory: simple and safe)
        var ids = items.Select(i => i.Id).ToList();
        var upcoming = await _db.Bookings
            .AsNoTracking()
            .Where(b => ids.Contains(b.CourseId) && b.Status == BookingStatus.Upcoming)
            .Select(b => new { b.CourseId, b.StartTime })
            .ToListAsync();

        foreach (var item in items)
        {
            item.NextSessionStart = upcoming
                .Where(u => u.CourseId == item.Id && u.StartTime > now)
                .OrderBy(u => u.StartTime)
                .Select(u => (DateTimeOffset?)u.StartTime)
                .FirstOrDefault();

            item.DisplayStatus =
                item.Status == CourseStatus.Archived ? "Cancelled"
                : item.EndDate is not null && item.EndDate < today ? "Completed"
                : "Active";
        }

        return Ok(items);
    }

    // POST /api/courses/{id}/cancel   (teacher only)
    // Cancels the course, cancels its upcoming bookings and refunds every student.
    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> CancelCourse(int id)
    {
        var result = await _bookings.CancelCourseAsync(CurrentUserId, id);
        return result.Success
            ? Ok(result.Value)
            : StatusCode(result.StatusCode, result.Error);
    }

    // GET /api/courses/{id}/schedule  (teacher only: the weekly slots they set)
    [HttpGet("{id:int}/schedule")]
    public async Task<IActionResult> GetSchedule(int id)
    {
        var course = await _db.Courses.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
        if (course is null)
            return NotFound(Err("Course not found.", "COURSE_NOT_FOUND"));
        if (course.TeacherId != CurrentUserId)
            return StatusCode(403, Err("You are not the teacher of this course.", "NOT_COURSE_OWNER"));

        return Ok(await LoadSchedule(id));
    }

    // PUT /api/courses/{id}/schedule  (teacher only: replaces ALL weekly slots)
    [HttpPut("{id:int}/schedule")]
    public async Task<IActionResult> SetSchedule(int id, [FromBody] UpdateScheduleRequest req)
    {
        var course = await _db.Courses.FirstOrDefaultAsync(c => c.Id == id);
        if (course is null)
            return NotFound(Err("Course not found.", "COURSE_NOT_FOUND"));
        if (course.TeacherId != CurrentUserId)
            return StatusCode(403, Err("You are not the teacher of this course.", "NOT_COURSE_OWNER"));

        var (slots, error) = ParseSchedule(req.Schedule, course.DurationMinutes);
        if (error is not null)
            return BadRequest(Err(error));

        var old = await _db.CourseAvailabilities.Where(a => a.CourseId == id).ToListAsync();
        _db.CourseAvailabilities.RemoveRange(old);
        foreach (var s in slots!) s.CourseId = id;
        _db.CourseAvailabilities.AddRange(slots);
        await _db.SaveChangesAsync();

        return Ok(await LoadSchedule(id));
    }

    private async Task<List<ScheduleSlotDto>> LoadSchedule(int courseId)
    {
        var rows = await _db.CourseAvailabilities
            .AsNoTracking()
            .Where(a => a.CourseId == courseId)
            .ToListAsync();

        return rows
            .OrderBy(a => a.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)a.DayOfWeek)  // Monday first
            .ThenBy(a => a.StartTime)
            .Select(a => new ScheduleSlotDto { DayOfWeek = a.DayOfWeek, StartTime = a.StartTime.ToString("HH:mm") })
            .ToList();
    }

    // Validates the slots and converts them to entities.
    private static (List<CourseAvailability>? Slots, string? Error) ParseSchedule(
        List<ScheduleSlotDto>? schedule, int durationMinutes)
    {
        if (schedule is null || schedule.Count == 0)
            return (null, "Add at least one weekly time slot, otherwise students cannot book this course.");
        if (schedule.Count > 50)
            return (null, "Too many time slots (maximum 50).");

        var parsed = new List<(DayOfWeek Day, TimeOnly Time)>();
        foreach (var s in schedule)
        {
            if (!Enum.IsDefined(s.DayOfWeek))
                return (null, "Invalid dayOfWeek.");
            if (!TimeOnly.TryParseExact(s.StartTime, new[] { "HH:mm", "HH:mm:ss" },
                    null, System.Globalization.DateTimeStyles.None, out var time))
                return (null, $"Invalid startTime '{s.StartTime}'. Use HH:mm, for example 18:00.");
            if (time.ToTimeSpan().TotalMinutes + durationMinutes > 24 * 60)
                return (null, $"A session starting at {s.StartTime} would run past midnight.");
            parsed.Add((s.DayOfWeek, time));
        }

        if (parsed.Distinct().Count() != parsed.Count)
            return (null, "Duplicate time slots are not allowed.");

        foreach (var day in parsed.GroupBy(p => p.Day))
        {
            var times = day.Select(d => d.Time).OrderBy(t => t).ToList();
            for (var i = 1; i < times.Count; i++)
                if ((times[i] - times[i - 1]).TotalMinutes < durationMinutes)
                    return (null, $"Slots on {day.Key} overlap: sessions last {durationMinutes} minutes.");
        }

        var entities = parsed
            .Select(p => new CourseAvailability { DayOfWeek = p.Day, StartTime = p.Time })
            .ToList();

        return (entities, null);
    }
}