namespace ComplianceService.Application.IntegrationEvents;

public class ComplianceUpdatedIntegrationEvent
{
    public Guid HospitalId { get; set; }
    public Guid FrameworkId { get; set; }
    public DateTime OccurredOn { get; set; }
    public string EventType { get; set; } = "ComplianceUpdated";
}
