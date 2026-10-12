namespace SkillLoop.Api.DTOs
{
    public record TransactionDto(
    int Id,
    int Amount,
    string Type,
    int? BookingId,
    DateTimeOffset CreatedAt
);
}
