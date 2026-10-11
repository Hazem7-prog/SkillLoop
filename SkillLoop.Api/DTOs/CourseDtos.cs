using SkillLoop.Api.Models;

namespace SkillLoop.Api.DTOs;

public record CategoryDto(int Id, string Name, string Icon);

public class CourseListItemDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? CoverImageUrl { get; set; }
    public CourseLevel Level { get; set; }
    public int PricePerSession { get; set; }
    public int DurationMinutes { get; set; }
    public int TotalSessions { get; set; }
    public string TeacherId { get; set; } = string.Empty;
    public string TeacherName { get; set; } = string.Empty;
    public string? TeacherAvatarUrl { get; set; }

    // Filled from A's Review table once it exists (design shows "4.9 (122)")
    public double? AverageRating { get; set; }
    public int ReviewsCount { get; set; }
}

public class CourseDetailsDto : CourseListItemDto
{
    public string Description { get; set; } = string.Empty;
    public string? TeacherJobTitle { get; set; }
    public List<string> Tags { get; set; } = new();
    public List<string> LearningOutcomes { get; set; } = new();
    public int MaxStudentsPerSession { get; set; }
    public bool AllowsOnline { get; set; }
    public bool AllowsOffline { get; set; }
    public string? OfflineLocation { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public int StudentsCount { get; set; }
    public bool IsSaved { get; set; }
}