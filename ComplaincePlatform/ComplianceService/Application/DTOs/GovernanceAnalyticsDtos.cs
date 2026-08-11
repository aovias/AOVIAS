namespace ComplianceService.Application.DTOs;

/// <summary>Phase 4 Sprint 6 — domain-side governance KPI enrichment (best-effort).</summary>
public sealed class GovernanceDomainKpisDto
{
    public int DocumentsEffective { get; init; }
    public int TrainingOverdue { get; init; }
    public int OpenIncidents { get; init; }
    public int OpenRisks { get; init; }
    public string Notes { get; init; } = string.Empty;
}

public sealed class UpsertGovernanceTraceLinkRequest
{
    public Guid OrganizationId { get; init; }
    public string SourceEntityType { get; init; } = string.Empty;
    public Guid SourceEntityId { get; init; }
    public string SourceLabel { get; init; } = string.Empty;
    public string TargetEntityType { get; init; } = string.Empty;
    public Guid TargetEntityId { get; init; }
    public string TargetLabel { get; init; } = string.Empty;
    public string RelationshipType { get; init; } = "LinkedTo";
}
