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
        public async Task<IActionResult> GetConversations()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var conversations =
                await _conversationService
                    .GetUserConversationsAsync(userId);

            return Ok(conversations);
        }
    }
}
