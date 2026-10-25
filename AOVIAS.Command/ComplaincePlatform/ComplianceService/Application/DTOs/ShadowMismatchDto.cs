namespace ComplianceService.Application.DTOs;

public sealed class ShadowMismatchDto
{
    public Guid Id { get; init; }
    public string EntityType { get; init; } = string.Empty;
    public Guid EntityId { get; init; }
    public string Action { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
    public string MetadataJson { get; init; } = "{}";
}
