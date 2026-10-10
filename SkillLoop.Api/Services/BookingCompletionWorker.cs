using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SkillLoop.Api.Data;
using SkillLoop.Api.Models;

namespace SkillLoop.Api.Services;

// Every minute: Upcoming bookings whose end time has passed become Completed,
// and the teacher is paid (hold at booking, pay after completion).
public class BookingCompletionWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<BookingCompletionWorker> _logger;

    public BookingCompletionWorker(IServiceScopeFactory scopes, ILogger<BookingCompletionWorker> logger)
    {
        _scopes = scopes;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        do
        {
            try
            {
                await CompleteFinishedBookings(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Booking completion run failed");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task CompleteFinishedBookings(CancellationToken ct)
    {
        using var scope = _scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var wallet = scope.ServiceProvider.GetRequiredService<IWalletService>();

        var now = DateTimeOffset.UtcNow;
        var ids = await db.Bookings
            .Where(b => b.Status == BookingStatus.Upcoming && b.EndTime <= now)
            .Select(b => b.Id)
            .ToListAsync(ct);

        foreach (var id in ids)
        {
            try
            {
                await using var tx = await db.Database.BeginTransactionAsync(ct);

                var booking = await db.Bookings.FirstAsync(b => b.Id == id, ct);
                if (booking.Status == BookingStatus.Upcoming)
                {
                    booking.Status = BookingStatus.Completed;
                    await db.SaveChangesAsync(ct);
                    await wallet.PayTeacher(id);
                    await tx.CommitAsync(ct);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Could not complete booking {BookingId}", id);
            }
            finally
            {
                db.ChangeTracker.Clear();
            }
        }
    }
}