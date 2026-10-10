using SkillLoop.Api.DTOs;

namespace SkillLoop.Api.Services
{
    public interface IConversationService
    {
        Task<List<ConversationDto>> GetUserConversationsAsync(string userId);

        Task CreateConversationAsync(string userId, string otherParticipantId, int? courseId);


        Task<List<MessageDto>?> GetConversationMessagesAsync(int conversationId,string userId);

        Task<int?> MarkAsReadASync(int conversationId , string userId); 


    }
}
