namespace SkillLoop.Api.Models
{
    public class PromoCode
    {
        public int Id { get; set; }

        public string Code { get; set; } = string.Empty;

        public int Credits { get; set; }

        public DateTimeOffset ExpiryDate { get; set; }
    }
}
