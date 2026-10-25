namespace ComplianceService.Application.DTOs;

public sealed class EvidenceDetailDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string EvidenceType { get; set; } = string.Empty;
    /// <summary>Opaque URI (e.g. local://...); do not treat as public URL.</summary>
    public string FileUrl { get; set; } = string.Empty;
    public string UploadedBy { get; set; } = string.Empty;
    public DateTime UploadedOn { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string ArtifactType { get; set; } = string.Empty;
    public string LifecycleStatus { get; set; } = string.Empty;
    public string Metadata { get; set; } = string.Empty;
    public EvidenceVerificationDetailDto Verification { get; set; } = new();
    public IReadOnlyList<EvidenceVersionItemDto> Versions { get; set; } = Array.Empty<EvidenceVersionItemDto>();
}

public sealed class EvidenceVerificationDetailDto
{
    public string Status { get; set; } = string.Empty;
    public bool? IsApproved { get; set; }
    public string? VerifiedBy { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public string? ReviewComment { get; set; }
    /// <summary>Legacy alias retained for backward compatibility.</summary>
    public string? Remarks { get; set; }
    /// <summary>Base64-encoded row version for optimistic concurrency.</summary>
    public string? RowVersion { get; set; }
}

public sealed class EvidenceVersionItemDto
{
    public int VersionNumber { get; set; }
    public string FileUrl { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? Notes { get; set; }
}

public sealed class VerifyOeEvidenceRequest
{
    public bool Accepted { get; set; }
    public string? Remarks { get; set; }
}

public enum EvidenceApprovalAction
{
    Approve = 1,
    Reject = 2,
    RequestMoreEvidence = 3
}

public sealed class EvidenceApprovalRequest
{
    public string Action { get; set; } = string.Empty;
    public string? Remark { get; set; }
    public string? Recommendation { get; set; }
    public string? ReviewerRole { get; set; }
    /// <summary>Optional base64-encoded row version provided by client for optimistic concurrency.</summary>
    public string? RowVersion { get; set; }
}

public sealed class EvidenceApprovalResultDto
{
    public Guid EvidenceId { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool? IsApproved { get; set; }
    public string? ReviewComment { get; set; }
    public string? VerifiedBy { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public string? RowVersion { get; set; }
}

public sealed class BulkEvidenceUploadItemResult
{
    public string FileName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public Guid? EvidenceId { get; set; }
    public string? ErrorMessage { get; set; }
}

public sealed class BulkEvidenceUploadResponseDto
{
    public int TotalFiles { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public IReadOnlyList<BulkEvidenceUploadItemResult> Items { get; set; } = Array.Empty<BulkEvidenceUploadItemResult>();
}

/// <summary>Single file payload for bulk upload (controller maps from <c>IFormFile</c>).</summary>
public sealed class BulkUploadIncomingFile
{
    public Stream Stream { get; init; } = Stream.Null;
    public string FileName { get; init; } = string.Empty;
    public string? ContentType { get; init; }
    public long Length { get; init; }
}

public sealed class EvidenceOptions
{
    public const string SectionName = "Evidence";

    /// <summary>Maximum number of files per bulk upload request.</summary>
    public int BulkUploadMaxFiles { get; set; } = 15;

    /// <summary>Maximum size per file in bytes (should match Gateway).</summary>
    public long MaxFileBytes { get; set; } = 10_485_760;
}
