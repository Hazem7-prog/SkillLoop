namespace SkillLoop.Api.Models;

public class SavedCourse
{
    public string UserId { get; set; } = string.Empty;

    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;
}