using System.Text.Json.Serialization;

namespace ComplianceService.Application.DTOs;

/// <summary>GET /journeys/{id}/session — spec journey-screen-api v2.0 §4.1</summary>
public class JourneySessionDto
{
    public Guid JourneyId { get; set; }

    [JsonPropertyName("frameworkCode")]
    public string FrameworkCode { get; set; } = string.Empty;

    [JsonPropertyName("frameworkName")]
    public string FrameworkName { get; set; } = string.Empty;

    /// <summary>0–4 per spec (integer JSON).</summary>
    public int OperationalMode { get; set; }

    public bool ReadOnly { get; set; }

    public bool SnapshotReadOnly { get; set; }

    public DateTime LastSyncedAt { get; set; }

    public JourneyCatalogMetaDto Catalog { get; set; } = new();

    public JourneyModeProgressionDto? ModeProgression { get; set; }
}

public class JourneyCatalogMetaDto
{
    public string? StandardEdition { get; set; }
    public string? EffectiveFrom { get; set; }
    public int? TotalStandardsExpected { get; set; }
    public int? TotalOesExpected { get; set; }
    public bool IsPartialCatalog { get; set; }
}

public class JourneyModeProgressionDto
{
    public List<int> AllowedNextModes { get; set; } = new();

    /// <summary>Keys are target mode integers 0–4 as strings for JSON object keys.</summary>
    public Dictionary<string, string?> LockReasonByTargetMode { get; set; } = new();
}

/// <summary>GET /journeys/{id}/dashboard — §4.2</summary>
public class JourneyDashboardDto
{
    public JourneyOverviewMetricsDto Overview { get; set; } = new();

    public JourneyDashboardAccreditationDto Accreditation { get; set; } = new();

    public decimal? AccreditationReadinessPercent { get; set; }

    public List<TrendPointDto> OeScoreTrend { get; set; } = new();

    public List<TrendPointDto> ChapterImprovementTrend { get; set; } = new();

    public List<TrendPointDto> RiskEvolutionTrend { get; set; } = new();

    /// <summary>Authoritative maturity stage from <see cref="Services.StageUnlockingService"/>.</summary>
    public MaturityStage CurrentStage { get; set; }

    /// <summary>Per-stage unlock state — same source as Compliance Maturity dashboard.</summary>
    public IReadOnlyList<MaturityStageDto> MaturityStages { get; set; } = Array.Empty<MaturityStageDto>();
}

public class JourneyDashboardAccreditationDto
{
    public decimal OverallCompliancePercent { get; set; }

    public decimal CoreOeAverageScore { get; set; }

    public decimal AverageStandardScore { get; set; }

    public decimal AverageChapterScore { get; set; }

    public IReadOnlyList<string> Violations { get; set; } = Array.Empty<string>();
}

/// <summary>GET /journeys/{id}/chapters — §4.3 wrapper</summary>
public class JourneyChaptersIndexDto
{
    public List<JourneyChapterIndexItemDto> Items { get; set; } = new();
}

/// <summary>Chapter row for execution left rail (Phase 48 fields).</summary>
public class JourneyChapterIndexItemDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal? ComplianceScorePercent { get; set; }
    public string? Risk { get; set; }
    public string? CoreComplianceStatus { get; set; }
    public int? OpenIssuesCount { get; set; }
    public bool? PendingAudit { get; set; }
    public bool? CoreFailureFlag { get; set; }
    public string? IssueBadgeLabel { get; set; }
    public string? PassFailLabel { get; set; }
    public bool? ChangedSinceLastAudit { get; set; }
    public string? MaturityLabel { get; set; }
    public bool? HighlightHighRisk { get; set; }
}

public class PatchObjectiveElementResponseDto
{
    public ObjectiveElementDto ObjectiveElement { get; set; } = new();

    public JourneyOverviewMetricsDto? Overview { get; set; }

    /// <summary>Optimistic concurrency revision (score history count for OE+journey).</summary>
    public int Version { get; set; }
}

public class PatchOperationalModeRequestDto
{
    public int OperationalMode { get; set; }
}
