namespace ComplianceService.Domain;

/// <summary>
/// Phase 4 — lightweight cross-domain graph edges for Committee/Meeting ↔ Risk/Document/Task/Deficiency.
/// Written by Gateway when governance decisions/actions carry typed links.
/// </summary>
public class GovernanceTraceLink
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string SourceEntityType { get; set; } = string.Empty;
    public Guid SourceEntityId { get; set; }
    public string SourceLabel { get; set; } = string.Empty;
    public string TargetEntityType { get; set; } = string.Empty;
    public Guid TargetEntityId { get; set; }
    public string TargetLabel { get; set; } = string.Empty;
    public string RelationshipType { get; set; } = "LinkedTo";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
