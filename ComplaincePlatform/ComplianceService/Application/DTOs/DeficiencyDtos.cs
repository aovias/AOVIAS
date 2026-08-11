namespace ComplianceService.Application.DTOs;

// ── Query / Read DTOs ────────────────────────────────────────────────────────

/// <summary>Full deficiency record returned by GET /internal/v1/deficiencies/{id}.</summary>
public class DeficiencyDto
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid HospitalId { get; set; }
    public Guid FrameworkId { get; set; }
    public Guid ObjectiveElementId { get; set; }
    public Guid ComplianceObligationId { get; set; }
    public string DeficiencyCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string DeficiencyType { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string MaturityLevel { get; set; } = string.Empty;
    public string? RiskCategory { get; set; }
    public string? AssignedTo { get; set; }
    public DateTime? DueDate { get; set; }
    public string Source { get; set; } = string.Empty;
    public DateTime DetectedDate { get; set; }
    public DateTime? ClosedDate { get; set; }
    public DateTime LastEvaluatedDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Aggregate severity counts for a hospital+framework.
/// Returned by GET /internal/v1/deficiencies/summary.
/// </summary>
public class DeficiencySummaryDto
{
    public int Critical { get; set; }
    public int Major { get; set; }
    public int Moderate { get; set; }
    public int Minor { get; set; }
    public int Total { get; set; }
    public int OpenCount { get; set; }
}

/// <summary>
/// Deficiency severity breakdown scoped to a single maturity stage.
/// Returned as an array by GET /internal/v1/deficiencies/by-stage.
/// </summary>
public class DeficiencyByStageDto
{
    public string Stage { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public int Critical { get; set; }
    public int Major { get; set; }
    public int Moderate { get; set; }
    public int Minor { get; set; }
    public int Total { get; set; }
}

/// <summary>Paginated deficiency list returned by GET /internal/v1/deficiencies.</summary>
public class DeficiencyListDto
{
    public IReadOnlyList<DeficiencyDto> Items { get; set; } = Array.Empty<DeficiencyDto>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

// ── Command / Write DTOs ─────────────────────────────────────────────────────

public class PatchDeficiencyStatusRequestDto
{
    public string Status { get; set; } = string.Empty;
}

public class AssignDeficiencyRequestDto
{
    public string AssignedTo { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
}

public class AcceptDeficiencyRequestDto
{
    public string? AcceptedBy { get; set; }
}

public class RejectDeficiencyOwnershipRequestDto
{
    public string? RejectedBy { get; set; }
    public string? Reason { get; set; }
}
