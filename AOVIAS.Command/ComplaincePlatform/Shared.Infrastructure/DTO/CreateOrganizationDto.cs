namespace Shared.Infrastructure.DTO;

public record CreateOrganizationDto
{
    public string Name { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
}
