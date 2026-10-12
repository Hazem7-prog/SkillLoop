using System.ComponentModel.DataAnnotations;

namespace SkillLoop.Api.Models
{
    public class ApplyPromoCodeRequestDto
    {
        [Required]
        public string Code { get; set; } = "";
    }
}
