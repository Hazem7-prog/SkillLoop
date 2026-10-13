namespace SkillLoop.Api.DTOs;

// "Discover New Skills" card: a course card with a Trending / Popular / New badge
public class DiscoverCourseDto : CourseListItemDto
{
    public string? Badge { get; set; }
}

// "Popular Instructor" row on Explore
public class InstructorDto
{
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? JobTitle { get; set; }
    public double? AverageRating { get; set; }   // from A's Review table (null until it exists)
    public int StudentsCount { get; set; }       // "(368 students)"
    public int CoursesCount { get; set; }
    public int PricePerSession { get; set; }     // lowest price among the instructor's active courses
}