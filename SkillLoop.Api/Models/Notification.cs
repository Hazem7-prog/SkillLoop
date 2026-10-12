namespace SkillLoop.Api.Models
{
    public class Notification
    {
        public int Id { get; set; }

        public string UserId { get; set; } = null!;

        public string Text { get; set; } = null!;

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
