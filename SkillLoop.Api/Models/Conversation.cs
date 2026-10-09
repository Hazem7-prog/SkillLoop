namespace SkillLoop.Api.Models
{
    public class Conversation
    {

        public int Id { get; set; }

        public string Participant1Id { get; set; } = string.Empty;

        public string Participant2Id { get; set; } = string.Empty;

        public int? CourseId { get; set; }

        public Course? Course { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
            = DateTimeOffset.UtcNow;

        public ICollection<Message> Messages { get; set; }
            = new List<Message>();
    }
}
