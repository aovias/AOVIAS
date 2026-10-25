using System.Text.Json.Serialization;
using Shared.Infrastructure.Domain.Enums;

namespace ComplianceService.Application.DTOs;

/// <summary>
/// Obligation compliance state for a single Objective Element — embedded in <see cref="ObjectiveElementDto"/>.
/// </summary>
public class ObligationStateDto
{
    public Guid ObligationId { get; set; }
    public string ImplementationStatus { get; set; } = string.Empty;
    public string AssessmentStatus { get; set; } = string.Empty;
    public string JourneyStatus { get; set; } = string.Empty;
    public string EvidenceStatus { get; set; } = string.Empty;
    public string VerificationStatus { get; set; } = string.Empty;
    public decimal ReadinessScore { get; set; }
    public DateTime? LastAssessmentDate { get; set; }
    public DateTime? LastVerificationDate { get; set; }
    public DateTime? LastSnapshotRefreshDate { get; set; }
    public string NextActionLabel { get; set; } = string.Empty;
    public string NextActionKey { get; set; } = string.Empty;
    public bool IsNextActionUrgent { get; set; }
}

public class JourneyWorkspaceSnapshotDto
{
    public Guid JourneyId { get; set; }
    public string FrameworkCode { get; set; } = string.Empty;
    public string FrameworkName { get; set; } = string.Empty;
    /// <summary>Serialized as JSON string (e.g. <c>selfAssessment</c>).</summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public OperationalMode OperationalMode { get; set; }
    public List<ChapterWithStandardsDto> Chapters { get; set; } = new();
    public AccreditationEligibilityDto Accreditation { get; set; } = new();
    public JourneyOverviewMetricsDto Overview { get; set; } = new();
    public List<TrendPointDto> OeScoreTrend { get; set; } = new();
    public List<TrendPointDto> ChapterImprovementTrend { get; set; } = new();
    public List<TrendPointDto> RiskEvolutionTrend { get; set; } = new();
}

public class AccreditationEligibilityDto
{
    public bool Eligible { get; set; }
    public decimal OverallCompliancePercent { get; set; }
    public IReadOnlyList<string> Violations { get; set; } = Array.Empty<string>();
}

/// <summary>
/// Per-maturity-stage compliance score exposed on the Journey Dashboard.
/// Allows the frontend to render a stage progress strip without a separate API call.
/// </summary>
public class StageProgressDto
{
    /// <summary>Stage identifier — matches <c>ObjectiveElementClassification</c> values: core | commitment | achievement | excellence.</summary>
    public string Stage { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    /// <summary>0–100 compliance score derived from OE scores filtered by this stage's classification.</summary>
    public decimal Score { get; set; }
    public int TotalOeCount { get; set; }
    public int ScoredOeCount { get; set; }

    // Obligation enrichment (Phase 4)
    public int TotalObligations { get; set; }
    public int VerifiedObligations { get; set; }
    public int ImplementedObligations { get; set; }
    public int PartialObligations { get; set; }
    public int DeficientObligations { get; set; }
    public decimal ObligationReadinessScore { get; set; }
}

public class JourneyOverviewMetricsDto
{
    public decimal CoreCompliancePercent { get; set; }
    public int FailedCoreCount { get; set; }
    public int OesAtOrBelowThreeCount { get; set; }
    public int OpenActionPlansCount { get; set; }

    /// <summary>
    /// Per-stage compliance scores — Core / Commitment / Achievement / Excellence.
    /// Populated by <see cref="Services.JourneyWorkspaceAppService.GetDashboardAsync"/>.
    /// </summary>
    public List<StageProgressDto> StageBreakdown { get; set; } = new();

    // Obligation enrichment (Phase 4)
    public ObligationMaturityBreakdownDto? ObligationBreakdown { get; set; }
    public decimal ObligationReadinessScore { get; set; }
}

public class TrendPointDto
{
    public string Label { get; set; } = string.Empty;
    public decimal Value { get; set; }
}

public class OeObservationDto
{
    public Guid Id { get; set; }
    /// <summary>minor | major</summary>
    public string Severity { get; set; } = "minor";
    public string Text { get; set; } = string.Empty;
    public Guid? NonConformanceId { get; set; }
}

public class OeActionPlanDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public DateTime? Deadline { get; set; }
    public string Status { get; set; } = "open";
}

public class OeEvidenceItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string VerificationStatus { get; set; } = string.Empty;
    public DateTime? UploadedAt { get; set; }
    public DateTime? ExpiryAt { get; set; }
    public string? UploadedBy { get; set; }
}

public class OeHistoryEventDto
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime Timestamp { get; set; }
    public string? Actor { get; set; }
}

public class PatchObjectiveElementRequest
{
    /// <summary>Optional optimistic concurrency token (must match server <see cref="ObjectiveElementDto.Version"/>).</summary>
    public int? Version { get; set; }

    public bool? IsNa { get; set; }
    public string? NaJustification { get; set; }
    public int? Score { get; set; }
    public string? AuditRemarks { get; set; }
    public string? ScoreRationale { get; set; }
}

public class RegisterOeEvidenceRequest
{
    public string EvidenceType { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? Metadata { get; set; }
}
