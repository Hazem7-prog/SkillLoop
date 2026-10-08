using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SkillLoop.Api.DTOs;
using SkillLoop.Api.Models;

namespace SkillLoop.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private static readonly Dictionary<string, string> AllowedImages = new()
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp"
    };

    private readonly UserManager<ApplicationUser> _users;
    public UsersController(UserManager<ApplicationUser> users) => _users = users;

    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    private static MyProfileDto ToMy(ApplicationUser u) => new(
        u.Id, u.FullName, u.Email!, u.PhoneNumber,
        u.JobTitle, u.Location, u.Bio, u.AvatarUrl, u.CreatedAt);

    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var user = await _users.FindByIdAsync(CurrentUserId);
        if (user is null) return NotFound(new ApiError("User not found", "USER_NOT_FOUND"));
        return Ok(ToMy(user));
    }

    [HttpPut("me")]
    public async Task<IActionResult> Update(UpdateProfileDto dto)
    {
        var user = await _users.FindByIdAsync(CurrentUserId);
        if (user is null) return NotFound(new ApiError("User not found", "USER_NOT_FOUND"));

        user.FullName = dto.FullName;
        user.JobTitle = dto.JobTitle;
        user.Location = dto.Location;
        user.Bio = dto.Bio;

        var result = await _users.UpdateAsync(user);
        if (!result.Succeeded)
            return BadRequest(new ApiError(
                string.Join("; ", result.Errors.Select(e => e.Description)), "UPDATE_FAILED"));

        return Ok(ToMy(user));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var u = await _users.FindByIdAsync(id);
        if (u is null) return NotFound(new ApiError("User not found", "USER_NOT_FOUND"));

        return Ok(new PublicProfileDto(
            u.Id, u.FullName, u.JobTitle, u.Location, u.Bio, u.AvatarUrl, u.CreatedAt));
    }

    [HttpPost("me/avatar")]
    public async Task<IActionResult> UploadAvatar(IFormFile file, [FromServices] IWebHostEnvironment env)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new ApiError("No file uploaded", "NO_FILE"));
        if (file.Length > 2 * 1024 * 1024)
            return BadRequest(new ApiError("File must be 2MB or less", "FILE_TOO_LARGE"));
        if (!AllowedImages.TryGetValue(file.ContentType, out var ext))
            return BadRequest(new ApiError("Only JPG, PNG or WEBP allowed", "INVALID_FILE_TYPE"));

        var user = await _users.FindByIdAsync(CurrentUserId);
        if (user is null) return NotFound(new ApiError("User not found", "USER_NOT_FOUND"));

        var root = env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");
        var folder = Path.Combine(root, "avatars");
        Directory.CreateDirectory(folder);

        var fileName = $"{Guid.NewGuid()}{ext}";
        await using (var stream = System.IO.File.Create(Path.Combine(folder, fileName)))
            await file.CopyToAsync(stream);

        user.AvatarUrl = $"/avatars/{fileName}";
        await _users.UpdateAsync(user);

        return Ok(new { avatarUrl = user.AvatarUrl });
    }
}