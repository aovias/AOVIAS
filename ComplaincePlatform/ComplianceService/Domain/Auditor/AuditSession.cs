using ComplianceService.Domain.Enums;
using Shared.Infrastructure.Domain;

namespace ComplianceService.Domain.Auditor;

/// <summary>
/// External auditor assessment session — independent assessment layer (Sprint 14).
/// </summary>
public class AuditSession : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid? HospitalId { get; set; }
    public Guid? FrameworkId { get; set; }

    public string SessionNumber { get; set; } = string.Empty;
    public AuditType AuditType { get; set; } = AuditType.InternalAudit;
    public string Scope { get; set; } = string.Empty;

    public Guid? LeadAuditorUserId { get; set; }
    public string? LeadAuditorName { get; set; }

    /// <summary>JSON array of { userId, name, role } team members.</summary>
    public string? AuditorTeamJson { get; set; }

    public DateTime? PlannedStartDate { get; set; }
    public DateTime? PlannedEndDate { get; set; }
    public DateTime? ActualStartDate { get; set; }
    public DateTime? ActualEndDate { get; set; }

    public AuditSessionStatus Status { get; set; } = AuditSessionStatus.Planned;
    public AuditSessionResult OverallResult { get; set; } = AuditSessionResult.NotDetermined;
    public string? Summary { get; set; }

    public Guid? CreatedByUserId { get; set; }
    public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;
    public Guid? LastUpdatedBy { get; set; }

    /// <summary>Optional link to annual/monthly scheduled audit program row.</summary>
    public Guid? ScheduledAuditId { get; set; }

    /// <summary>Legacy AI/workflow program id (ComplianceProgram) for generate-facade compatibility.</summary>
    public Guid? ComplianceProgramId { get; set; }

    public ICollection<AuditObservation> Observations { get; set; } = new List<AuditObservation>();
    public ICollection<AuditComment> Comments { get; set; } = new List<AuditComment>();

    public void TransitionStatus(AuditSessionStatus newStatus, Guid? updatedBy = null)
    {
        Status = newStatus;
        LastUpdatedAt = DateTime.UtcNow;
        LastUpdatedBy = updatedBy;
    }

    /// <summary>
    /// Submitted / Closed sessions reject new findings and observations.
    /// </summary>
    public void EnsureAcceptsFindings()
    {
        if (Status is AuditSessionStatus.Closed or AuditSessionStatus.Submitted)
        {
            throw new InvalidOperationException(
                $"Cannot add findings to audit session {Id} when status is {Status}.");
        }
    }
}
