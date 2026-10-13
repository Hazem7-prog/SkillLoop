using Microsoft.EntityFrameworkCore;
using SkillLoop.Api.Data;
using SkillLoop.Api.DTOs;
using SkillLoop.Api.Models;

namespace SkillLoop.Api.Services;

// Single source of truth for "which times can a student book on this date?".
// The availability endpoint uses it now; BookingService will reuse it to validate bookings.
public interface IAvailabilityService
{
    Task<AvailabilityDto?> GetAsync(int courseId, DateOnly date, string? currentUserId);
}

public class AvailabilityService : IAvailabilityService
{
    private readonly AppDbContext _db;

    public AvailabilityService(AppDbContext db) => _db = db;

    public async Task<AvailabilityDto?> GetAsync(int courseId, DateOnly date, string? currentUserId)
    {
        var course = await _db.Courses
            .AsNoTracking()
            .Where(c => c.Id == courseId && c.Status == CourseStatus.Active)
            .Select(c => new
            {
                c.DurationMinutes,
                c.PricePerSession,
                c.MaxStudentsPerSession,
                c.AllowsOnline,
                c.AllowsOffline,
                c.OfflineLocation,
                c.EndDate
            })
            .FirstOrDefaultAsync();

        if (course is null) return null;

        // The teacher's weekly slots for this weekday (e.g. every Wednesday at 10:00 UTC)
        var weekday = date.DayOfWeek;
        // After the course's end date no slots are offered
        var pastEnd = course.EndDate.HasValue && date > course.EndDate.Value;
        var times = pastEnd ? new List<TimeOnly>() : await _db.CourseAvailabilities
            .AsNoTracking()
            .Where(a => a.CourseId == courseId && a.DayOfWeek == weekday)
            .Select(a => a.StartTime)
            .ToListAsync();
        times.Sort();

        // Bookings already made on that day (cancelled ones free their spot)
        var dayStart = new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var dayEnd = dayStart.AddDays(1);

        var booked = await _db.Bookings
            .AsNoTracking()
            .Where(b => b.CourseId == courseId
                     && b.Status != BookingStatus.Cancelled
                     && b.StartTime >= dayStart
                     && b.StartTime < dayEnd)
            .Select(b => new { b.StartTime, b.StudentId })
            .ToListAsync();

        var now = DateTimeOffset.UtcNow;

        var slots = times.Select(t =>
        {
            var start = new DateTimeOffset(date.ToDateTime(t), TimeSpan.Zero);
            var taken = booked.Count(b => b.StartTime == start);
            var bookedByMe = currentUserId is not null
                             && booked.Any(b => b.StartTime == start && b.StudentId == currentUserId);
            var spotsLeft = Math.Max(course.MaxStudentsPerSession - taken, 0);

            return new AvailabilitySlotDto
            {
                StartTime = start,
                EndTime = start.AddMinutes(course.DurationMinutes),
                SpotsLeft = spotsLeft,
                IsBookedByMe = bookedByMe,
                IsAvailable = start > now && spotsLeft > 0 && !bookedByMe
            };
        }).ToList();

        return new AvailabilityDto
        {
            CourseId = courseId,
            Date = date,
            DurationMinutes = course.DurationMinutes,
            PricePerSession = course.PricePerSession,
            MaxStudentsPerSession = course.MaxStudentsPerSession,
            AllowsOnline = course.AllowsOnline,
            AllowsOffline = course.AllowsOffline,
            OfflineLocation = course.OfflineLocation,
            Slots = slots
        };
    }
}