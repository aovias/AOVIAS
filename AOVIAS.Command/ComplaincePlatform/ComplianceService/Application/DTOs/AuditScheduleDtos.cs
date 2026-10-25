using ComplianceService.Domain;
using ComplianceService.Domain.Enums;

namespace ComplianceService.Application.DTOs;

public record ScheduledAuditDto
{
    public Guid Id { get; init; }
    public Guid OrganizationId { get; init; }
    public Guid HospitalId { get; init; }
    public Guid FrameworkId { get; init; }
    public Guid? ComplianceJourneyId { get; init; }
    public string Title { get; init; } = string.Empty;
    public DateTime ScheduledDate { get; init; }
    public ScheduledAuditStatus Status { get; init; }
    public string? AuditorName { get; init; }
    public string? Result { get; init; }
    public int? ProgramYear { get; init; }
    public int? ProgramMonth { get; init; }
    public AuditType? AuditType { get; init; }
    public string? AuditTypeLabel { get; init; }
    public string? ProgramName { get; init; }
}

public record CreateScheduledAuditRequest
{
    public Guid? HospitalId { get; init; }
    public Guid FrameworkId { get; init; }
    public string Title { get; init; } = string.Empty;
    public DateTime ScheduledDate { get; init; }
    public string? AuditorName { get; init; }
    public int? ProgramYear { get; init; }
    public int? ProgramMonth { get; init; }
    public AuditType? AuditType { get; init; }
    public string? ProgramName { get; init; }
    public Guid? ComplianceJourneyId { get; init; }
}

public record AuditCalendarQuery
{
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
    public int? Year { get; init; }
    public int? Month { get; init; }
    public Guid? HospitalId { get; init; }
    public Guid? FrameworkId { get; init; }
}

public record GenerateAuditFacadeRequest
{
    public Guid OrganizationId { get; init; }
    public Guid ComplianceProgramId { get; init; }
    public Guid? HospitalId { get; init; }
    public Guid? FrameworkId { get; init; }
    public AuditType AuditType { get; init; } = AuditType.InternalAudit;
}

public record GenerateAuditFacadeResponse
{
    public Guid Id { get; init; }
    public string SessionNumber { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public Guid OrganizationId { get; init; }
    public Guid? ComplianceProgramId { get; init; }
    public DateTime StartedAt { get; init; }
}

public record CloseAuditSessionRequest
{
    public AuditSessionResult? OverallResult { get; init; }
    public string? Summary { get; init; }
}

public record AuditSimulationRequestDto
{
    public Guid? HospitalId { get; init; }
    public Guid? FrameworkId { get; init; }
}

public record AuditSimulationResultDto
{
    public int MajorNCCount { get; init; }
    public int MinorNCCount { get; init; }
    public int ObservationCount { get; init; }
    public int OFICount { get; init; }
    public int TotalNonConformances { get; init; }
    public int Score { get; init; }
    public int TotalClauses { get; init; }
    public int CompliantClauses { get; init; }
    public int OpenTasks { get; init; }
    public IReadOnlyList<AuditSimulationHighRiskChapterDto> HighRiskChapters { get; init; } = Array.Empty<AuditSimulationHighRiskChapterDto>();
}

public record AuditSimulationHighRiskChapterDto
{
    public Guid ChapterId { get; init; }
    public string ChapterCode { get; init; } = string.Empty;
    public string ChapterName { get; init; } = string.Empty;
    public decimal CompletionPercentage { get; init; }
    public int TotalObjectiveElements { get; init; }
    public int CompletedObjectiveElements { get; init; }
}
