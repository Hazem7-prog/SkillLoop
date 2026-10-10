using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SkillLoop.Api.Data;
using SkillLoop.Api.DTOs;
using SkillLoop.Api.Models;

namespace SkillLoop.Api.Services;

public interface IBookingService
{
    Task<ServiceResult<BookingDto>> CreateAsync(string userId, CreateBookingRequest req);
    Task<List<BookingDto>> GetMineAsync(string userId, BookingStatus? status);
    Task<ServiceResult<BookingDto>> CancelAsync(string userId, int bookingId);
}

public class BookingService : IBookingService
{
    private readonly AppDbContext _db;
    private readonly IAvailabilityService _availability;
    private readonly IWalletService _wallet;
    private readonly IChatService _chat;
    private readonly ILogger<BookingService> _logger;

    public BookingService(AppDbContext db, IAvailabilityService availability,
        IWalletService wallet, IChatService chat, ILogger<BookingService> logger)
    {
        _db = db;
        _availability = availability;
        _wallet = wallet;
        _chat = chat;
        _logger = logger;
    }

    // ---------------------------------------------------------------- create
    public async Task<ServiceResult<BookingDto>> CreateAsync(string userId, CreateBookingRequest req)
    {
        if (req.CourseId <= 0 || req.StartTime is null || req.Type is null || !Enum.IsDefined(req.Type.Value))
            return Fail(400, "courseId, startTime and type are required.", "VALIDATION_ERROR");

        var start = req.StartTime.Value.ToUniversalTime();

        var course = await _db.Courses
            .AsNoTracking()
            .Where(c => c.Id == req.CourseId && c.Status == CourseStatus.Active)
            .Select(c => new
            {
                c.Id, c.Title, c.TeacherId, c.PricePerSession, c.DurationMinutes,
                c.MaxStudentsPerSession, c.AllowsOnline, c.AllowsOffline
            })
            .FirstOrDefaultAsync();

        if (course is null)
            return Fail(404, "Course not found.", "COURSE_NOT_FOUND");
        if (course.TeacherId == userId)
            return Fail(400, "You cannot book your own course.", "CANNOT_BOOK_OWN_COURSE");

        var typeAllowed = req.Type == BookingType.Online ? course.AllowsOnline : course.AllowsOffline;
        if (!typeAllowed)
            return Fail(400, $"This course does not offer {req.Type} sessions.", "SESSION_TYPE_NOT_ALLOWED");

        // The requested time must be one of the teacher's real slots
        var availability = await _availability.GetAsync(course.Id, DateOnly.FromDateTime(start.UtcDateTime), userId);
        var slot = availability?.Slots.FirstOrDefault(s => s.StartTime == start);
        if (slot is null)
            return Fail(400, "That time is not offered by the teacher.", "INVALID_SLOT");
        if (slot.StartTime <= DateTimeOffset.UtcNow)
            return Fail(400, "That time has already passed.", "SLOT_IN_PAST");

        // ONE transaction: check the spot + create the booking + charge credits
        await using var tx = await _db.Database.BeginTransactionAsync();

        // Lock this course's row until the transaction ends, so two students booking the
        // last spot at the same time are processed one after the other.
        await _db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT Id FROM Courses WITH (UPDLOCK, ROWLOCK) WHERE Id = {course.Id}");

        var takenBy = await _db.Bookings
            .Where(b => b.CourseId == course.Id && b.StartTime == start && b.Status != BookingStatus.Cancelled)
            .Select(b => b.StudentId)
            .ToListAsync();

        if (takenBy.Contains(userId))
            return Fail(409, "You already booked this time.", "ALREADY_BOOKED");
        if (takenBy.Count >= course.MaxStudentsPerSession)
            return Fail(409, "This time slot is full.", "SLOT_FULL");

        var booking = new Booking
        {
            CourseId = course.Id,
            StudentId = userId,
            StartTime = start,
            EndTime = start.AddMinutes(course.DurationMinutes),
            Type = req.Type.Value,
            Status = BookingStatus.Upcoming,
            PricePaid = course.PricePerSession
        };

        _db.Bookings.Add(booking);
        try
        {
            await _db.SaveChangesAsync();   // gives us booking.Id
        }
        catch (DbUpdateException)
        {
            return Fail(409, "You already booked this time.", "ALREADY_BOOKED");
        }

        var paid = await _wallet.DeductCredits(userId, course.PricePerSession, booking.Id);
        if (!paid)
            return Fail(402, "Not enough credits.", "INSUFFICIENT_CREDITS");   // leaving = rollback

        await tx.CommitAsync();

        // Messages are sent AFTER the commit: a chat problem must never undo a paid booking
        var studentName = await _db.Users.Where(u => u.Id == userId).Select(u => u.FullName).FirstOrDefaultAsync();
        var when = FormatTime(start);
        await TryNotifyAsync(userId,
            $"Your {req.Type} session for \"{course.Title}\" is confirmed for {when}.");
        await TryNotifyAsync(course.TeacherId,
            $"{studentName ?? "A student"} booked your {req.Type} session for \"{course.Title}\" on {when}.");

        var dto = await Project(_db.Bookings.AsNoTracking().Where(b => b.Id == booking.Id)).FirstAsync();
        return ServiceResult<BookingDto>.Ok(dto, 201);
    }

    // ------------------------------------------------------------------ mine
    public async Task<List<BookingDto>> GetMineAsync(string userId, BookingStatus? status)
    {
        var query = _db.Bookings.AsNoTracking().Where(b => b.StudentId == userId);
        if (status.HasValue)
            query = query.Where(b => b.Status == status.Value);

        var projected = Project(query);

        // Upcoming: soonest first. Everything else: most recent first.
        return status == BookingStatus.Upcoming
            ? await projected.OrderBy(b => b.StartTime).ToListAsync()
            : await projected.OrderByDescending(b => b.StartTime).ToListAsync();
    }

    // ---------------------------------------------------------------- cancel
    public async Task<ServiceResult<BookingDto>> CancelAsync(string userId, int bookingId)
    {
        await using var tx = await _db.Database.BeginTransactionAsync();

        // Lock the booking row so a double tap cannot refund twice
        await _db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT Id FROM Bookings WITH (UPDLOCK, ROWLOCK) WHERE Id = {bookingId}");

        var booking = await _db.Bookings
            .Include(b => b.Course)
            .FirstOrDefaultAsync(b => b.Id == bookingId && b.StudentId == userId);

        if (booking is null)
            return Fail(404, "Booking not found.", "BOOKING_NOT_FOUND");
        if (booking.Status != BookingStatus.Upcoming)
            return Fail(409, "Only upcoming bookings can be cancelled.", "BOOKING_NOT_CANCELLABLE");
        if (booking.StartTime <= DateTimeOffset.UtcNow)
            return Fail(409, "This session has already started.", "SESSION_ALREADY_STARTED");

        booking.Status = BookingStatus.Cancelled;
        await _db.SaveChangesAsync();
        await _wallet.RefundCredits(booking.Id);
        await tx.CommitAsync();

        var studentName = await _db.Users.Where(u => u.Id == userId).Select(u => u.FullName).FirstOrDefaultAsync();
        var when = FormatTime(booking.StartTime);
        await TryNotifyAsync(userId,
            $"Your booking for \"{booking.Course.Title}\" on {when} was cancelled and {booking.PricePaid} credits were refunded.");
        await TryNotifyAsync(booking.Course.TeacherId,
            $"{studentName ?? "A student"} cancelled their session for \"{booking.Course.Title}\" on {when}.");

        var dto = await Project(_db.Bookings.AsNoTracking().Where(b => b.Id == bookingId)).FirstAsync();
        return ServiceResult<BookingDto>.Ok(dto);
    }

    // --------------------------------------------------------------- helpers
    private IQueryable<BookingDto> Project(IQueryable<Booking> bookings) =>
        from b in bookings
        join u in _db.Users on b.Course.TeacherId equals u.Id
        select new BookingDto
        {
            Id = b.Id,
            CourseId = b.CourseId,
            CourseTitle = b.Course.Title,
            CoverImageUrl = b.Course.CoverImageUrl,
            TeacherId = b.Course.TeacherId,
            TeacherName = u.FullName,
            TeacherAvatarUrl = u.AvatarUrl,
            StartTime = b.StartTime,
            EndTime = b.EndTime,
            Type = b.Type,
            Status = b.Status,
            PricePaid = b.PricePaid,
            CreatedAt = b.CreatedAt
        };

    private async Task TryNotifyAsync(string userId, string text)
    {
        try { await _chat.SendSystemMessage(userId, text); }
        catch (Exception ex) { _logger.LogWarning(ex, "Could not send system message to {UserId}", userId); }
    }

    private static string FormatTime(DateTimeOffset t) =>
        t.UtcDateTime.ToString("ddd, d MMM yyyy 'at' HH:mm 'UTC'", CultureInfo.InvariantCulture);

    private static ServiceResult<BookingDto> Fail(int status, string message, string code) =>
        ServiceResult<BookingDto>.Fail(status, message, code);
}