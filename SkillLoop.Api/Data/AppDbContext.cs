using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SkillLoop.Api.Models;

namespace SkillLoop.Api.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<PasswordResetOtp> PasswordResetOtps => Set<PasswordResetOtp>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<CourseAvailability> CourseAvailabilities => Set<CourseAvailability>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<SavedCourse> SavedCourses => Set<SavedCourse>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // --- Courses & Booking (B) ---
        builder.Entity<SavedCourse>().HasKey(s => new { s.UserId, s.CourseId });

        builder.Entity<Course>().HasIndex(c => c.TeacherId);
        builder.Entity<Booking>().HasIndex(b => b.StudentId);

        // A slot can only be booked once, unless the booking was cancelled (Cancelled = 2)
        builder.Entity<Booking>()
            .HasIndex(b => new { b.CourseId, b.StartTime })
            .IsUnique()
            .HasFilter("[Status] <> 2");
    }
}