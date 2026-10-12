namespace SkillLoop.Api.Models
{
    public class PromoCodeRedemption
    {
        public string UserId { get; set; } = string.Empty;

        public int PromoCodeId { get; set; }

        public DateTimeOffset RedeemedAt { get; set; }
            = DateTimeOffset.UtcNow;
    }
}
