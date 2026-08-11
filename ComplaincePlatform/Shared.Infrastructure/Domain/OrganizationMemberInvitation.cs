using System.ComponentModel.DataAnnotations;

namespace Shared.Infrastructure.Domain;

/// <summary>
/// Email-based invite to join an organization/hospital (CRP Phase 7 — token redeem pipeline).
/// </summary>
public sealed class OrganizationMemberInvitation : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public Guid HospitalId { get; set; }

    public Guid? DepartmentId { get; set; }

    [MaxLength(128)]
    public string Role { get; set; } = string.Empty;

    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    /// <summary>Upper-invariant email for duplicate detection (matches pending invites only via filtered index).</summary>
    [MaxLength(256)]
    public string NormalizedEmail { get; set; } = string.Empty;

    /// <summary>SHA-256 hex of redeem token; raw token is only emailed.</summary>
    [MaxLength(256)]
    public string? TokenHash { get; set; }

    public DateTime ExpiresAt { get; set; }

    public InvitationStatus Status { get; set; }

    public DateTime? AcceptedAt { get; set; }

    public Guid? AcceptedUserId { get; set; }

    [MaxLength(200)]
    public string? Designation { get; set; }

    /// <summary>Permanent | Contract | Consultant | Temporary | Intern | Volunteer</summary>
    [MaxLength(40)]
    public string EmploymentType { get; set; } = "Permanent";

    public Guid? ReportsToUserId { get; set; }
    public Guid? ManagerEmploymentId { get; set; }
    public bool RequireSeat { get; set; } = true;
    public Guid? PositionId { get; set; }
    public Guid? InvitedByUserId { get; set; }
    public DateTime? CancelledAt { get; set; }

    /// <summary>Last completed pipeline step for resume (IdentityEnsure, PersonEnsure, …).</summary>
    [MaxLength(64)]
    public string? PipelineStep { get; set; }

    [MaxLength(1024)]
    public string? PipelineError { get; set; }

    public Guid? MembershipId { get; set; }
    public Guid? EmploymentId { get; set; }
    public Guid? PersonId { get; set; }
    public bool SeatAssigned { get; set; }
}
