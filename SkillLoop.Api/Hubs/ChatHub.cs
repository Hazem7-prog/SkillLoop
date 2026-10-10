using Microsoft.AspNetCore.SignalR;
using SkillLoop.Api.DTOs;
using SkillLoop.Api.Services;
using System.Security.Claims;

namespace SkillLoop.Api.Hubs
{
    public class ChatHub(IConversationService _conversationService) : Hub
    {
        public async Task JoinConversation(int conversationId)
        {
            var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                throw new HubException("Unauthorized");

            var allowed = await _conversationService.IsParticipantAsync(conversationId, userId);

            if (!allowed)
                throw new HubException("Access denied");

            await Groups.AddToGroupAsync(Context.ConnectionId, $"conversation-{conversationId}");

        }

        public async Task SendMessage(int conversationId, string text)
        {
            var userId = Context.User?
                .FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                throw new HubException("Unauthorized");

            MessageDto? message;

            try
            {
                message = await _conversationService
                    .SendMessageAsync(conversationId, userId, text);
            }
            catch (ArgumentException ex)
            {
                throw new HubException(ex.Message);
            }

            if (message == null)
                throw new HubException("Access denied");

            await Clients.Group($"conversation-{conversationId}")
                .SendAsync("ReceiveMessage", message);
        }
    }
}
