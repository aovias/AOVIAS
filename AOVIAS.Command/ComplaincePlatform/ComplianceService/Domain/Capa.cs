using Shared.Infrastructure.Domain;

namespace ComplianceService.Domain
{
    public enum CapaStatus
    {
        Open = 0,
        InProgress = 1,
        PendingApproval = 2,
        Approved = 3,
        Completed = 4,
        Cancelled = 5
    }

    /// <summary>
    /// LEGACY orphan CAPA aggregate (Phase 46 Control Center).
    /// <para><b>SSOT:</b> CAPA fields on <see cref="ComplianceDeficiency"/> via DCC APIs / <c>CapaQueryService</c>.</para>
    /// No application write path creates these rows. Control Center KPIs no longer read this table.
    /// Retained for schema compatibility until Phase B migration drops <c>Capas</c>.
    /// </summary>
    public class Capa : BaseEntity
    {
        public Guid TenantId { get; set; }
        public Guid OrganizationId { get; set; }
        public Guid HospitalId { get; set; }
        public Guid? FrameworkId { get; set; }
        public Guid? ObjectiveElementId { get; set; }
        public Guid? SourceFindingId { get; set; }

        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? RootCause { get; set; }
        public string? CorrectiveAction { get; set; }
        public string? PreventiveAction { get; set; }

        public Guid? OwnerUserId { get; set; }
        public string? OwnerName { get; set; }
        public Guid? DepartmentId { get; set; }

        public DateTime? DueDate { get; set; }
        public CapaStatus Status { get; set; } = CapaStatus.Open;

        public Guid? ApprovedBy { get; set; }
        public DateTime? ApprovedAt { get; set; }
    }
}
