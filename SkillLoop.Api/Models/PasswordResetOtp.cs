namespace SkillLoop.Api.Models;

public class PasswordResetOtp
{
    public int Id { get; set; }
    public string UserId { get; set; } = "";
    public string Code { get; set; } = "";
    public DateTimeOffset ExpiresAt { get; set; }
    public int Attempts { get; set; }
    public bool IsUsed { get; set; }
}