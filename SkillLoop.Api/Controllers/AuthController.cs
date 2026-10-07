using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SkillLoop.Api.DTOs;
using SkillLoop.Api.Models;
using SkillLoop.Api.Services;

namespace SkillLoop.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly ITokenService _tokens;

    public AuthController(UserManager<ApplicationUser> users, ITokenService tokens)
    {
        _users = users;
        _tokens = tokens;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        var user = new ApplicationUser
        {
            FullName = dto.FullName,
            UserName = dto.Email,
            Email = dto.Email,
            PhoneNumber = dto.Phone
        };

        var result = await _users.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
            return BadRequest(new ApiError(
                string.Join("; ", result.Errors.Select(e => e.Description)), "REGISTER_FAILED"));

        return Ok(new AuthResponse(_tokens.CreateToken(user), user.Id, user.FullName, user.Email!));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var user = await _users.FindByEmailAsync(dto.Email);
        if (user is null || !await _users.CheckPasswordAsync(user, dto.Password))
            return Unauthorized(new ApiError("Invalid email or password", "INVALID_CREDENTIALS"));

        return Ok(new AuthResponse(_tokens.CreateToken(user), user.Id, user.FullName, user.Email!));
    }
}