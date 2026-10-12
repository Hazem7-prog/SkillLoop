using System.ComponentModel.DataAnnotations;

namespace SkillLoop.Api.DTOs
{
    public class BuyCreditsRequestDto
    {
        [Range(1, 1000)]
        public int Credits { get; set; }
    }
}
