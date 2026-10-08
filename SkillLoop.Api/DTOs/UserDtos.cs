using System.ComponentModel.DataAnnotations;

namespace SkillLoop.Api.DTOs;

public record MyProfileDto(
    string Id, string FullName, string Email, string? Phone,
    string? JobTitle, string? Location, string? Bio,
    string? AvatarUrl, DateTimeOffset CreatedAt);

public record PublicProfileDto(
    string Id, string FullName, string? JobTitle, string? Location,
    string? Bio, string? AvatarUrl, DateTimeOffset CreatedAt);

public class UpdateProfileDto
{
    [Required, StringLength(100)] public string FullName { get; set; } = "";
    [StringLength(100)] public string? JobTitle { get; set; }
    [StringLength(100)] public string? Location { get; set; }
    [StringLength(500)] public string? Bio { get; set; }
}