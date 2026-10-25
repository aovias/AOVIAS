using Aovias.Deployment.Contracts.Capabilities;

namespace Aovias.Deployment.Contracts.Planning;

/// <summary>
/// The fixed, deterministic execution phase sequence (Sprint M2.4 D5). Every
/// <see cref="DeploymentPlan"/> declares exactly these eight phases, in exactly this order —
/// the sequence itself is platform policy, not something a plan can reorder or omit from.
/// </summary>
public enum DeploymentExecutionPhaseName
{
    Preflight = 1,
    Infrastructure = 2,
    Upload = 3,
    Configuration = 4,
    Deployment = 5,
    Health = 6,
    Certification = 7,
    Completion = 8
}

/// <summary>Which service and package version this plan was produced for, and when (Sprint M2.4 D3).</summary>
public sealed class DeploymentPlanMetadata
{
    /// <summary>Deterministic identity: <c>{ServiceId}:{ServiceVersion}:{ContractContentHash}</c> — never a random GUID, so the same approved deployment always yields the same plan id.</summary>
    public required string PlanId { get; init; }

    public required string ServiceId { get; init; }

    public required string ServiceVersion { get; init; }

    public required int SchemaVersion { get; init; }

    /// <summary>The <see cref="Admission.AdmissionResult.CorrelationId"/> of the admission decision that authorized this plan (Final Engineering Rule 1: Admission precedes planning).</summary>
    public required string CorrelationId { get; init; }

    /// <summary>Supplied by the caller from a fixed point in time (e.g. deployment start), never a fresh clock read inside the planner — mirrors the determinism discipline <c>ContractPublishingMetadata.GeneratedUtc</c> established in Sprint M2.2.</summary>
    public required DateTimeOffset GeneratedUtc { get; init; }
}

/// <summary>Which environment this plan targets (Sprint M2.4 D3).</summary>
public sealed class DeploymentPlanEnvironment
{
    public required string EnvironmentId { get; init; }

    /// <summary>The <see cref="EnvironmentCapabilityProfile"/> id this environment was validated against during admission, if any (Sprint M2.3 D6).</summary>
    public string? CapabilityProfileId { get; init; }
}

/// <summary>One entry in the plan's service graph: the primary service plus every declared dependency (Sprint M2.4 D3).</summary>
public sealed class DeploymentPlanServiceEntry
{
    public required string ServiceId { get; init; }

    public required bool Required { get; init; }

    public required string Type { get; init; }
}

/// <summary>
/// The infrastructure section of the plan (Sprint M2.4 D3): what the contract requires, restated
/// alongside the requirement-capability match report (Sprint M2.3 D4) that was already evaluated
/// during admission, so the plan is self-describing without a caller needing to re-run matching.
/// </summary>
public sealed class DeploymentPlanInfrastructure
{
    public IReadOnlyList<Requirement> Requirements { get; init; } = [];

    public IReadOnlyList<string> RequiredFilesystemDirectories { get; init; } = [];

    public IReadOnlyList<string> RequiredSecrets { get; init; } = [];

    public IReadOnlyList<string> RequiredCertificates { get; init; } = [];

    public RequirementMatchReport? RequirementMatch { get; init; }
}

/// <summary>A single unit of execution within a phase (Sprint M2.4 D5/D6). Runtime provisioners consume tasks; they do not create or reorder them.</summary>
public sealed class DeploymentTask
{
    public required string TaskId { get; init; }

    public required string Description { get; init; }

    /// <summary>Every id here must resolve to a real task elsewhere in the same plan — an unresolvable id is an orphaned dependency (Sprint M2.4 D9).</summary>
    public IReadOnlyList<string> DependsOnTaskIds { get; init; } = [];
}

/// <summary>One of the plan's eight fixed phases, populated with the deterministic tasks this specific deployment requires (Sprint M2.4 D5).</summary>
public sealed class DeploymentExecutionPhase
{
    public required DeploymentExecutionPhaseName Name { get; init; }

    /// <summary>1-based position in the fixed phase sequence — always equal to <c>(int)Name</c>, restated as data so a validator can check it without an enum-to-int assumption baked in.</summary>
    public required int Order { get; init; }

    public IReadOnlyList<DeploymentTask> Tasks { get; init; } = [];
}

/// <summary>A point in the plan where health must be verified before execution proceeds (Sprint M2.4 D3/D5).</summary>
public sealed class HealthGate
{
    public required string Id { get; init; }

    public required DeploymentExecutionPhaseName Phase { get; init; }

    public required string Description { get; init; }
}

/// <summary>A point identified during planning where execution could safely be rolled back if a later phase fails (Sprint M2.4 D7). Planning only — no execution of a rollback happens here.</summary>
public sealed class RollbackCheckpoint
{
    public required string Id { get; init; }

    public required DeploymentExecutionPhaseName BeforePhase { get; init; }

    public required string Description { get; init; }
}

/// <summary>A gate the deployment must pass before it is considered certified/accepted (Sprint M2.4 D3/D5) — mirrors the checks <c>VerifyDeploymentAcceptanceAsync</c> already performs, restated as planned data rather than only as ad hoc runtime logic.</summary>
public sealed class CertificationGate
{
    public required string Id { get; init; }

    public required DeploymentExecutionPhaseName Phase { get; init; }

    public required string Description { get; init; }
}

/// <summary>What should happen if execution fails after this plan was approved (Sprint M2.4 D3/D7). Describes intent only — the execution engine that would act on it is out of this sprint's scope.</summary>
public enum RecoveryStrategyType
{
    RollbackToPrevious,
    None
}

public sealed class RecoveryStrategy
{
    public required RecoveryStrategyType Type { get; init; }

    public required string Description { get; init; }

    /// <summary>Which phases, if they fail, should trigger this recovery strategy.</summary>
    public IReadOnlyList<DeploymentExecutionPhaseName> TriggersOnPhaseFailure { get; init; } = [];
}

/// <summary>Plan-level diagnostics (Sprint M2.4 D3) — distinct from <see cref="Admission.AdmissionDiagnostic"/>: these describe the plan itself (its warnings, its content hash), not the admission decision that authorized it.</summary>
public sealed class PlanDiagnostics
{
    /// <summary>Non-blocking admission warnings (e.g. an unmet optional requirement), carried forward so a plan is self-describing about degraded-but-approved conditions.</summary>
    public IReadOnlyList<string> Warnings { get; init; } = [];

    public required string AdmissionCorrelationId { get; init; }

    /// <summary>SHA-256 over the plan's deterministic content (Sprint M2.4 D10), excluding <see cref="DeploymentPlanMetadata.GeneratedUtc"/> and <see cref="DeploymentPlanMetadata.CorrelationId"/> — see <see cref="DeploymentPlanIntegrity"/>.</summary>
    public required string ContentHash { get; init; }
}

/// <summary>
/// The single execution document a deployment produces (Sprint M2.4 D3). Immutable once
/// constructed — every property is init-only and every collection is a read-only list — matching
/// Final Engineering Rule 4: "Deployment Plans are immutable after approval." Runtime
/// provisioners execute this document; they do not decide whether to, and they do not modify it.
/// </summary>
public sealed class DeploymentPlan
{
    public required DeploymentPlanMetadata Metadata { get; init; }

    public required DeploymentPlanEnvironment Environment { get; init; }

    public required IReadOnlyList<DeploymentPlanServiceEntry> Services { get; init; }

    public required DeploymentPlanInfrastructure Infrastructure { get; init; }

    /// <summary>Deterministic service startup order (Sprint M2.4 D2) — the same ordering basis <c>Planning.DeploymentPlanner.Evaluate</c> already computes for the legacy boolean-gated path.</summary>
    public required IReadOnlyList<string> Dependencies { get; init; }

    public required IReadOnlyList<DeploymentExecutionPhase> ExecutionPhases { get; init; }

    public required IReadOnlyList<HealthGate> HealthGates { get; init; }

    public required IReadOnlyList<RollbackCheckpoint> RollbackPoints { get; init; }

    public required IReadOnlyList<CertificationGate> CertificationGates { get; init; }

    public required RecoveryStrategy Recovery { get; init; }

    public required PlanDiagnostics Diagnostics { get; init; }
}
