using Shared.Infrastructure.Domain;

namespace ComplianceService.Domain
{
    public enum ActivityEventType
    {
        EvidenceUploaded = 0,
        TaskCompleted = 1,
        FindingRaised = 2,
        AuditScheduled = 3,
        CapaApproved = 4,
        PolicyUpdated = 5
    }

    /// <summary>
    /// Append-only activity feed event for the Control Center Activity Stream. Written by existing
    /// flows (task completion, evidence upload, finding raised, CAPA approval, audit scheduling).
    /// </summary>
    public class ActivityEvent : BaseEntity
    {
        public Guid TenantId { get; set; }
        public Guid OrganizationId { get; set; }
        public Guid? HospitalId { get; set; }
        public Guid? FrameworkId { get; set; }

        public ActivityEventType Type { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public Guid? ActorUserId { get; set; }
        public string? ActorName { get; set; }
        public Guid? DepartmentId { get; set; }
        public string? DepartmentName { get; set; }

        public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    }
}
