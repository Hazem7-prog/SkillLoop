
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SkillLoop.Api.Models;

namespace SkillLoop.Api.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    public DbSet<PasswordResetOtp> PasswordResetOtps => Set<PasswordResetOtp>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<CourseAvailability> CourseAvailabilities => Set<CourseAvailability>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<SavedCourse> SavedCourses => Set<SavedCourse>();

    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<Message> Messages => Set<Message>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Courses & Booking (B)

        builder.Entity<SavedCourse>()
            .HasKey(s => new { s.UserId, s.CourseId });

        builder.Entity<Course>()
            .HasIndex(c => c.TeacherId);

        builder.Entity<Booking>()
            .HasIndex(b => b.StudentId);

        // Prevent booking the same slot twice
        builder.Entity<Booking>()
            .HasIndex(b => new
            {
                b.CourseId,
                b.StudentId,
                b.StartTime
            })
            .IsUnique()
            .HasFilter("[Status] <> 2");

        builder.Entity<Course>()
            .Property(c => c.MaxStudentsPerSession)
            .HasDefaultValue(1);

        builder.Entity<Course>()
            .Property(c => c.AllowsOnline)
            .HasDefaultValue(true);

        builder.Entity<Category>().HasData(
            new Category { Id = 1, Name = "Programming", Icon = "code" },
            new Category { Id = 2, Name = "Design", Icon = "palette" },
            new Category { Id = 3, Name = "Languages", Icon = "language" },
            new Category { Id = 4, Name = "Music", Icon = "music" },
            new Category { Id = 5, Name = "Business", Icon = "briefcase" },
            new Category { Id = 6, Name = "Photography", Icon = "camera" },
            new Category { Id = 7, Name = "Cooking", Icon = "chef-hat" },
            new Category { Id = 8, Name = "Fitness", Icon = "dumbbell" }
        );

        // Chat & Conversations (C)

        builder.Entity<Conversation>()
            .HasOne(c => c.Course)
            .WithMany()
            .HasForeignKey(c => c.CourseId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<Message>()
            .HasOne(m => m.Conversation)
            .WithMany(c => c.Messages)
            .HasForeignKey(m => m.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Conversation>()
            .HasIndex(c => c.Participant1Id);

        builder.Entity<Conversation>()
            .HasIndex(c => c.Participant2Id);

        builder.Entity<Message>()
            .Property(m => m.Text)
            .HasMaxLength(4000)
            .IsRequired();

        builder.Entity<Message>()
            .HasIndex(m => new
            {
                m.ConversationId,
                m.SentAt
            });
    }
}
