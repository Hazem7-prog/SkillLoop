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

public record AuthResponse(string Token, string UserId, string FullName, string Email);