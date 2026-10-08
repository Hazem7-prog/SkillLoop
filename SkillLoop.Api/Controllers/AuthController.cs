using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SkillLoop.Api.DTOs;
using SkillLoop.Api.Models;
using SkillLoop.Api.Services;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using SkillLoop.Api.Data;

namespace SkillLoop.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly ITokenService _tokens;
    private readonly AppDbContext _db;
    private readonly ILogger<AuthController> _logger;

    public AuthController(UserManager<ApplicationUser> users, ITokenService tokens,
        AppDbContext db, ILogger<AuthController> logger)
    {
        _users = users;
        _tokens = tokens;
        _db = db;
        _logger = logger;
    }   

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        if (await _users.Users.AnyAsync(u => u.PhoneNumber == dto.Phone))
            return BadRequest(new ApiError("Phone number already in use", "PHONE_EXISTS"));

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

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordDto dto)
    {
        var user = await _users.Users.FirstOrDefaultAsync(u => u.PhoneNumber == dto.Phone);
        if (user is not null)
        {
            // إلغاء أي كود قديم
            await _db.PasswordResetOtps
                .Where(o => o.UserId == user.Id && !o.IsUsed)
                .ExecuteUpdateAsync(s => s.SetProperty(o => o.IsUsed, true));

            var code = RandomNumberGenerator.GetInt32(1000, 10000).ToString();
            _db.PasswordResetOtps.Add(new PasswordResetOtp
            {
                UserId = user.Id,
                Code = code,
                ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(5)
            });
            await _db.SaveChangesAsync();

            // مؤقت: بدل الـ SMS. يتشال لما نربط مزود SMS
            _logger.LogInformation("OTP for {Phone}: {Code}", dto.Phone, code);
        }

        // نفس الرد دايماً، عشان محدش يعرف مين مسجّل
        return Ok(new { message = "If the number exists, a code was sent." });
    }

    [HttpPost("verify-otp")]
    public async Task<IActionResult> VerifyOtp(VerifyOtpDto dto)
    {
        var invalid = new ApiError("Invalid or expired code", "INVALID_OTP");

        var user = await _users.Users.FirstOrDefaultAsync(u => u.PhoneNumber == dto.Phone);
        if (user is null) return BadRequest(invalid);

        var otp = await _db.PasswordResetOtps
            .Where(o => o.UserId == user.Id && !o.IsUsed && o.ExpiresAt > DateTimeOffset.UtcNow)
            .OrderByDescending(o => o.Id)
            .FirstOrDefaultAsync();

        if (otp is null || otp.Attempts >= 5) return BadRequest(invalid);

        if (otp.Code != dto.Code)
        {
            otp.Attempts++;
            await _db.SaveChangesAsync();
            return BadRequest(invalid);
        }

        otp.IsUsed = true;
        await _db.SaveChangesAsync();

        var resetToken = await _users.GeneratePasswordResetTokenAsync(user);
        return Ok(new { resetToken });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordDto dto)
    {
        var user = await _users.Users.FirstOrDefaultAsync(u => u.PhoneNumber == dto.Phone);
        if (user is null)
            return BadRequest(new ApiError("Invalid request", "INVALID_REQUEST"));

        var result = await _users.ResetPasswordAsync(user, dto.ResetToken, dto.NewPassword);
        if (!result.Succeeded)
            return BadRequest(new ApiError(
                string.Join("; ", result.Errors.Select(e => e.Description)), "RESET_FAILED"));

        return Ok(new { message = "Password updated" });
    }
}