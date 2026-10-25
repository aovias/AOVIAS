using ComplianceService.Domain.Enums;
using Shared.Infrastructure.Domain;

namespace ComplianceService.Domain;

/// <summary>Structured RCA — not free text only.</summary>
public class IncidentRca : BaseEntity
{
    public Guid IncidentId { get; set; }
    public Incident? Incident { get; set; }

    public RcaMethod Method { get; set; } = RcaMethod.FiveWhys;
    public string? FiveWhysJson { get; set; }
    public string? FishboneJson { get; set; }
    public string? HumanFactorsJson { get; set; }
    public string? ProcessFactorsJson { get; set; }
    public string? EnvironmentalFactorsJson { get; set; }
    public string? EquipmentFactorsJson { get; set; }
    public string? OrganizationFactorsJson { get; set; }
    public string? RootCauseSummary { get; set; }

    /// <summary>Corrective action proposed from RCA — applied to deficiency CAPA SSOT on promote.</summary>
    public string? CorrectiveActionSummary { get; set; }

    /// <summary>Preventive action proposed from RCA — applied to deficiency CAPA SSOT on promote.</summary>
    public string? PreventiveActionSummary { get; set; }

    /// <summary>Optional root-cause category label (maps to <see cref="RootCauseCategory"/> when promoting).</summary>
    public string? RootCauseCategory { get; set; }

    public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;
    public Guid? CompletedByUserId { get; set; }
}
