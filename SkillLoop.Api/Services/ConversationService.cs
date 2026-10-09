using Microsoft.EntityFrameworkCore;
using SkillLoop.Api.Data;
using SkillLoop.Api.DTOs;

namespace SkillLoop.Api.Services
{

    public class ConversationService : IConversationService
    {
        private readonly AppDbContext _context;

        public ConversationService(AppDbContext context)
        {
            _context = context;
        }

        public Task CreateConversationAsync(string userId, string otherParticipantId, int? courseId)
        {
            throw new NotImplementedException();
        }

        public async Task<List<ConversationDto>>
            GetUserConversationsAsync(string userId)
        {
            return await _context.Conversations
                .AsNoTracking()
                .Where(c =>
                    c.Participant1Id == userId ||
                    c.Participant2Id == userId)
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new ConversationDto(
                    c.Id,
                    c.CourseId,
                    c.CreatedAt,
                    c.Participant1Id == userId
                        ? c.Participant2Id
                        : c.Participant1Id
                ))
                .ToListAsync();
        }
    }
}
