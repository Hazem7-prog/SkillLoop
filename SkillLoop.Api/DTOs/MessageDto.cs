namespace SkillLoop.Api.DTOs
{
    public record MessageDto(
    int Id,
    string SenderId,
    string Text,
    DateTimeOffset SentAt,
    bool IsRead
);
}
