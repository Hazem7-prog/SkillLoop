namespace SkillLoop.Api.DTOs;

// One "Continue Learning" card: cover, title, teacher, progress bar, "3 / 5 lessons", "4h left"
public class LearningCourseDto
{
    public int CourseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? CoverImageUrl { get; set; }
    public string TeacherId { get; set; } = string.Empty;
    public string TeacherName { get; set; } = string.Empty;
    public string? TeacherAvatarUrl { get; set; }

    public int TotalSessions { get; set; }
    public int CompletedSessions { get; set; }
    public int ProgressPercent { get; set; }          // 0..100
    public int RemainingMinutes { get; set; }         // remaining sessions x session duration
    public bool IsCompleted { get; set; }
    public DateTimeOffset? NextSessionStart { get; set; }   // next Upcoming booking, if any
}