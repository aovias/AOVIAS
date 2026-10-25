using Shared.Infrastructure.Domain;

namespace ProfessionalService.Domain.Entities;

/// <summary>
/// Lite training program (Phase 4 Sprint 2). Optional link to controlled document (WI/SOP).
/// </summary>
public class TrainingProgram : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public Guid? HospitalId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? ControlledDocumentId { get; set; }
    public string? RoleCode { get; set; }
    public Guid? DepartmentId { get; set; }
    public bool IsMandatory { get; set; }
    public int? RenewalMonths { get; set; }
    public string Status { get; set; } = "Active";
    public bool IsDeleted { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public ICollection<TrainingSession> Sessions { get; set; } = new List<TrainingSession>();
}

public class TrainingSession : BaseEntity
{
    public Guid TrainingProgramId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime StartsAt { get; set; }
    public DateTime? EndsAt { get; set; }
    public string? Location { get; set; }
    public string? Facilitator { get; set; }
    public int? Capacity { get; set; }
    public string Status { get; set; } = "Scheduled";
    public bool IsDeleted { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public TrainingProgram? Program { get; set; }
    public ICollection<TrainingAttendance> Attendances { get; set; } = new List<TrainingAttendance>();
}

public class TrainingAttendance : BaseEntity
{
    public Guid TrainingSessionId { get; set; }
    public Guid ProfessionalProfileId { get; set; }
    /// <summary>Assigned | Attended | Absent | Excused</summary>
    public string AttendanceStatus { get; set; } = "Assigned";
    public DateTime? CheckedInAt { get; set; }
    public string? CertificateUrl { get; set; }
    public DateTime? CertificateIssuedAt { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public bool IsDeleted { get; set; }
    public TrainingSession? Session { get; set; }
    public TrainingAssessment? Assessment { get; set; }
}

/// <summary>Lite assessment tied to a session attendance row.</summary>
public class TrainingAssessment : BaseEntity
{
    public Guid TrainingAttendanceId { get; set; }
    public decimal? Score { get; set; }
    public decimal? PassMark { get; set; }
    public bool? Passed { get; set; }
    public string? AssessorName { get; set; }
    public DateTime? AssessedAt { get; set; }
    public string? Notes { get; set; }
    public TrainingAttendance? Attendance { get; set; }
}
