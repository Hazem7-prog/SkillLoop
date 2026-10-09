namespace SkillLoop.Api.DTOs
{
    public record ConversationDto(
     int Id,
     int? CourseId,
     DateTimeOffset CreatedAt,
     string OtherParticipantId
 );
}
