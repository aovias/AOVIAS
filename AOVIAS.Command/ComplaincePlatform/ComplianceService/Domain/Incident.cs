using ComplianceService.Domain.Enums;
using Shared.Infrastructure.Domain;
using Shared.Infrastructure.Domain.Enums;

namespace ComplianceService.Domain;

/// <summary>
/// Sprint 16 — Patient safety incident aggregate (operational capture layer).
/// Incidents never auto-promote to deficiencies; human approval required.
/// </summary>
public class Incident : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid? HospitalId { get; set; }

    public string IncidentNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public IncidentType IncidentType { get; set; } = IncidentType.AdverseEvent;
    public IncidentSeverity Severity { get; set; } = IncidentSeverity.Moderate;
    public IncidentPriority Priority { get; set; } = IncidentPriority.Routine;
    public IncidentCategory Category { get; set; } = IncidentCategory.Other;

    public string? Location { get; set; }
    public FindingDepartment Department { get; set; } = FindingDepartment.Clinical;

    public Guid? ReporterUserId { get; set; }
    public string? ReporterName { get; set; }
    public bool IsAnonymous { get; set; }

    public bool PatientInvolved { get; set; }
    public string? StaffInvolvedJson { get; set; }

    public DateTime IncidentDateTime { get; set; } = DateTime.UtcNow;
    public IncidentStatus Status { get; set; } = IncidentStatus.Reported;
    public InvestigationStatus InvestigationStatus { get; set; } = InvestigationStatus.NotStarted;
    public bool RcaRequired { get; set; }

    public Guid? OwnerUserId { get; set; }
    public string? OwnerName { get; set; }

    /// <summary>Optional link after human-approved promotion.</summary>
    public Guid? FindingId { get; set; }

    /// <summary>CAPA SSOT link after promotion — points to <see cref="ComplianceDeficiency"/>, never the orphan Capas table.</summary>
    public Guid? DeficiencyId { get; set; }

    /// <summary>
    /// Legacy optional link. Do not write new Capas-table ids here.
    /// CAPA SSOT is <see cref="DeficiencyId"/> → <see cref="ComplianceDeficiency"/> CAPA fields.
    /// Retained for backward-compatible reads only; promotion never sets this from Capas.
    /// </summary>
    public Guid? CapaId { get; set; }

    public string? RelatedDocumentationJson { get; set; }
    public DateTime? ClosedAt { get; set; }
    public Guid? ClosedByUserId { get; set; }
    public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;
    public Guid? LastUpdatedByUserId { get; set; }

    public ICollection<IncidentTimelineEntry> Timeline { get; set; } = new List<IncidentTimelineEntry>();
    public ICollection<IncidentAttachment> Attachments { get; set; } = new List<IncidentAttachment>();
    public IncidentInvestigation? Investigation { get; set; }
    public IncidentRca? RootCauseAnalysis { get; set; }

    /// <summary>Closed / cancelled incidents are immutable for mutate/promote paths.</summary>
    public void EnsureMutable()
    {
        if (Status is IncidentStatus.Closed or IncidentStatus.Cancelled)
            throw new InvalidOperationException("Closed or cancelled incidents cannot be modified.");
    }
}
