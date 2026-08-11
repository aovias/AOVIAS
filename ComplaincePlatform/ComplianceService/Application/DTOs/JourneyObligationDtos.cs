namespace ComplianceService.Application.DTOs;

/// <summary>
/// Obligation overview for a journey — wraps summary + journey-level readiness.
/// Returned by GET /internal/v1/journey-obligations/overview
/// </summary>
public class JourneyObligationOverviewDto
{
    public Guid HospitalId { get; set; }
    public Guid FrameworkId { get; set; }
    public OrganizationObligationSummaryDto Summary { get; set; } = new();
    public decimal JourneyReadinessScore { get; set; }
    public int TotalObligations { get; set; }
    public int ObligationsWithNextAction { get; set; }
    public int UrgentObligationsCount { get; set; }
}

/// <summary>
/// Paginated obligation list for a journey.
/// Returned by GET /internal/v1/journey-obligations
/// </summary>
public class JourneyObligationsListDto
{
    public IReadOnlyList<OrganizationObligationDto> Items { get; set; } = Array.Empty<OrganizationObligationDto>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

/// <summary>
/// Per-level readiness breakdown: obligation → OE → stage → journey.
/// Returned by GET /internal/v1/journey-obligations/readiness
/// </summary>
public class JourneyReadinessDto
{
    public Guid HospitalId { get; set; }
    public Guid FrameworkId { get; set; }
    public decimal JourneyReadinessScore { get; set; }
    public List<StageReadinessDto> ByStage { get; set; } = new();
    public List<ObligationReadinessProjectionDto> ByChapter { get; set; } = new();
}

public class StageReadinessDto
{
    public string Stage { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public decimal ReadinessScore { get; set; }
    public int TotalObligations { get; set; }
}
