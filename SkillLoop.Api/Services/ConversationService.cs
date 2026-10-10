using Microsoft.EntityFrameworkCore;
using SkillLoop.Api.Data;
using SkillLoop.Api.DTOs;
using SkillLoop.Api.Models;

namespace SkillLoop.Api.Services
{

    public class ConversationService : IConversationService
    {
        private readonly AppDbContext _context;

        public ConversationService(AppDbContext context)
        {
            _context = context;
        }


        public async Task CreateConversationAsync( string userId, string otherParticipantId,int? courseId)
        {
            if (userId == otherParticipantId)
                throw new ArgumentException("Cannot chat with yourself");

            var userExists = await _context.Users
                .AnyAsync(u => u.Id == otherParticipantId);

            if (!userExists)
                throw new KeyNotFoundException("User not found");

            if (courseId.HasValue &&
                !await _context.Courses.AnyAsync(c => c.Id == courseId.Value))
                throw new KeyNotFoundException("Course not found");

            var exists = await _context.Conversations.AnyAsync(c =>
                    c.CourseId == courseId &&
                    ((c.Participant1Id == userId &&
                      c.Participant2Id == otherParticipantId) ||
                     (c.Participant1Id == otherParticipantId &&
                      c.Participant2Id == userId)));

            if (exists)
                return;

            var conversation = new Conversation
            {
                Participant1Id = userId,
                Participant2Id = otherParticipantId,
                CourseId = courseId,
                CreatedAt = DateTimeOffset.UtcNow
            };

            _context.Conversations.Add(conversation);

            await _context.SaveChangesAsync();
        }

        public async Task<List<MessageDto>?> GetConversationMessagesAsync(int conversationId, string userId)
        {
            var conversationExists = await _context.Conversations.AnyAsync(c => c.Id == conversationId &&(c.Participant1Id == userId || c.Participant2Id == userId));

            if (!conversationExists)
                return null;

            var messages = await _context.Messages
                .Where(m => m.ConversationId == conversationId)
                .OrderBy(m => m.SentAt)
                .Select(m => new MessageDto(
                    m.Id,
                    m.SenderId,
                    m.Text,
                    m.SentAt,
                    m.IsRead
                ))
                .ToListAsync();

            return messages;
        }

        public async Task<List<ConversationDto>> GetUserConversationsAsync(string userId)
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

        public  async Task<int?> MarkAsReadASync(int conversationId, string userId)
        {
            var conversationExists = await _context.Conversations.AnyAsync(c => c.Id == conversationId && (c.Participant1Id == userId || c.Participant2Id == userId));

            var messages = await _context.Messages.Where(m => m.ConversationId == conversationId && m.SenderId != userId && !m.IsRead) .ToListAsync();

            foreach (var message in messages)
            {
                message.IsRead = true;
            }

            await _context.SaveChangesAsync();

            return messages.Count;
        }
    }
}
