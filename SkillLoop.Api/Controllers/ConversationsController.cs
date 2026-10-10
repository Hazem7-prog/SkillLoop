using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SkillLoop.Api.Services;
using System.Security.Claims;

namespace SkillLoop.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ConversationsController : ControllerBase
    {
        private readonly IConversationService _conversationService;

        public ConversationsController(IConversationService conversationService)
        {
            _conversationService = conversationService;
        }

        [HttpGet]
        public async Task<IActionResult> GetConversations([FromQuery] string? filter = null)
        {
            var userId = User.FindFirstValue( ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            filter = filter?.Trim().ToLowerInvariant();

            if (!string.IsNullOrEmpty(filter) &&
                filter != "read" &&
                filter != "unread")
            {
                return BadRequest(new
                {
                    error = "Filter must be read or unread",
                    code = "INVALID_FILTER"
                });
            }

            var conversations = await _conversationService
                .GetUserConversationsAsync(userId, filter);

            return Ok(conversations);
        }



        [HttpPost]
        public async Task<IActionResult> CreateConversation( string otherParticipantId, int? courseId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            try
            {
                await _conversationService.CreateConversationAsync(userId, otherParticipantId,courseId);

                return Ok("Conversation created or already exists");
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message, code = "INVALID_PARTICIPANT" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message, code = "NOT_FOUND" });
            }
        }



        [HttpGet("{id}/messages")]
        [Authorize]
        public async Task<IActionResult> GetConversationMessages(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var messages = await _conversationService
                .GetConversationMessagesAsync(id, userId);

            if (messages == null)
                return NotFound(new
                {
                    error = "Conversation not found",
                    code = "CONVERSATION_NOT_FOUND"
                });

            return Ok(messages);
        }


        [HttpPost("{id}/read")]
        [Authorize]

        public async Task<IActionResult>  MarkAsReadASync(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var result = await _conversationService
                .MarkAsReadASync(id, userId);


            return Ok(result);
        }

    }
}
