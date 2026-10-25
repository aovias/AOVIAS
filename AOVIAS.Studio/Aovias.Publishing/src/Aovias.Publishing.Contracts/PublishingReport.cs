namespace Aovias.Publishing.Contracts;

/// <summary>
/// Per-stage performance timing captured during publishing (D15).
/// </summary>
public sealed record PublishingStageTiming(string StageName, TimeSpan Duration, bool Succeeded)
{
    public double DurationMs => Math.Round(Duration.TotalMilliseconds, 1);
}

/// <summary>
/// Comprehensive publishing report (D11) emitted on every publish and embedded
/// in the produced package.
/// </summary>
public sealed class PublishingReport
{
    public required string Service { get; init; }

    public required string Version { get; init; }

    public required string Profile { get; init; }

    public required DateTimeOffset GeneratedUtc { get; init; }

    public required string PublishingEngineVersion { get; init; }

    public bool DeploymentReady { get; init; }

    public TimeSpan TotalDuration { get; init; }

    public double TotalDurationMs => Math.Round(TotalDuration.TotalMilliseconds, 1);

    public IReadOnlyList<PublishingStageTiming> StageTimings { get; init; } = [];

    public int ChecksumCount { get; init; }

    public bool PackageVerified { get; init; }

    public DeploymentReadinessReport? Validation { get; init; }

    public QualityGateReport? QualityGates { get; init; }

    public IReadOnlyList<ApiProbeResult> ApiProbes { get; init; } = [];

    public IReadOnlyList<string> Warnings { get; init; } = [];

    public IReadOnlyList<string> Recommendations { get; init; } = [];

    public string? SourceRevision { get; init; }

    public string? PublishingInputFingerprint { get; init; }

    public string? PackageFingerprint { get; init; }

    public string? DockerImageIdentity { get; init; }

    public int FingerprintPolicyVersion { get; init; }
}
