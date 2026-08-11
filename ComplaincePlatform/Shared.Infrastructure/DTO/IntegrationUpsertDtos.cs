namespace Shared.Infrastructure.DTO;

public sealed class IntegrationUpsertRequest
{
    public Guid OrganizationId { get; set; }
    public string? ExternalId { get; set; }
    public Dictionary<string, string?> Fields { get; set; } = new();
}

public sealed class IntegrationUpsertResponse
{
    public Guid EntityId { get; set; }
    public bool WasCreated { get; set; }
}

public sealed class IntegrationRollbackRequest
{
    public Guid OrganizationId { get; set; }
    public Guid EntityId { get; set; }
    public string TargetEntity { get; set; } = string.Empty;
}
