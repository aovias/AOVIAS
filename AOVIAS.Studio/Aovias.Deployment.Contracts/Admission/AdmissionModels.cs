using Aovias.Deployment.Contracts.Capabilities;
using Aovias.Deployment.Contracts.Infrastructure;

namespace Aovias.Deployment.Contracts.Admission;

/// <summary>The two, and only two, outcomes of admission (Sprint M2.4 D1). No partial approval.</summary>
public enum AdmissionDecision
{
    Approved,
    Rejected
}

/// <summary>Whether a diagnostic blocks deployment or merely warns about a degraded, still-deployable outcome (Sprint M2.4 D8).</summary>
public enum AdmissionSeverity
{
    Warning,
    Blocking
}

/// <summary>
/// The eight admission stages (Sprint M2.4 D1/D4), in the exact evaluation order
/// <see cref="DeploymentAdmissionEngine"/> uses. Requirement Matching and Capability Matching are
/// two distinct categories evaluated together (they share one pass over
/// <see cref="RequirementCapabilityMatcher"/>'s output): Requirement Matching corresponds to
/// <see cref="RequirementMatchStatus.Missing"/> (no capability of the right category exists at
/// all) and Capability Matching corresponds to <see cref="RequirementMatchStatus.IncompatibleVersion"/>
/// (a capability of the right category exists, but not at a compatible version) — the brief's own
/// two named stages map exactly onto the matcher's two distinct failure modes.
/// </summary>
public enum AdmissionCategory
{
    PackageIntegrity,
    SchemaCompatibility,
    RuntimeCompatibility,
    RequirementMatching,
    CapabilityMatching,
    InfrastructureReadiness,
    EnvironmentValidation
}

/// <summary>
/// A single structured admission diagnostic (Sprint M2.4 D8). Every rejection is built from one
/// or more of these — there is no generic "unexpected error" path in
/// <see cref="DeploymentAdmissionEngine"/>.
/// </summary>
public sealed class AdmissionDiagnostic
{
    public required AdmissionCategory Category { get; init; }

    public required string Reason { get; init; }

    public required string AffectedService { get; init; }

    /// <summary>The requirement/capability category involved, when applicable (e.g. <c>"sql-database"</c>).</summary>
    public string? AffectedCapability { get; init; }

    public required string Remediation { get; init; }

    public required AdmissionSeverity Severity { get; init; }

    public required string CorrelationId { get; init; }
}

/// <summary>
/// The result of evaluating an <see cref="AdmissionRequest"/> (Sprint M2.4 D1). <see cref="Decision"/>
/// is <see cref="AdmissionDecision.Approved"/> only when every evaluated category produced no
/// <see cref="AdmissionSeverity.Blocking"/> diagnostic — there is no partial-approval state.
/// </summary>
public sealed class AdmissionResult
{
    public required AdmissionDecision Decision { get; init; }

    public required string CorrelationId { get; init; }

    public IReadOnlyList<AdmissionDiagnostic> Diagnostics { get; init; } = [];

    /// <summary>
    /// Every category the engine actually evaluated before stopping (Sprint M2.4 D4: "Failure
    /// immediately stops planning" — read here as evaluation itself stopping at the first
    /// blocking category, so a rejection's diagnostics are always about the one real cause, not
    /// a mixture of a root failure and its downstream noise).
    /// </summary>
    public IReadOnlyList<AdmissionCategory> EvaluatedCategories { get; init; } = [];

    public IReadOnlyList<AdmissionDiagnostic> Blockers
        => Diagnostics.Where(diagnostic => diagnostic.Severity == AdmissionSeverity.Blocking).ToList();

    public IReadOnlyList<AdmissionDiagnostic> Warnings
        => Diagnostics.Where(diagnostic => diagnostic.Severity == AdmissionSeverity.Warning).ToList();
}

/// <summary>
/// Pre-gathered fact for the Package Integrity admission stage (Sprint M2.4 D1). The engine
/// itself performs no I/O and re-reads no bytes — <see cref="Aovias.Deployment.Contracts.Reading.DeploymentPackageContractReader"/>
/// (M2.2) already verifies the package checksum as a precondition of successfully producing an
/// <see cref="InfrastructureContract"/> at all, so by the time an <see cref="AdmissionRequest"/>
/// exists, the caller already knows whether integrity held.
/// </summary>
public sealed class PackageIntegrityFacts
{
    public required bool Verified { get; init; }

    public string? FailureReason { get; init; }

    public static PackageIntegrityFacts Ok() => new() { Verified = true };

    public static PackageIntegrityFacts Failed(string reason) => new() { Verified = false, FailureReason = reason };
}

/// <summary>
/// Pre-gathered fact for the Environment Validation admission stage (Sprint M2.4 D1).
/// </summary>
public sealed class EnvironmentRegistrationFacts
{
    public required string EnvironmentId { get; init; }

    public required bool Registered { get; init; }

    /// <summary>
    /// The <see cref="EnvironmentCapabilityProfile"/> id this environment claims to be, if any
    /// (Sprint M2.3 D6). When set, admission confirms the profile actually resolves via
    /// <see cref="EnvironmentCapabilityProfileLoader"/> before approving.
    /// </summary>
    public string? CapabilityProfileId { get; init; }
}

/// <summary>
/// Pre-gathered fact for the Infrastructure Readiness admission stage (Sprint M2.4 D1). Sourced
/// from the existing <see cref="Infrastructure.InfrastructureProvisioningPipeline"/> (M2.0–M2.3,
/// unchanged this sprint) — Admission reuses that pipeline's verdict rather than re-implementing
/// infrastructure readiness checks itself.
/// </summary>
public sealed class InfrastructureReadinessFacts
{
    public required bool Ready { get; init; }

    public IReadOnlyList<string> Blockers { get; init; } = [];

    public static InfrastructureReadinessFacts FromReadinessReport(InfrastructureReadinessReport report)
        => new() { Ready = report.DeploymentAllowed, Blockers = report.Blockers };
}

/// <summary>
/// Everything <see cref="DeploymentAdmissionEngine"/> needs to reach a deterministic decision.
/// Deliberately contains no live I/O handles — every fact is pre-gathered by the caller (Sprint
/// M2.4 D1/D4), which is what makes <see cref="IDeploymentAdmissionEngine.Evaluate"/> a pure,
/// synchronous, trivially-unit-testable function.
/// </summary>
public sealed class AdmissionRequest
{
    public required string ServiceId { get; init; }

    /// <summary>Threads through every diagnostic and the final <see cref="AdmissionResult"/>, so a rejection can always be traced back to one deployment attempt (Sprint M2.4 D8).</summary>
    public required string CorrelationId { get; init; }

    public required InfrastructureContract Contract { get; init; }

    public required PackageIntegrityFacts PackageIntegrity { get; init; }

    public required string RuntimeAgentVersion { get; init; }

    /// <summary>
    /// Optional minimum Runtime Agent version this package requires, evaluated via the same
    /// <see cref="VersionConstraint"/> grammar Sprint M2.3 introduced for capability versions.
    /// <see langword="null"/> means no constraint is declared — every runtime version is
    /// compatible (today's platform has no per-package minimum runtime version field; this is a
    /// forward-compatible hook, see the M2.4 report's Technical Debt section).
    /// </summary>
    public string? MinimumRuntimeAgentVersion { get; init; }

    public required IReadOnlyList<Capability> AvailableCapabilities { get; init; }

    public required EnvironmentRegistrationFacts Environment { get; init; }

    public required InfrastructureReadinessFacts InfrastructureReadiness { get; init; }
}
