namespace Shared.Infrastructure.DTO;

public record UpdateOrganizationDto
{
    public string Name { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public byte[] RowVersion { get; init; } = Array.Empty<byte>();
}
