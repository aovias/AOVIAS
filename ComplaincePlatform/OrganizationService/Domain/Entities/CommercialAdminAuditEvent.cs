namespace OrganizationService.Domain.Entities;

/// <summary>CRP Phase 4 — durable commercial administration audit trail.</summary>
public sealed class CommercialAdminAuditEvent
{
    public Guid Id { get; set; }
    public Guid? OrganizationId { get; set; }
    public Guid? OperatorUserId { get; set; }
    public string OperatorEmail { get; set; } = string.Empty;
    public string TargetType { get; set; } = string.Empty;
    public string? TargetId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? PreviousJson { get; set; }
    public string? NewJson { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? CorrelationId { get; set; }
    public DateTime OccurredAtUtc { get; set; }
}
