namespace SkillLoop.Api.DTOs;

// The "My Teaching Overview" card on the Teach screen
public class TeachingOverviewDto
{
    public int ActiveSessions { get; set; }      // my Active courses (the rows shown as "Active")
    public int SessionsCompleted { get; set; }   // completed bookings on my courses
    public double? AverageRating { get; set; }   // from A's Review table (null until it exists)
    public int CreditsEarned { get; set; }       // sum of PricePaid on completed bookings

    // Extras (cheap to compute, useful for the profile stats too)
    public int UpcomingSessions { get; set; }
    public int StudentsCount { get; set; }
}