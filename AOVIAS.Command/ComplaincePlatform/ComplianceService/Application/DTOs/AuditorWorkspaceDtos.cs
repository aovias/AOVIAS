using ComplianceService.Domain.Enums;
using Shared.Infrastructure.Domain.Enums;

namespace ComplianceService.Application.DTOs;

public record AuditorDashboardDto
{
    public int ActiveSessions { get; init; }
    public int AreasReviewed { get; init; }
    public int PendingReviews { get; init; }
    public int FindingsRaised { get; init; }
    public int Observations { get; init; }
    public int CriticalRisks { get; init; }
    public double CoveragePercent { get; init; }
    public int EvidenceReviewed { get; init; }
    public decimal ReadinessSnapshot { get; init; }
    public IReadOnlyList<AuditorSessionSummaryDto> RecentSessions { get; init; } = Array.Empty<AuditorSessionSummaryDto>();
}

public record AuditorSessionSummaryDto
{
    public Guid Id { get; init; }
    public string SessionNumber { get; init; } = string.Empty;
    public AuditType AuditType { get; init; }
    public AuditSessionStatus Status { get; init; }
    public AuditSessionResult OverallResult { get; init; }
    public string Scope { get; init; } = string.Empty;
    public string? LeadAuditorName { get; init; }
    public DateTime? PlannedStartDate { get; init; }
    public DateTime? PlannedEndDate { get; init; }
    public int FindingsCount { get; init; }
    public int ObservationsCount { get; init; }
    public DateTime CreatedAt { get; init; }
    /// <summary>
    /// Scheduled audit this session was started from, when it came from planning.
    /// Read-only counterpart of <see cref="CreateAuditSessionRequest.ScheduledAuditId"/> —
    /// lets planning resolve an already-started audit instead of creating a duplicate session.
    /// </summary>
    public Guid? ScheduledAuditId { get; init; }
}

public record AuditSessionDetailDto : AuditorSessionSummaryDto
{
    public Guid? HospitalId { get; init; }
    public Guid? FrameworkId { get; init; }
    public Guid? LeadAuditorUserId { get; init; }
    public IReadOnlyList<AuditorTeamMemberDto> AuditorTeam { get; init; } = Array.Empty<AuditorTeamMemberDto>();
    public DateTime? ActualStartDate { get; init; }
    public DateTime? ActualEndDate { get; init; }
    public string? Summary { get; init; }
    public IReadOnlyList<AuditObservationListItemDto> Observations { get; init; } = Array.Empty<AuditObservationListItemDto>();
    public IReadOnlyList<AuditCommentDto> Comments { get; init; } = Array.Empty<AuditCommentDto>();
    public IReadOnlyList<AuditorFindingSummaryDto> Findings { get; init; } = Array.Empty<AuditorFindingSummaryDto>();
}

public record AuditorTeamMemberDto
{
    public Guid? UserId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Role { get; init; }
}

public record CreateAuditSessionRequest
{
    public AuditType AuditType { get; init; } = AuditType.InternalAudit;
    public Guid? FrameworkId { get; init; }
    public string Scope { get; init; } = string.Empty;
    public string? LeadAuditorName { get; init; }
    public IReadOnlyList<AuditorTeamMemberDto>? AuditorTeam { get; init; }
    public DateTime? PlannedStartDate { get; init; }
    public DateTime? PlannedEndDate { get; init; }
    public string? Summary { get; init; }
    public Guid? ScheduledAuditId { get; init; }
}

public record AuditObservationListItemDto
{
    public Guid Id { get; init; }
    public string ObservationNumber { get; init; } = string.Empty;
    public ObservationType Type { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public Guid? ObjectiveElementId { get; init; }
    public Guid? ChapterId { get; init; }
    public string? ChapterCode { get; init; }
    public string? OeCode { get; init; }
    public FindingDepartment? Department { get; init; }
    public string? CreatedByName { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record CreateAuditObservationRequest
{
    public Guid AuditSessionId { get; init; }
    public ObservationType Type { get; init; } = ObservationType.GeneralComment;
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public Guid? ObjectiveElementId { get; init; }
    public Guid? ChapterId { get; init; }
    /// <summary>Optional checklist chapter code when Guid is unknown.</summary>
    public string? ChapterCode { get; init; }
    /// <summary>Optional checklist OE code when Guid is unknown.</summary>
    public string? OeCode { get; init; }
    public FindingDepartment? Department { get; init; }
}

public record AuditCommentDto
{
    public Guid Id { get; init; }
    public AuditCommentType Type { get; init; }
    public string Content { get; init; } = string.Empty;
    public Guid AuthorUserId { get; init; }
    public string? AuthorName { get; init; }
    public bool IsAuditorAuthor { get; init; }
    public Guid? ParentCommentId { get; init; }
    public DateTime CreatedAt { get; init; }
    public IReadOnlyList<AuditCommentDto> Replies { get; init; } = Array.Empty<AuditCommentDto>();
}

public record CreateAuditCommentRequest
{
    public Guid AuditSessionId { get; init; }
    public AuditCommentType Type { get; init; } = AuditCommentType.GeneralComment;
    public string Content { get; init; } = string.Empty;
    public Guid? ParentCommentId { get; init; }
}

public record AuditorFindingSummaryDto
{
    public Guid Id { get; init; }
    public string FindingNumber { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public FindingSeverity Severity { get; init; }
    public FindingStatus Status { get; init; }
    public Guid? ObjectiveElementId { get; init; }
    public Guid? PromotedDeficiencyId { get; init; }
    public DateTime DateRaised { get; init; }
}

public record EvidenceTraceabilityDto
{
    public Guid ObjectiveElementId { get; init; }
    public string ObjectiveElementCode { get; init; } = string.Empty;
    public string ObjectiveElementDescription { get; init; } = string.Empty;
    public IReadOnlyList<TraceabilityTimelineEntryDto> Timeline { get; init; } = Array.Empty<TraceabilityTimelineEntryDto>();
    public decimal? CurrentReadinessScore { get; init; }
}

public record TraceabilityTimelineEntryDto
{
    public string Layer { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime? Timestamp { get; init; }
    public Guid? EntityId { get; init; }
    public string? Detail { get; init; }
}

public record AuditorSessionReportDto
{
    public Guid SessionId { get; init; }
    public string SessionNumber { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public DateTime GeneratedAt { get; init; }
    public string Summary { get; init; } = string.Empty;
    public IReadOnlyList<string> Strengths { get; init; } = Array.Empty<string>();
    public IReadOnlyList<AuditObservationListItemDto> Observations { get; init; } = Array.Empty<AuditObservationListItemDto>();
    public IReadOnlyList<AuditorFindingSummaryDto> Findings { get; init; } = Array.Empty<AuditorFindingSummaryDto>();
    public string RiskSummary { get; init; } = string.Empty;
    public IReadOnlyList<string> Recommendations { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> EvidenceGaps { get; init; } = Array.Empty<string>();
    public decimal ReadinessSummary { get; init; }
    public string MarkdownContent { get; init; } = string.Empty;
}

public record AuditorAssessmentContextDto
{
    public Guid? FrameworkId { get; init; }
    public string? FrameworkName { get; init; }
    public IReadOnlyList<AuditorChapterContextDto> Chapters { get; init; } = Array.Empty<AuditorChapterContextDto>();
}

public record AuditorChapterContextDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public IReadOnlyList<AuditorStandardContextDto> Standards { get; init; } = Array.Empty<AuditorStandardContextDto>();
}

public record AuditorStandardContextDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public IReadOnlyList<AuditorObjectiveElementContextDto> ObjectiveElements { get; init; } = Array.Empty<AuditorObjectiveElementContextDto>();
}

public record AuditorObjectiveElementContextDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public int EvidenceCount { get; init; }
    public int VerifiedEvidenceCount { get; init; }
    public int DeficiencyCount { get; init; }
    public int OpenCapaCount { get; init; }
    public int TaskCount { get; init; }
    public decimal? ReadinessScore { get; init; }
}
