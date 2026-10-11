namespace SkillLoop.Api.Models
{
    public class Transaction
    {
        public int Id { get; set; }

        public string UserId { get; set; } = string.Empty;

        public int Amount { get; set; }

        public TransactionType Type { get; set; }

        public int? BookingId { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
            = DateTimeOffset.UtcNow;
    }
}
