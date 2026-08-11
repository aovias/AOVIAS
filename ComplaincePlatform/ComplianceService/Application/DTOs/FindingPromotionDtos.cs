namespace ComplianceService.Application.DTOs;

/// <summary>Sprint 7B Phase 1 — eligibility evaluation (Eligible / NotEligible).</summary>
public class FindingPromotionEligibilityDto
{
    public Guid FindingId { get; set; }
    /// <summary>Eligible or NotEligible</summary>
    public string EligibilityStatus { get; set; } = string.Empty;
    public bool IsEligible { get; set; }
    public IReadOnlyList<string> ValidationReasons { get; set; } = Array.Empty<string>();
}

public class FindingPromotionStatusDto
{
    public Guid FindingId { get; set; }
    public string EligibilityStatus { get; set; } = string.Empty;
    public bool IsEligible { get; set; }
    public bool IsReady { get; set; }
    public bool IsPromoted { get; set; }
    public bool AllowPromotion { get; set; }
    public Guid? PromotedDeficiencyId { get; set; }
    public Guid? PrimaryRemediationTaskId { get; set; }
    public string? FindingNumber { get; set; }
    public IReadOnlyList<string> ValidationMessages { get; set; } = Array.Empty<string>();
}

public class PromoteFindingResultDto
{
    public Guid FindingId { get; set; }
    public Guid DeficiencyId { get; set; }
    public string DeficiencyCode { get; set; } = string.Empty;
    public Guid? PrimaryRemediationTaskId { get; set; }
    public DateTime PromotedAt { get; set; }
}
