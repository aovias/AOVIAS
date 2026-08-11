using ComplianceService.Domain.Enums;

namespace ComplianceService.Application.DTOs;

/// <summary>Full obligation record returned by list and detail endpoints.</summary>
public class OrganizationObligationDto
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid HospitalId { get; set; }
    public Guid FrameworkId { get; set; }
    public Guid ChapterId { get; set; }
    public Guid StandardId { get; set; }
    public Guid ObjectiveElementId { get; set; }
    public Guid? ComplianceNodeId { get; set; }

    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string MaturityLevel { get; set; } = string.Empty;
    public string? RiskCategory { get; set; }
    public decimal Weight { get; set; }

    public string ImplementationStatus { get; set; } = string.Empty;
    public string AssessmentStatus { get; set; } = string.Empty;
    public string JourneyStatus { get; set; } = string.Empty;
    public string EvidenceStatus { get; set; } = string.Empty;
    public string VerificationStatus { get; set; } = string.Empty;

    public decimal ReadinessScore { get; set; }
    public decimal CompletionPercentage { get; set; }

    public DateTime? LastAssessmentDate { get; set; }
    public DateTime? LastEvidenceDate { get; set; }
    public DateTime? LastVerificationDate { get; set; }
    public DateTime? LastSnapshotRefreshDate { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
}

/// <summary>Summary with total counts and per-maturity breakdown.</summary>
public class OrganizationObligationSummaryDto
{
    public Guid OrganizationId { get; set; }
    public Guid FrameworkId { get; set; }

    public int TotalObligations { get; set; }
    public int Implemented { get; set; }
    public int PartiallyImplemented { get; set; }
    public int NotImplemented { get; set; }
    public int NotStarted { get; set; }
    public int Verified { get; set; }
    public int Passed { get; set; }
    public int Failed { get; set; }

    public decimal OverallReadinessScore { get; set; }

    public ObligationMaturityBreakdownDto MaturityBreakdown { get; set; } = new();
}

/// <summary>Per-band (Core / Commitment / Achievement / Excellence) obligation counts and scores.</summary>
public class ObligationMaturityBreakdownDto
{
    public ObligationBandSummaryDto Core { get; set; } = new();
    public ObligationBandSummaryDto Commitment { get; set; } = new();
    public ObligationBandSummaryDto Achievement { get; set; } = new();
    public ObligationBandSummaryDto Excellence { get; set; } = new();
}

/// <summary>Counts for a single maturity band.</summary>
public class ObligationBandSummaryDto
{
    public int Total { get; set; }
    public int Verified { get; set; }
    public int Implemented { get; set; }
    public int Partial { get; set; }
    public int Deficient { get; set; }
    /// <summary>Obligations explicitly marked NotStarted (excludes Unknown).</summary>
    public int NotStarted { get; set; }
    /// <summary>Obligations not yet assessed or in default state.</summary>
    public int Unknown { get; set; }
    public decimal ReadinessScore { get; set; }
}

/// <summary>Per-chapter readiness score for a given organization × framework.</summary>
public class ObligationReadinessProjectionDto
{
    public Guid ChapterId { get; set; }
    public string ChapterCode { get; set; } = string.Empty;
    public string ChapterTitle { get; set; } = string.Empty;
    public int TotalObligations { get; set; }
    public int CompletedObligations { get; set; }
    public decimal ReadinessScore { get; set; }
}

/// <summary>Lightweight status DTO returned by the single-obligation status endpoint.</summary>
public class ObligationStatusDto
{
    public Guid Id { get; set; }
    public Guid ObjectiveElementId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string ImplementationStatus { get; set; } = string.Empty;
    public string AssessmentStatus { get; set; } = string.Empty;
    public string JourneyStatus { get; set; } = string.Empty;
    public string EvidenceStatus { get; set; } = string.Empty;
    public string VerificationStatus { get; set; } = string.Empty;
    public decimal ReadinessScore { get; set; }
    public decimal CompletionPercentage { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>Request body for PATCH /obligations/{id}/status.</summary>
public class PatchObligationStatusRequest
{
    public string? ImplementationStatus { get; set; }
    public string? VerificationStatus { get; set; }
    public string? EvidenceStatus { get; set; }
    public string? UpdatedBy { get; set; }
}
