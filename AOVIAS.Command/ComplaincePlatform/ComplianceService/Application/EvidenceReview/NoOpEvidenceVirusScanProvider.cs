namespace ComplianceService.Application.EvidenceReview;

/// <summary>Always-clean scan provider for Development and environments without a scanner.</summary>
public sealed class NoOpEvidenceVirusScanProvider : IEvidenceVirusScanProvider
{
    public Task<EvidenceVirusScanResult> ScanAsync(
        Stream content,
        string? fileName,
        string? contentType,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new EvidenceVirusScanResult(IsClean: true, ThreatName: null, Provider: "NoOp"));
    }
}
