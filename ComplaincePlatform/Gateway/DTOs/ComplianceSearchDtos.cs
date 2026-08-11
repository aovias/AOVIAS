namespace Gateway.DTOs;

/// <summary>
/// Unified compliance search hit returned by <c>GET /api/v1/compliance/search</c>.
/// Facade over ComplianceService graph search (and optional evidence/task enrichment).
/// </summary>
public sealed class ComplianceSearchHitDto
{
    public string Type { get; set; } = string.Empty;
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public Guid? HospitalId { get; set; }
    public string? Department { get; set; }
    public string? Owner { get; set; }
    public string? Href { get; set; }
    public string? Subtitle { get; set; }
}

public sealed class ComplianceSearchResponseDto
{
    public string Query { get; set; } = string.Empty;
    public int TotalResults { get; set; }
    public string Source { get; set; } = "graph";
    public IReadOnlyList<ComplianceSearchHitDto> Results { get; set; } = Array.Empty<ComplianceSearchHitDto>();
}
