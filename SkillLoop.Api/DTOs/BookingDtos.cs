using SkillLoop.Api.Models;

namespace SkillLoop.Api.DTOs;

public class CreateBookingRequest
{
    public int CourseId { get; set; }
    public DateTimeOffset? StartTime { get; set; }   // must match a slot from /availability
    public BookingType? Type { get; set; }           // "Online" or "Offline"
}

public class BookingDto
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public string? CoverImageUrl { get; set; }
    public string TeacherId { get; set; } = string.Empty;
    public string TeacherName { get; set; } = string.Empty;
    public string? TeacherAvatarUrl { get; set; }
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }
    public BookingType Type { get; set; }
    public BookingStatus Status { get; set; }
    public int PricePaid { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}