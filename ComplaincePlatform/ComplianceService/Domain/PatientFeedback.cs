using ComplianceService.Domain.Enums;
using Shared.Infrastructure.Domain;

namespace ComplianceService.Domain;

/// <summary>Independent patient feedback entity — may link to incidents when appropriate.</summary>
public class PatientFeedback : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid? HospitalId { get; set; }

    public string FeedbackNumber { get; set; } = string.Empty;
    public PatientFeedbackType FeedbackType { get; set; } = PatientFeedbackType.Feedback;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? PatientName { get; set; }
    public string? ContactInfo { get; set; }
    public string? Department { get; set; }
    public Guid? LinkedIncidentId { get; set; }
    public Incident? LinkedIncident { get; set; }
    public string Status { get; set; } = "Open";
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
    public Guid? AssignedToUserId { get; set; }
}
