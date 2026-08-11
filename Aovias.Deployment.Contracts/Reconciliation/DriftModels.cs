namespace Aovias.Deployment.Contracts.Reconciliation;

/// <summary>What kind of difference was found between Desired and Observed State (Sprint M2.6 D3).</summary>
public enum DriftCategory
{
    MissingResource,
    ExtraResource,
    VersionMismatch,
    UnhealthyService,
    MissingCertificate,
    ConfigurationDrift,
    PermissionDrift,
    NetworkDrift,
    SecretDrift,
    StorageDrift
}

/// <summary>
/// The fixed classification ladder every drift finding is assigned (Sprint M2.6 D4), in
/// ascending order of how much a finding constrains allowable repair actions. Declared in this
/// exact order so <c>Severity1 &lt; Severity2</c> comparisons (used by the Reconciliation
/// Planner's execution ordering) are meaningful.
/// </summary>
public enum DriftSeverity
{
    Informational,
    Warning,
    Recoverable,
    Critical,
    Security
}

/// <summary>One specific difference between Desired and Observed State (Sprint M2.6 D3/D4).</summary>
public sealed class DriftFinding
{
    public required string FindingId { get; init; }

    public required DriftCategory Category { get; init; }

    public required DriftSeverity Severity { get; init; }

    /// <summary>One of the <see cref="ReconciliationResourceTypes"/> constants.</summary>
    public required string ResourceType { get; init; }

    public required string ResourceId { get; init; }

    public required string Expected { get; init; }

    public required string Actual { get; init; }

    public required string Description { get; init; }
}

/// <summary>
/// The output of one <see cref="IDriftDetectionEngine.DetectDrift"/> call (Sprint M2.6 D3): every
/// difference found between one <see cref="DesiredState"/> and one <see cref="ObservedState"/>,
/// already classified. Contains no repair decisions — Drift Detection never performs or proposes
/// repairs (Final Engineering Rule: "Drift Detection never performs repairs").
/// </summary>
public sealed class DriftReport
{
    public required string EnvironmentId { get; init; }

    public required string ServiceId { get; init; }

    public required string CorrelationId { get; init; }

    public required DateTimeOffset GeneratedUtc { get; init; }

    public IReadOnlyList<DriftFinding> Findings { get; init; } = [];

    public bool HasDrift => Findings.Count > 0;

    public bool HasCriticalOrSecurityDrift => Findings.Any(finding => finding.Severity is DriftSeverity.Critical or DriftSeverity.Security);
}

/// <summary>Compares Desired State against Observed State and classifies every difference (Sprint M2.6 D3). The sole implementation is <see cref="DriftDetectionEngine"/>.</summary>
public interface IDriftDetectionEngine
{
    DriftReport DetectDrift(DesiredState desired, ObservedState observed, string correlationId);
}
