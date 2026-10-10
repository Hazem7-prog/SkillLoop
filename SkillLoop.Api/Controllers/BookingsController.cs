using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillLoop.Api.DTOs;
using SkillLoop.Api.Models;
using SkillLoop.Api.Services;

namespace SkillLoop.Api.Controllers;

[ApiController]
[Route("api/bookings")]
[Authorize]
public class BookingsController : ControllerBase
{
    private readonly IBookingService _bookings;

    public BookingsController(IBookingService bookings) => _bookings = bookings;

    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    // POST /api/bookings
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateBookingRequest req) =>
        ToResult(await _bookings.CreateAsync(CurrentUserId, req));

    // GET /api/bookings/mine?status=Upcoming
    [HttpGet("mine")]
    public async Task<ActionResult<List<BookingDto>>> Mine([FromQuery] BookingStatus? status) =>
        Ok(await _bookings.GetMineAsync(CurrentUserId, status));

    // POST /api/bookings/{id}/cancel
    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id) =>
        ToResult(await _bookings.CancelAsync(CurrentUserId, id));

    private IActionResult ToResult(ServiceResult<BookingDto> result) =>
        result.Success
            ? StatusCode(result.StatusCode, result.Value)
            : StatusCode(result.StatusCode, result.Error);
}