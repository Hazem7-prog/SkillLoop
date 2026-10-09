using SkillLoop.Api.DTOs;

namespace SkillLoop.Api.Services
{
    public interface IConversationService
    {
        Task<List<ConversationDto>> GetUserConversationsAsync(string userId);

        Task CreateConversationAsync(string userId, string otherParticipantId, int? courseId);
    }
}
