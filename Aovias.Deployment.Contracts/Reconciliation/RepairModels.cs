namespace Aovias.Deployment.Contracts.Reconciliation;

/// <summary>
/// The concrete kind of corrective action a repair task performs (Sprint M2.6 D5/D6). The first
/// six values are exactly the brief's "allowed automatically" list; the following six are exactly
/// its "require operator approval" list; <see cref="Unknown"/> is the fail-safe default for any
/// drift this sprint's matrix does not (yet) map to a named, safe strategy (D11: never guess a
/// repair strategy).
/// </summary>
public enum RepairActionType
{
    // Allowed automatically (subject to policy — see RepairPolicy).
    RecreateDockerNetwork,
    RecreateDirectory,
    RestartContainer,
    RestorePermissions,
    RecreateSymbolicLink,
    ReloadConfiguration,

    // Always require operator approval.
    DeleteVolume,
    DatabaseChange,
    CertificateReplacement,
    SecretReplacement,
    ImageDowngrade,
    RollbackDeployment,

    /// <summary>No safe, unambiguous repair strategy is known for this finding — always requires operator approval (D11: "multiple repair strategies exist" / "repair increases uncertainty").</summary>
    Unknown
}

public enum RepairRisk
{
    Low,
    Medium,
    High
}

/// <summary>One proposed corrective action for one <see cref="DriftFinding"/> (Sprint M2.6 D5). Planning only — constructing a <see cref="RepairTask"/> never executes anything (Final Engineering Rule: "The Reconciliation Planner decides how drift should be resolved" — deciding is not doing).</summary>
public sealed class RepairTask
{
    public required string TaskId { get; init; }

    public required string FindingId { get; init; }

    public required RepairActionType ActionType { get; init; }

    public required string ResourceType { get; init; }

    public required string ResourceId { get; init; }

    public required string Description { get; init; }

    public required RepairRisk Risk { get; init; }

    /// <summary>
    /// The matrix's fixed opinion on whether this action type may ever run without an operator,
    /// independent of environment-specific <see cref="IRepairPolicyEvaluator"/> evaluation. <see langword="null"/>
    /// means the matrix itself defers entirely to policy (e.g. Configuration Drift — see
    /// <see cref="DriftRepairMatrix"/>).
    /// </summary>
    public required bool? RequiresApprovalByDefault { get; init; }
}

/// <summary>
/// The output of one <see cref="IReconciliationPlanner.Plan"/> call (Sprint M2.6 D5): every
/// <see cref="RepairTask"/> derived from a <see cref="DriftReport"/>, in the order they should be
/// attempted. No repair execution occurs while building this plan.
/// </summary>
public sealed class ReconciliationPlan
{
    public required string EnvironmentId { get; init; }

    public required string ServiceId { get; init; }

    public required string CorrelationId { get; init; }

    public required DateTimeOffset GeneratedUtc { get; init; }

    /// <summary>Execution order: ascending <see cref="RepairRisk"/>, so the lowest-risk, most likely to succeed repairs (e.g. restarting a container) are attempted before higher-risk ones — a deliberate, deterministic tie-break, not the input findings' order.</summary>
    public IReadOnlyList<RepairTask> RepairTasks { get; init; } = [];

    public bool RequiresOperatorApproval => RepairTasks.Any(task => task.RequiresApprovalByDefault != false);
}

/// <summary>
/// The single, deterministic mapping from (<see cref="DriftCategory"/>, resource type) to a
/// repair strategy, severity, and default automatic-repair/approval posture (Sprint M2.6 D4/D5/D6)
/// — this is the code-form of the M2.6 report's Drift Detection Matrix (§4); both
/// <see cref="DriftDetectionEngine"/> (for severity) and <see cref="ReconciliationPlanner"/> (for
/// repair mapping) read from this one table so the two can never drift out of sync with each
/// other or with the published report.
/// </summary>
public static class DriftRepairMatrix
{
    /// <param name="Category">The drift category this entry matches.</param>
    /// <param name="ResourceType">The resource type this entry matches (see <see cref="ReconciliationResourceTypes"/>).</param>
    /// <param name="Severity">The severity this drift/resource combination is classified as.</param>
    /// <param name="ActionType">The repair action to take when this entry matches.</param>
    /// <param name="Risk">The risk tier of the repair action, used to order repair attempts.</param>
    /// <param name="RequiresApprovalByDefault">null = the matrix itself does not fix this; an <see cref="IRepairPolicyEvaluator"/> decides both whether to attempt a repair at all and whether it needs approval (e.g. Configuration Drift).</param>
    public sealed record Entry(
        DriftCategory Category,
        string ResourceType,
        DriftSeverity Severity,
        RepairActionType ActionType,
        RepairRisk Risk,
        bool? RequiresApprovalByDefault);

    private static readonly Entry DefaultEntry = new(
        DriftCategory.ExtraResource, ReconciliationResourceTypes.Service, DriftSeverity.Security, RepairActionType.Unknown, RepairRisk.High, true);

    private static readonly IReadOnlyDictionary<(DriftCategory Category, string ResourceType), Entry> Entries =
        new List<Entry>
        {
            new(DriftCategory.MissingResource, ReconciliationResourceTypes.DockerNetwork, DriftSeverity.Recoverable, RepairActionType.RecreateDockerNetwork, RepairRisk.Low, false),
            new(DriftCategory.MissingResource, ReconciliationResourceTypes.FilesystemPath, DriftSeverity.Recoverable, RepairActionType.RecreateDirectory, RepairRisk.Low, false),
            new(DriftCategory.MissingResource, ReconciliationResourceTypes.SymbolicLink, DriftSeverity.Recoverable, RepairActionType.RecreateSymbolicLink, RepairRisk.Low, false),
            new(DriftCategory.MissingResource, ReconciliationResourceTypes.Container, DriftSeverity.Recoverable, RepairActionType.RestartContainer, RepairRisk.Low, false),
            new(DriftCategory.MissingResource, ReconciliationResourceTypes.Secret, DriftSeverity.Critical, RepairActionType.SecretReplacement, RepairRisk.High, true),
            new(DriftCategory.MissingResource, ReconciliationResourceTypes.Volume, DriftSeverity.Critical, RepairActionType.DeleteVolume, RepairRisk.High, true),

            new(DriftCategory.UnhealthyService, ReconciliationResourceTypes.Container, DriftSeverity.Recoverable, RepairActionType.RestartContainer, RepairRisk.Low, false),
            new(DriftCategory.UnhealthyService, ReconciliationResourceTypes.HealthEndpoint, DriftSeverity.Recoverable, RepairActionType.RestartContainer, RepairRisk.Low, false),

            new(DriftCategory.PermissionDrift, ReconciliationResourceTypes.FilesystemPath, DriftSeverity.Recoverable, RepairActionType.RestorePermissions, RepairRisk.Low, false),
            // Permission drift on a path holding certificate/secret material is a security
            // concern, not routine drift (D4: the "Security" tier exists precisely for this) —
            // still auto-repairable (restoring a stricter permission is always safe), but always
            // reported to an operator regardless of policy.
            new(DriftCategory.PermissionDrift, ReconciliationResourceTypes.SensitivePath, DriftSeverity.Security, RepairActionType.RestorePermissions, RepairRisk.Low, true),

            new(DriftCategory.ConfigurationDrift, ReconciliationResourceTypes.ConfigurationKey, DriftSeverity.Warning, RepairActionType.ReloadConfiguration, RepairRisk.Medium, null),

            new(DriftCategory.MissingCertificate, ReconciliationResourceTypes.Certificate, DriftSeverity.Critical, RepairActionType.CertificateReplacement, RepairRisk.High, true),

            new(DriftCategory.SecretDrift, ReconciliationResourceTypes.Secret, DriftSeverity.Critical, RepairActionType.SecretReplacement, RepairRisk.High, true),

            new(DriftCategory.StorageDrift, ReconciliationResourceTypes.Volume, DriftSeverity.Critical, RepairActionType.DeleteVolume, RepairRisk.High, true),

            new(DriftCategory.VersionMismatch, ReconciliationResourceTypes.PackageVersion, DriftSeverity.Critical, RepairActionType.ImageDowngrade, RepairRisk.High, true),

            new(DriftCategory.NetworkDrift, ReconciliationResourceTypes.DockerNetwork, DriftSeverity.Critical, RepairActionType.RollbackDeployment, RepairRisk.High, true),

            new(DriftCategory.ExtraResource, ReconciliationResourceTypes.Container, DriftSeverity.Warning, RepairActionType.Unknown, RepairRisk.Medium, true),
            new(DriftCategory.ExtraResource, ReconciliationResourceTypes.DockerNetwork, DriftSeverity.Warning, RepairActionType.Unknown, RepairRisk.Medium, true),
            new(DriftCategory.ExtraResource, ReconciliationResourceTypes.Volume, DriftSeverity.Warning, RepairActionType.Unknown, RepairRisk.Medium, true)
        }.ToDictionary(entry => (entry.Category, entry.ResourceType));

    /// <summary>Looks up the fixed classification/repair entry for a (category, resource type) pair, falling back to the most conservative possible entry (Security severity, <see cref="RepairActionType.Unknown"/>, approval always required) for anything not yet explicitly mapped — D11: fail safely rather than guess.</summary>
    public static Entry Resolve(DriftCategory category, string resourceType)
        => Entries.TryGetValue((category, resourceType), out var entry) ? entry : DefaultEntry with { Category = category, ResourceType = resourceType };
}
