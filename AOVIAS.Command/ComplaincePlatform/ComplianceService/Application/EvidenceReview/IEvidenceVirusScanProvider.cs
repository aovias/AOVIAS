namespace ComplianceService.Application.EvidenceReview;

/// <summary>
/// Extension point for malware/virus scanning of evidence artifacts.
/// Dev/default implementation is a no-op that always reports clean.
/// </summary>
public interface IEvidenceVirusScanProvider
{
    Task<EvidenceVirusScanResult> ScanAsync(
        Stream content,
        string? fileName,
        string? contentType,
        CancellationToken cancellationToken = default);
}

public sealed record EvidenceVirusScanResult(bool IsClean, string? ThreatName = null, string Provider = "NoOp");
