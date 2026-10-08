namespace SkillLoop.Api.Models;

public class Course
{
    public int Id { get; set; }

    // Reference to the user by id only (never touch ApplicationUser)
    public string TeacherId { get; set; } = string.Empty;

    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public CourseLevel Level { get; set; }
    public int TotalSessions { get; set; }
    public int PricePerSession { get; set; }      // in Credits (whole numbers)
    public int DurationMinutes { get; set; }
    public string Tags { get; set; } = string.Empty;   // comma-separated
    public CourseStatus Status { get; set; } = CourseStatus.Active;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<CourseAvailability> Availabilities { get; set; } = new List<CourseAvailability>();
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}