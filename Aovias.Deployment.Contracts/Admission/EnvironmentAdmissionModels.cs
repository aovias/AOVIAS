using Aovias.Deployment.Contracts.Capabilities;

namespace Aovias.Deployment.Contracts.Admission;

/// <summary>
/// Sprint M4.4 — the outcome of matching one requirement of one already-known service against an
/// environment's declared <see cref="EnvironmentCapabilityProfile"/>, classified into the four
/// states ROADMAP.md's M4.4 acceptance criterion names ("admission matrix E2E
/// (satisfied/missing/external/deferrable)"). This is a labeling layer over
/// <see cref="RequirementMatchStatus"/> — it introduces no second matching algorithm:
/// <see cref="EnvironmentAdmissionEvaluator"/> classifies every <see cref="RequirementMatchResult"/>
/// <see cref="RequirementCapabilityMatcher"/> (Sprint M2.3 D4) already produced.
/// </summary>
public enum EnvironmentRequirementStatus
{
    /// <summary>A capability satisfies the requirement (<see cref="RequirementMatchStatus.Satisfied"/>).</summary>
    Satisfied,

    /// <summary>
    /// Unmet, but the requirement's category is one no environment capability profile is meant to
    /// satisfy (<see cref="RequirementCategories.ExternallyProvisionedCategories"/>) — fulfilled at
    /// deploy time by configuration/secrets, not by this environment's own infrastructure. Never
    /// blocks readiness.
    /// </summary>
    External,

    /// <summary>Unmet, but the requirement is declared non-mandatory (<see cref="Requirement.Mandatory"/> is <see langword="false"/>) — the gap can be resolved later without blocking deployment today.</summary>
    Deferrable,

    /// <summary>Unmet, mandatory, and not externally provisioned — this is what blocks Deployment Ready.</summary>
    Missing
}

/// <summary>One requirement's admission classification, scoped to the service that declared it (Sprint M4.4).</summary>
public sealed class EnvironmentRequirementDiagnostic
{
    public required string ServiceId { get; init; }

    public required string RequirementId { get; init; }

    public required string Category { get; init; }

    public required EnvironmentRequirementStatus Status { get; init; }

    /// <summary>The matcher's own message — for unmet requirements this already includes remediation guidance (Sprint M2.3 D9), reused verbatim rather than re-worded.</summary>
    public required string Message { get; init; }
}

/// <summary>
/// The environment-level Deployment Ready state (Sprint M4.4) — deliberately distinct from
/// <c>Aovias.Publishing.Contracts.DeploymentReadinessReport</c> (ADR-0009 tier T2: is a
/// *package* environment-independently valid) and from <see cref="AdmissionResult"/> (tier T3
/// per-package admission at the moment of one deployment attempt). This report answers a third,
/// narrower question: "given everything this environment has been asked to run so far, is its
/// declared capability profile still sufficient?" — evaluated continuously, not once per deployment.
/// </summary>
public sealed class EnvironmentAdmissionReport
{
    public required string CapabilityProfileId { get; init; }

    /// <summary><see langword="true"/> only when no diagnostic is <see cref="EnvironmentRequirementStatus.Missing"/> — mirrors <see cref="AdmissionResult"/>'s "no partial approval" rule (Sprint M2.4 D1) at environment scope.</summary>
    public required bool IsReady { get; init; }

    public IReadOnlyList<EnvironmentRequirementDiagnostic> Diagnostics { get; init; } = [];

    public IReadOnlyList<string> ServiceIdsEvaluated { get; init; } = [];

    public DateTimeOffset EvaluatedUtc { get; init; } = DateTimeOffset.UtcNow;

    public int SatisfiedCount => Diagnostics.Count(d => d.Status == EnvironmentRequirementStatus.Satisfied);

    public int ExternalCount => Diagnostics.Count(d => d.Status == EnvironmentRequirementStatus.External);

    public int DeferrableCount => Diagnostics.Count(d => d.Status == EnvironmentRequirementStatus.Deferrable);

    public int MissingCount => Diagnostics.Count(d => d.Status == EnvironmentRequirementStatus.Missing);

    /// <summary>Every blocking diagnostic's message — what <c>EnvironmentAdmissionHostedService</c> and the Runtime Agent API surface as actionable remediation text.</summary>
    public IReadOnlyList<string> Blockers => Diagnostics
        .Where(d => d.Status == EnvironmentRequirementStatus.Missing)
        .Select(d => d.Message)
        .ToList();
}
