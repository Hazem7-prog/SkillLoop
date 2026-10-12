using Microsoft.AspNetCore.SignalR;
using SkillLoop.Api.Data;
using SkillLoop.Api.Hubs;
using SkillLoop.Api.Models;

namespace SkillLoop.Api.Services
{

    public class ChatService : IChatService
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly ILogger<ChatService> _logger;

        public ChatService(
            AppDbContext context,
            IHubContext<ChatHub> hubContext,
            ILogger<ChatService> logger)
        {
            _context = context;
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task SendSystemMessage(string userId, string text)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("Invalid user ID");

            if (string.IsNullOrWhiteSpace(text))
                throw new ArgumentException("Message cannot be empty");

           
            var notification = new Notification
            {
                UserId = userId,
                Text = text,
                CreatedAt = DateTimeOffset.UtcNow
            };

            _context.Notifications.Add(notification);

            await _context.SaveChangesAsync();

            
            try
            {
                await _hubContext.Clients.User(userId)
                    .SendAsync("ReceiveSystemMessage", new
                    {
                        notification.Id,
                        notification.Text,
                        notification.CreatedAt
                    });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(  ex,  "Failed to send real-time notification to {UserId}", userId);
            }
        }
    }
}
