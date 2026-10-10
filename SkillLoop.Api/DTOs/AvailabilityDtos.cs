namespace SkillLoop.Api.DTOs;

// One time button in the "Select Time" grid
public class AvailabilitySlotDto
{
    public DateTimeOffset StartTime { get; set; }   // UTC
    public DateTimeOffset EndTime { get; set; }     // UTC (StartTime + course duration)
    public int SpotsLeft { get; set; }
    public bool IsAvailable { get; set; }           // false if past, full, or already booked by me
    public bool IsBookedByMe { get; set; }
}

// Everything the "Book a Session" screen needs for one date
public class AvailabilityDto
{
    public int CourseId { get; set; }
    public DateOnly Date { get; set; }
    public int DurationMinutes { get; set; }
    public int PricePerSession { get; set; }
    public int MaxStudentsPerSession { get; set; }
    public bool AllowsOnline { get; set; }
    public bool AllowsOffline { get; set; }
    public string? OfflineLocation { get; set; }
    public List<AvailabilitySlotDto> Slots { get; set; } = new();
}