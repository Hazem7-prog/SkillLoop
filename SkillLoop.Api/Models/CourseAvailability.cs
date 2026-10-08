namespace SkillLoop.Api.Models;

// A weekly recurring slot set by the teacher, e.g. every Monday at 18:00 (UTC)
public class CourseAvailability
{
    public int Id { get; set; }

    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;

    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }   // UTC
}