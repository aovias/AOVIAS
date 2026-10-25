namespace Shared.Infrastructure.DTO;

public record AddMemberDto
{
    public Guid UserId { get; init; }
    public string Role { get; init; } = string.Empty;
}
