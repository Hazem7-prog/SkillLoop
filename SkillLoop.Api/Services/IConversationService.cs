using SkillLoop.Api.DTOs;

namespace SkillLoop.Api.Services
{
    public interface IConversationService
    {
        Task<List<ConversationDto>> GetUserConversationsAsync(string userId , string? filter);

        Task CreateConversationAsync(string userId, string otherParticipantId, int? courseId);


        Task<List<MessageDto>?> GetConversationMessagesAsync(int conversationId,string userId);

        Task<int?> MarkAsReadASync(int conversationId , string userId);


        Task<bool> IsParticipantAsync(int conversationId, string userId);

        Task<MessageDto?> SendMessageAsync( int conversationId,string userId, string text);

    }
}
