
using Microsoft.EntityFrameworkCore;
using SkillLoop.Api.Data;
using SkillLoop.Api.DTOs;
using SkillLoop.Api.Models;

namespace SkillLoop.Api.Services;

public class WalletService : IWalletService
{
    private readonly AppDbContext _context;

    public WalletService(AppDbContext context)
    {
        _context = context;
    }

   
    public async Task<int> GetBalanceAsync(string userId)
    {
        var wallet = await _context.Wallets
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.UserId == userId);

        return wallet?.Balance ?? 0;
    }

    
    public async Task<int> BuyCreditsAsync(
        string userId, int credits)
    {
        if (credits < 1 || credits > 1000)
            throw new ArgumentException("Invalid credits amount");

        await using var transaction = await _context.Database
            .BeginTransactionAsync(
                System.Data.IsolationLevel.Serializable);

        var wallet = await _context.Wallets
            .FirstOrDefaultAsync(w => w.UserId == userId);

        if (wallet == null)
        {
            wallet = new Wallet
            {
                UserId = userId,
                Balance = 0
            };

            _context.Wallets.Add(wallet);
        }

        if (wallet.Balance > int.MaxValue - credits)
            throw new InvalidOperationException(  "Wallet balance limit exceeded");

        wallet.Balance += credits;

        _context.Transactions.Add(new Transaction
        {
            UserId = userId,
            Amount = credits,
            Type = TransactionType.Purchase,
            CreatedAt = DateTimeOffset.UtcNow
        });

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return wallet.Balance;
    }

 
    public async Task<bool> DeductCredits(
        string userId, int amount, int bookingId)
    {
        if (amount < 0)
            throw new ArgumentException("Invalid amount");

        if (_context.Database.CurrentTransaction == null)
            throw new InvalidOperationException( "Database transaction required");

        var booking = await _context.Bookings
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == bookingId);

        if (booking == null ||
            booking.StudentId != userId ||
            booking.PricePaid != amount ||
            booking.Status != BookingStatus.Upcoming)
        {
            throw new InvalidOperationException("Invalid booking payment");
        }

        if (amount == 0)
            return true;

        var alreadyPaid = await _context.Transactions
            .AnyAsync(t =>
                t.BookingId == bookingId &&
                t.Type == TransactionType.SessionBooking);

        if (alreadyPaid)
            throw new InvalidOperationException("Booking already paid");

        var updated = await _context.Wallets
            .Where(w => w.UserId == userId &&
                        w.Balance >= amount)
            .ExecuteUpdateAsync(s => s
                .SetProperty(
                    w => w.Balance,
                    w => w.Balance - amount));

        if (updated == 0)
            return false;

        _context.Transactions.Add(new Transaction
        {
            UserId = userId,
            Amount = -amount,
            Type = TransactionType.SessionBooking,
            BookingId = bookingId,
            CreatedAt = DateTimeOffset.UtcNow
        });

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task RefundCredits(int bookingId)
    {
        if (_context.Database.CurrentTransaction == null)
            throw new InvalidOperationException("Database transaction required");

        var booking = await _context.Bookings
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == bookingId);

        if (booking == null)
            throw new InvalidOperationException("Booking not found");

        if (booking.Status != BookingStatus.Cancelled)
            throw new InvalidOperationException( "Booking is not cancelled");

        if (booking.PricePaid == 0)
            return;

        var payment = await _context.Transactions
            .AsNoTracking()
            .FirstOrDefaultAsync(t =>
                t.BookingId == bookingId &&
                t.Type == TransactionType.SessionBooking);

        if (payment == null ||
            payment.UserId != booking.StudentId ||
            payment.Amount != -booking.PricePaid)
        {
            throw new InvalidOperationException("Invalid booking payment");
        }

        var alreadyRefunded = await _context.Transactions
            .AnyAsync(t =>
                t.BookingId == bookingId &&
                t.Type == TransactionType.Refund);

        if (alreadyRefunded)
            return;

        var amount = -payment.Amount;

        var updated = await _context.Wallets
            .Where(w => w.UserId == payment.UserId &&
                        w.Balance <= int.MaxValue - amount)
            .ExecuteUpdateAsync(s => s
                .SetProperty(
                    w => w.Balance,
                    w => w.Balance + amount));

        if (updated == 0)
            throw new InvalidOperationException( "Cannot refund wallet");

        _context.Transactions.Add(new Transaction
        {
            UserId = payment.UserId,
            Amount = amount,
            Type = TransactionType.Refund,
            BookingId = bookingId,
            CreatedAt = DateTimeOffset.UtcNow
        });

        await _context.SaveChangesAsync();
    }

    
    public async Task PayTeacher(int bookingId)
    {
        if (_context.Database.CurrentTransaction == null)
            throw new InvalidOperationException( "Database transaction required");

        var booking = await _context.Bookings
            .AsNoTracking()
            .Where(b => b.Id == bookingId)
            .Select(b => new
            {
                b.PricePaid,
                b.Status,
                TeacherId = b.Course.TeacherId
            })
            .FirstOrDefaultAsync();

        if (booking == null)
            throw new InvalidOperationException( "Booking not found");

        if (booking.Status != BookingStatus.Completed)
            throw new InvalidOperationException( "Session not completed");

        if (booking.PricePaid < 0)
            throw new InvalidOperationException("Invalid booking price");

        if (booking.PricePaid == 0)
            return;

        var paymentExists = await _context.Transactions
            .AnyAsync(t =>
                t.BookingId == bookingId &&
                t.Type == TransactionType.SessionBooking &&
                t.Amount == -booking.PricePaid);

        if (!paymentExists)
            throw new InvalidOperationException( "Booking was not paid");

        var refunded = await _context.Transactions
            .AnyAsync(t =>
                t.BookingId == bookingId &&
                t.Type == TransactionType.Refund);

        if (refunded)
            throw new InvalidOperationException("Booking was refunded");

        var alreadyPaid = await _context.Transactions
            .AnyAsync(t =>
                t.BookingId == bookingId &&
                t.Type == TransactionType.SessionEarning);

        if (alreadyPaid)
            return;

       
        var teacherExists = await _context.Users
            .AnyAsync(u => u.Id == booking.TeacherId);

        if (!teacherExists)
            throw new InvalidOperationException("Teacher not found");

       
        await _context.Database.ExecuteSqlInterpolatedAsync($@"
            INSERT INTO Wallets (UserId, Balance)
            SELECT {booking.TeacherId}, 0
            WHERE NOT EXISTS (
                SELECT 1
                FROM Wallets WITH (UPDLOCK, HOLDLOCK)
                WHERE UserId = {booking.TeacherId}
            )");

        var updated = await _context.Wallets
            .Where(w => w.UserId == booking.TeacherId &&
                        w.Balance <= int.MaxValue - booking.PricePaid)
            .ExecuteUpdateAsync(s => s
                .SetProperty(
                    w => w.Balance,
                    w => w.Balance + booking.PricePaid));

        if (updated == 0)
            throw new InvalidOperationException("Cannot credit teacher wallet");

        _context.Transactions.Add(new Transaction
        {
            UserId = booking.TeacherId,
            Amount = booking.PricePaid,
            Type = TransactionType.SessionEarning,
            BookingId = bookingId,
            CreatedAt = DateTimeOffset.UtcNow
        });

        await _context.SaveChangesAsync();
    }

    
    public async Task<List<TransactionDto>> GetTransactionsAsync(
        string userId)
    {
        var transactions = await _context.Transactions
            .AsNoTracking()
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        return transactions.Select(t => new TransactionDto(
            t.Id,
            t.Amount,
            t.Type.ToString(),
            t.BookingId,
            t.CreatedAt
        )).ToList();
    }

    
    public async Task<int?> ApplyPromoCodeAsync(
        string userId, string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return null;

        await using var transaction = await _context.Database
            .BeginTransactionAsync(
                System.Data.IsolationLevel.Serializable);

        var normalizedCode = code.Trim().ToUpperInvariant();

        var promo = await _context.PromoCodes
            .FirstOrDefaultAsync(p =>
                p.Code.ToUpper() == normalizedCode);

        if (promo == null ||
            promo.ExpiryDate <= DateTimeOffset.UtcNow ||
            promo.Credits <= 0)
        {
            return null;
        }

        var alreadyUsed = await _context.PromoCodeRedemptions
            .AnyAsync(r =>
                r.UserId == userId &&
                r.PromoCodeId == promo.Id);

        if (alreadyUsed)
            return null;

        var wallet = await _context.Wallets
            .FirstOrDefaultAsync(w => w.UserId == userId);

        if (wallet == null)
        {
            wallet = new Wallet
            {
                UserId = userId,
                Balance = 0
            };

            _context.Wallets.Add(wallet);
        }

        if (wallet.Balance > int.MaxValue - promo.Credits)
            throw new InvalidOperationException(
                "Wallet balance limit exceeded");

        wallet.Balance += promo.Credits;

        _context.PromoCodeRedemptions.Add(
            new PromoCodeRedemption
            {
                UserId = userId,
                PromoCodeId = promo.Id,
                RedeemedAt = DateTimeOffset.UtcNow
            });

        _context.Transactions.Add(new Transaction
        {
            UserId = userId,
            Amount = promo.Credits,
            Type = TransactionType.PromoCode,
            CreatedAt = DateTimeOffset.UtcNow
        });

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return wallet.Balance;
    }
}
