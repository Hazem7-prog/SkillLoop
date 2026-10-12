namespace SkillLoop.Api.Services;

// Owned by C, used by B (agreed in the API contract, section 7)
public interface IWalletService
{
    Task<bool> DeductCredits(string userId, int amount, int bookingId);
    Task RefundCredits(int bookingId);
    Task PayTeacher(int bookingId);

   
    
}