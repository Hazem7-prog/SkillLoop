using System.ComponentModel.DataAnnotations;

namespace SkillLoop.Api.DTOs;

public record ApiError(string Error, string Code);

public class RegisterDto
{
    [Required] public string FullName { get; set; } = "";
    [Required, EmailAddress] public string Email { get; set; } = "";
    [Required] public string Phone { get; set; } = "";
    [Required, MinLength(8)] public string Password { get; set; } = "";
    [Required, Compare(nameof(Password))] public string ConfirmPassword { get; set; } = "";
}

public class LoginDto
{
    [Required, EmailAddress] public string Email { get; set; } = "";
    [Required] public string Password { get; set; } = "";
}

public class ForgotPasswordDto
{
    [Required] public string Phone { get; set; } = "";
}

public class VerifyOtpDto
{
    [Required] public string Phone { get; set; } = "";
    [Required, StringLength(4, MinimumLength = 4)] public string Code { get; set; } = "";
}

public class ResetPasswordDto
{
    [Required] public string Phone { get; set; } = "";
    [Required] public string ResetToken { get; set; } = "";
    [Required, MinLength(8)] public string NewPassword { get; set; } = "";
    [Required, Compare(nameof(NewPassword))] public string ConfirmPassword { get; set; } = "";
}

public record AuthResponse(string Token, string UserId, string FullName, string Email);