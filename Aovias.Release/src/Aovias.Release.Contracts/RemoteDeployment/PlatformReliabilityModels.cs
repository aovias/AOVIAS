using Aovias.Release.Contracts.RemoteDeployment;

namespace Aovias.Release.Contracts.RemoteDeployment;

/// <summary>
/// Certification gate categories evaluated before a platform deployment is marked successful.
/// </summary>
public enum CertificationGateStatus
{
    Pending,
    Pass,
    Fail,
    Skipped
}

/// <summary>
/// Platform-level certification report generated after deployment validation.
/// </summary>
public sealed class PlatformCertificationReport
{
    public required string DeploymentId { get; init; }

    public required string PlatformVersion { get; init; }

    public required string RuntimeVersion { get; init; }

    public DateTimeOffset GeneratedUtc { get; init; }

    public CertificationGateStatus Infrastructure { get; init; }

    public CertificationGateStatus Deployment { get; init; }

    public CertificationGateStatus SmokeTests { get; init; }

    public CertificationGateStatus ApiReachability { get; init; }

    public CertificationGateStatus DatabaseConnectivity { get; init; }

    public CertificationGateStatus CertificateValidity { get; init; }

    public CertificationGateStatus SecretResolution { get; init; }

    public CertificationGateStatus DependencyGraph { get; init; }

    public CertificationGateStatus Overall { get; init; }

    public bool IsCertified => Overall == CertificationGateStatus.Pass;

    public IReadOnlyList<string> Findings { get; init; } = [];

    public SmokeTestSuiteSummary SmokeTestSummary { get; init; } = new();

    public PlatformCertificationChecklist Checklist { get; init; } = new();
}

/// <summary>
/// Smoke test summary embedded in certification reports.
/// </summary>
public sealed class SmokeTestSuiteSummary
{
    public int Passed { get; init; }

    public int Failed { get; init; }

    public int Total { get; init; }

    public IReadOnlyList<string> FailedTests { get; init; } = [];
}

/// <summary>
/// Continuous deployment simulation result after publish.
/// </summary>
public sealed class PlatformDeploymentSimulationResult
{
    public required string SimulationId { get; init; }

    public bool Passed { get; init; }

    public string Message { get; init; } = string.Empty;

    public DateTimeOffset StartedUtc { get; init; }

    public DateTimeOffset CompletedUtc { get; init; }

    public IReadOnlyList<string> Steps { get; init; } = [];

    public PlatformCertificationReport? CertificationPreview { get; init; }
}

/// <summary>
/// Persisted deployment replay payload for deterministic re-execution.
/// </summary>
public sealed class DeploymentReplayRecord
{
    public required string DeploymentId { get; init; }

    public required string ReplayToken { get; init; }

    public required PlatformDeploymentRequest Request { get; init; }

    public PlatformDeploymentState FinalState { get; init; }

    public DateTimeOffset RecordedUtc { get; init; }

    public string PayloadHash { get; init; } = string.Empty;
}

/// <summary>
/// Result of replaying a prior deployment.
/// </summary>
public sealed class DeploymentReplayResult
{
    public required string OriginalDeploymentId { get; init; }

    public required string ReplayDeploymentId { get; init; }

    public bool IdenticalOutcome { get; init; }

    public string Message { get; init; } = string.Empty;
}

/// <summary>
/// Chaos test scenario identifiers.
/// </summary>
public enum ChaosScenario
{
    ContainerRestart,
    SqlUnavailable,
    RabbitMqUnavailable,
    NetworkInterruption,
    DiskPressure,
    RuntimeAgentRestart
}

/// <summary>
/// Result of an automated chaos scenario.
/// </summary>
public sealed class ChaosTestResult
{
    public required ChaosScenario Scenario { get; init; }

    public bool Recovered { get; init; }

    public TimeSpan RecoveryDuration { get; init; }

    public string Message { get; init; } = string.Empty;
}

/// <summary>
/// Self-healing reconciliation action record.
/// </summary>
public sealed class ReconciliationAction
{
    public required string Category { get; init; }

    public required string Resource { get; init; }

    public required string Action { get; init; }

    public bool Succeeded { get; init; }

    public string Message { get; init; } = string.Empty;

    public DateTimeOffset TimestampUtc { get; init; }
}

/// <summary>
/// Reconciliation report comparing desired vs actual state.
/// </summary>
public sealed class PlatformReconciliationReport
{
    public DateTimeOffset GeneratedUtc { get; init; }

    public IReadOnlyList<ReconciliationAction> Actions { get; init; } = [];

    public bool RequiresOperatorAttention { get; init; }
}

/// <summary>
/// Deployment reliability KPI snapshot.
/// </summary>
public sealed class DeploymentReliabilityMetrics
{
    public DateTimeOffset CollectedUtc { get; init; }

    public double DeploymentSuccessRate { get; init; }

    public TimeSpan MeanDeploymentTime { get; init; }

    public TimeSpan MeanRollbackTime { get; init; }

    public TimeSpan MeanRecoveryTime { get; init; }

    public IReadOnlyDictionary<string, int> FailuresByCategory { get; init; }
        = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

    public int InfrastructureRepairActions { get; init; }

    public double HealthStabilityScore { get; init; }

    public int TotalDeployments { get; init; }

    public int SuccessfulDeployments { get; init; }

    public int FailedDeployments { get; init; }
}
