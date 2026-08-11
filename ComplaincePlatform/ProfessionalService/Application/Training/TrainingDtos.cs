namespace ProfessionalService.Application.Training;

public class TrainingProgramDto
{
    public Guid Id { get; set; }
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
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int SessionCount { get; set; }
}

public class TrainingSessionDto
{
    public Guid Id { get; set; }
    public Guid TrainingProgramId { get; set; }
    public string ProgramTitle { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public DateTime StartsAt { get; set; }
    public DateTime? EndsAt { get; set; }
    public string? Location { get; set; }
    public string? Facilitator { get; set; }
    public int? Capacity { get; set; }
    public string Status { get; set; } = string.Empty;
    public int AssignedCount { get; set; }
    public int AttendedCount { get; set; }
    public decimal CompletionPercent { get; set; }
    public Guid? ControlledDocumentId { get; set; }
}

public class TrainingAttendanceDto
{
    public Guid Id { get; set; }
    public Guid TrainingSessionId { get; set; }
    public Guid ProfessionalProfileId { get; set; }
    public string AttendanceStatus { get; set; } = string.Empty;
    public DateTime? CheckedInAt { get; set; }
    public string? CertificateUrl { get; set; }
    public DateTime? CertificateIssuedAt { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public TrainingAssessmentDto? Assessment { get; set; }
}

public class TrainingAssessmentDto
{
    public Guid Id { get; set; }
    public Guid TrainingAttendanceId { get; set; }
    public decimal? Score { get; set; }
    public decimal? PassMark { get; set; }
    public bool? Passed { get; set; }
    public string? AssessorName { get; set; }
    public DateTime? AssessedAt { get; set; }
    public string? Notes { get; set; }
}

public class TrainingMatrixItemDto
{
    public Guid ProgramId { get; set; }
    public string ProgramCode { get; set; } = string.Empty;
    public string ProgramTitle { get; set; } = string.Empty;
    public bool IsMandatory { get; set; }
    public string? RoleCode { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? ControlledDocumentId { get; set; }
    public decimal CompletionPercent { get; set; }
    public int AssignedCount { get; set; }
    public int CompletedCount { get; set; }
}

public class CreateTrainingProgramRequest
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
    public Guid? CreatedByUserId { get; set; }
}

public class UpdateTrainingProgramRequest
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public Guid? ControlledDocumentId { get; set; }
    public string? RoleCode { get; set; }
    public Guid? DepartmentId { get; set; }
    public bool? IsMandatory { get; set; }
    public int? RenewalMonths { get; set; }
    public string? Status { get; set; }
}

public class CreateTrainingSessionRequest
{
    public Guid TrainingProgramId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime StartsAt { get; set; }
    public DateTime? EndsAt { get; set; }
    public string? Location { get; set; }
    public string? Facilitator { get; set; }
    public int? Capacity { get; set; }
    public Guid? CreatedByUserId { get; set; }
}

public class UpdateTrainingSessionRequest
{
    public string? Title { get; set; }
    public DateTime? StartsAt { get; set; }
    public DateTime? EndsAt { get; set; }
    public string? Location { get; set; }
    public string? Facilitator { get; set; }
    public int? Capacity { get; set; }
    public string? Status { get; set; }
}

public class AssignParticipantsRequest
{
    public List<Guid> ProfessionalProfileIds { get; set; } = new();
}

public class RecordAttendanceRequest
{
    public string AttendanceStatus { get; set; } = TrainingAttendanceCompletion.StatusAttended;
    public DateTime? CheckedInAt { get; set; }
}

public class RecordAssessmentRequest
{
    public decimal? Score { get; set; }
    public decimal? PassMark { get; set; }
    public bool? Passed { get; set; }
    public string? AssessorName { get; set; }
    public DateTime? AssessedAt { get; set; }
    public string? Notes { get; set; }
}

public class SetCertificateUrlRequest
{
    public string CertificateUrl { get; set; } = string.Empty;
    public DateTime? ExpiryDate { get; set; }
}
