namespace SkillLoop.Api.Models
{
    public class Message
    {
        public int Id { get; set; }

        public int ConversationId { get; set; }

        public Conversation Conversation { get; set; } = null!;

        public string SenderId { get; set; } = string.Empty;

        public string Text { get; set; } = string.Empty;

        public DateTimeOffset SentAt { get; set; }
            = DateTimeOffset.UtcNow;

        public bool IsRead { get; set; } = false;
    }
}
