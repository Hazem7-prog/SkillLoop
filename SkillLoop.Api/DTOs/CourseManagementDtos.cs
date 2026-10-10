using System.Text.Json.Serialization;
using SkillLoop.Api.Models;

namespace SkillLoop.Api.DTOs;

// One weekly recurring slot, e.g. { "dayOfWeek": "Monday", "startTime": "18:00" } (UTC)
public class ScheduleSlotDto
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public DayOfWeek DayOfWeek { get; set; }

    public string StartTime { get; set; } = string.Empty;   // "HH:mm"
}

public class CreateCourseRequest
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? CoverImageUrl { get; set; }
    public int CategoryId { get; set; }
    public CourseLevel? Level { get; set; }
    public int TotalSessions { get; set; }
    public int PricePerSession { get; set; }       // credits
    public int DurationMinutes { get; set; }
    public List<string>? Tags { get; set; }
    public List<string>? LearningOutcomes { get; set; }
    public int? MaxStudentsPerSession { get; set; }   // default 1
    public bool? AllowsOnline { get; set; }           // default true
    public bool? AllowsOffline { get; set; }          // default false
    public string? OfflineLocation { get; set; }      // required if AllowsOffline
    public List<ScheduleSlotDto>? Schedule { get; set; }
}

public record CreateCourseResponse(int Id);

public class UpdateScheduleRequest
{
    public List<ScheduleSlotDto>? Schedule { get; set; }
}

public class TeachingCourseDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string? CoverImageUrl { get; set; }
    public CourseLevel Level { get; set; }
    public CourseStatus Status { get; set; }
    public int PricePerSession { get; set; }
    public int DurationMinutes { get; set; }
    public int TotalSessions { get; set; }
    public int UpcomingBookings { get; set; }
    public int StudentsCount { get; set; }
}