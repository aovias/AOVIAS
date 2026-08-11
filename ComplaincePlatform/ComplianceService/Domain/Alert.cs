using Shared.Infrastructure.Domain;

namespace ComplianceService.Domain
{
    public enum AlertSeverity
    {
        Critical = 0,
        High = 1,
        Medium = 2,
        Low = 3
    }

    public enum AlertType
    {
        ComplianceDrop = 0,
        CriticalTasks = 1,
        MissingEvidence = 2,
        AuditRisk = 3,
        OverdueCapa = 4,
        PolicyGap = 5,
        DocumentExpiry = 6,
        ComplianceImprovement = 7
    }

    public enum AlertStatus
    {
        Active = 0,
        Acknowledged = 1,
        Dismissed = 2
    }

    /// <summary>
    /// Smart alert / intelligence signal surfaced on the Control Center. Generated (idempotently by
    /// <see cref="DedupeKey"/>) from live findings, tasks, evidence and score deltas.
    /// </summary>
    public class Alert : BaseEntity
    {
        public Guid TenantId { get; set; }
        public Guid OrganizationId { get; set; }
        public Guid? HospitalId { get; set; }
        public Guid? FrameworkId { get; set; }

        public AlertSeverity Severity { get; set; } = AlertSeverity.Medium;
        public AlertType Type { get; set; }

        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        /// <summary>Optional metric label rendered next to the alert (e.g. "-12%", "1 clause").</summary>
        public string? Metric { get; set; }

        public string? EntityType { get; set; }
        public Guid? EntityId { get; set; }

        /// <summary>Stable key used to upsert/refresh a live alert instead of duplicating it.</summary>
        public string? DedupeKey { get; set; }

        public AlertStatus Status { get; set; } = AlertStatus.Active;
        public DateTime? AcknowledgedAt { get; set; }
        public Guid? AcknowledgedBy { get; set; }
    }
}
