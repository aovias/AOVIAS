using ComplianceService.Domain.Enums;
using Shared.Infrastructure.Domain;

namespace ComplianceService.Domain
{
    public enum ScheduledAuditStatus
    {
        Planned = 0,
        InProgress = 1,
        Completed = 2,
        Passed = 3,
        Failed = 4
    }

    /// <summary>
    /// A scheduled / upcoming audit. Backfilled from <see cref="ComplianceJourney.AuditDate"/> and
    /// synced on AuditScheduled lifecycle transitions; powers "Upcoming Audits" and audit readiness.
    /// Thin program metadata via ProgramYear / ProgramMonth (no separate AuditProgram aggregate).
    /// </summary>
    public class ScheduledAudit : BaseEntity
    {
        public Guid TenantId { get; set; }
        public Guid OrganizationId { get; set; }
        public Guid HospitalId { get; set; }
        public Guid FrameworkId { get; set; }
        public Guid? ComplianceJourneyId { get; set; }

        public string Title { get; set; } = string.Empty;
        public DateTime ScheduledDate { get; set; }
        public ScheduledAuditStatus Status { get; set; } = ScheduledAuditStatus.Planned;
        public string? AuditorName { get; set; }
        public string? Result { get; set; }

        /// <summary>Annual program year (e.g. 2026). Defaults from ScheduledDate when unset.</summary>
        public int? ProgramYear { get; set; }

        /// <summary>Calendar month 1–12 within the program year.</summary>
        public int? ProgramMonth { get; set; }

        public AuditType? AuditType { get; set; }

        /// <summary>Optional program label (e.g. "FY2026 Internal Audit Plan").</summary>
        public string? ProgramName { get; set; }
    }
}
