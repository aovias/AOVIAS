using Aovias.Deployment.Contracts.Planning;

namespace Aovias.Deployment.Contracts.Execution;

/// <summary>
/// The shared, mutable execution context every provisioner receives (Sprint M2.5 D2). Unlike
/// <see cref="DeploymentPlan"/> (immutable, Sprint M2.4) or an <see cref="Admission.AdmissionResult"/>
/// (immutable, Sprint M2.4), this object is explicitly a live tracking record updated as
/// execution proceeds — <see cref="CurrentPhase"/>/<see cref="CurrentTaskId"/> change every step,
/// and <see cref="History"/> and <see cref="Diagnostics"/> accumulate. Every provisioner the
/// Execution Runtime calls receives the exact same instance, so state one provisioner records
/// (via <see cref="ProvisionerState"/>) is visible to every later provisioner in the same run.
/// </summary>
public sealed class DeploymentExecutionContext
{
    public required string EnvironmentId { get; init; }

    /// <summary>The single, already-approved, already-validated execution document (Sprint M2.4). The runtime never generates or modifies this — see Final Engineering Rule 3.</summary>
    public required DeploymentPlan Plan { get; init; }

    public required string RuntimeAgentVersion { get; init; }

    public DeploymentExecutionPhaseName? CurrentPhase { get; set; }

    public string? CurrentTaskId { get; set; }

    /// <summary>Arbitrary state a provisioner records for its own later use (e.g. a resolved file path) — never read or interpreted by the Execution Runtime itself.</summary>
    public IDictionary<string, object?> ProvisionerState { get; } = new Dictionary<string, object?>();

    public ExecutionHistory History { get; init; } = new();

    /// <summary>Non-blocking observations accumulated across the run (distinct from per-task <see cref="TaskExecutionOutcome.Warnings"/>, which are also folded in here).</summary>
    public List<string> Diagnostics { get; } = [];

    public DateTimeOffset StartedUtc { get; init; } = DateTimeOffset.UtcNow;
}
