namespace SkillLoop.Api.Models;

public class Booking
{
    public int Id { get; set; }

    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;

    public string StudentId { get; set; } = string.Empty;

    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }      // StartTime + Course.DurationMinutes

    public BookingType Type { get; set; }
    public BookingStatus Status { get; set; } = BookingStatus.Upcoming;

    public int PricePaid { get; set; }               // snapshot of credits charged
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}