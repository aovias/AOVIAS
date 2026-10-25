using ComplianceService.Domain.Enums;
using Shared.Infrastructure.Domain;

namespace ComplianceService.Domain;

public class IncidentInvestigation : BaseEntity
{
    public Guid IncidentId { get; set; }
    public Incident? Incident { get; set; }

    public string? IncidentSummary { get; set; }
    public string? ContributingFactorsJson { get; set; }
    public string? PeopleFactorsJson { get; set; }
    public string? EnvironmentFactorsJson { get; set; }
    public string? EquipmentFactorsJson { get; set; }
    public string? ProcessFactorsJson { get; set; }
    public string? CommunicationFactorsJson { get; set; }
    public string? TimelineJson { get; set; }
    public string? Recommendations { get; set; }
    public string? ReviewersJson { get; set; }
    public InvestigationStatus Status { get; set; } = InvestigationStatus.InProgress;
    public Guid? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;
}
