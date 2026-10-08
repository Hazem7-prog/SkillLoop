namespace SkillLoop.Api.Services;

// TEMPORARY: does nothing. Remove when C's real ChatService is merged.
public class FakeChatService : IChatService
{
    public Task SendSystemMessage(string userId, string text) => Task.CompletedTask;
}