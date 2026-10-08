namespace SkillLoop.Api.Services;

// TEMPORARY: always succeeds. Remove when C's real WalletService is merged.
public class FakeWalletService : IWalletService
{
    public Task<bool> DeductCredits(string userId, int amount, int bookingId) => Task.FromResult(true);
    public Task RefundCredits(int bookingId) => Task.CompletedTask;
    public Task PayTeacher(int bookingId) => Task.CompletedTask;
}