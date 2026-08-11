using Aovias.Deployment.Contracts.Capabilities;

namespace Aovias.Deployment.Contracts.Planning;

/// <summary>
/// Evaluates a deployment bundle against runtime capabilities and produces a deterministic
/// deployment plan summary.
///
/// Sprint M2.3 D5 adds requirement-capability matching alongside the pre-existing
/// <see cref="RuntimeCapabilities"/> boolean checks (kept, unchanged, for backward compatibility
/// — see the M2.3 report's Technical Debt section for why both still exist side by side).
/// Provisioners never evaluate requirements themselves: <see cref="Infrastructure.Provisioners.DependencyInfrastructureProvisioner"/>,
/// the one shared pipeline stage both DRV and Runtime Agent execute, calls this planner and
/// fails the stage on <see cref="DeploymentPlanEvaluation.CanDeploy"/> being <see langword="false"/>
/// — it never re-implements the match itself.
/// </summary>
public static class DeploymentPlanner
{
    public static DeploymentPlanEvaluation Evaluate(
        ServiceDeploymentBundle bundle,
        RuntimeCapabilities runtimeCapabilities)
        => Evaluate(bundle, runtimeCapabilities, requirements: [], availableCapabilities: []);

    /// <summary>
    /// D5: evaluates <paramref name="requirements"/> (typically <c>bundle.Manifest</c>'s owning
    /// contract's <c>Requirements</c>) against <paramref name="availableCapabilities"/> (the
    /// target environment's declared capabilities — see <see cref="EnvironmentCapabilityProfileLoader"/>)
    /// via <see cref="RequirementCapabilityMatcher"/>, in addition to the legacy boolean checks.
    /// A missing mandatory requirement blocks deployment exactly like a missing legacy
    /// capability flag always has.
    /// </summary>
    public static DeploymentPlanEvaluation Evaluate(
        ServiceDeploymentBundle bundle,
        RuntimeCapabilities runtimeCapabilities,
        IReadOnlyList<Requirement> requirements,
        IReadOnlyList<Capability> availableCapabilities)
    {
        var blockers = new List<string>();
        var warnings = new List<string>();
        var orderedDependencies = TopologicalSort(bundle.Dependency.Dependencies.Select(item => item.Service).ToList());

        if (bundle.Database.Required && !runtimeCapabilities.SqlServer)
        {
            blockers.Add("Runtime does not provide SQL Server capability required by database contract.");
        }

        if (bundle.Feature.RequiresRabbitMq && !runtimeCapabilities.RabbitMq)
        {
            blockers.Add("Runtime does not provide RabbitMQ capability required by feature contract.");
        }

        if (bundle.Feature.RequiresServiceBus && !runtimeCapabilities.ServiceBus)
        {
            blockers.Add("Runtime does not provide Service Bus capability required by feature contract.");
        }

        if (bundle.Certificate.Certificates.Any(item => item.Required) && !runtimeCapabilities.Certificates)
        {
            blockers.Add("Runtime does not provide certificate provisioning capability.");
        }

        if (bundle.Feature.RequiresNginx && !runtimeCapabilities.Nginx)
        {
            warnings.Add("Service requires nginx but runtime nginx capability is disabled.");
        }

        if (bundle.IsLegacySynthesized)
        {
            warnings.Add("Deployment contracts were synthesized from legacy package metadata.");
        }

        var contractReport = Validation.ContractValidator.Validate(bundle);
        if (!contractReport.Passed)
        {
            blockers.AddRange(contractReport.Results.Where(item => !item.Passed).SelectMany(item => item.Errors.Select(error => $"{item.Name}: {error}")));
        }

        // Sprint M2.3 D4/D5: requirement-capability matching. Deliberately additive to the
        // legacy boolean checks above rather than a replacement for them in this sprint — see
        // Technical Debt in the M2.3 report.
        RequirementMatchReport? matchReport = null;
        if (requirements.Count > 0)
        {
            matchReport = RequirementCapabilityMatcher.Evaluate(requirements, availableCapabilities);
            blockers.AddRange(matchReport.Blockers);
            warnings.AddRange(matchReport.Warnings);
        }

        return new DeploymentPlanEvaluation
        {
            ServiceId = bundle.Manifest.ServiceId,
            Version = bundle.Manifest.Version,
            CanDeploy = blockers.Count == 0,
            Blockers = blockers,
            Warnings = warnings,
            StartupOrder = orderedDependencies.Append(bundle.Manifest.ServiceId).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
            RequiredFilesystemDirectories = bundle.Filesystem.Directories.Concat(bundle.Filesystem.ReadOnlyDirectories).Select(item => item.Path).ToList(),
            RequiredSecrets = bundle.Secret.Secrets.Where(item => item.Required).Select(item => item.Name).ToList(),
            RequiredCertificates = bundle.Certificate.Certificates.Where(item => item.Required).Select(item => item.Id).ToList(),
            HealthProbe = bundle.Health,
            ContractValidation = contractReport,
            RequirementMatch = matchReport
        };
    }

    private static IReadOnlyList<string> TopologicalSort(IReadOnlyList<string> dependencies)
        => dependencies.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
}

public sealed class DeploymentPlanEvaluation
{
    public required string ServiceId { get; init; }

    public required string Version { get; init; }

    public bool CanDeploy { get; init; }

    public IReadOnlyList<string> Blockers { get; init; } = [];

    public IReadOnlyList<string> Warnings { get; init; } = [];

    public IReadOnlyList<string> StartupOrder { get; init; } = [];

    public IReadOnlyList<string> RequiredFilesystemDirectories { get; init; } = [];

    public IReadOnlyList<string> RequiredSecrets { get; init; } = [];

    public IReadOnlyList<string> RequiredCertificates { get; init; } = [];

    public HealthContract? HealthProbe { get; init; }

    public Validation.ContractValidationReport? ContractValidation { get; init; }

    /// <summary>
    /// Sprint M2.3 D5's requirement-capability match result, when the caller supplied any
    /// requirements to evaluate (<see langword="null"/> otherwise — matches the legacy no-op
    /// path). Restored in Sprint M3.1: this property was set by
    /// <see cref="DeploymentPlanner.Evaluate(ServiceDeploymentBundle, RuntimeCapabilities, IReadOnlyList{Requirement}, IReadOnlyList{Capability})"/>
    /// and read by <see cref="Infrastructure.Provisioners.DependencyInfrastructureProvisioner"/>
    /// but had never been declared on this model, which is a pre-existing compile error
    /// (CS0117/CS1061) predating M2.6/M2.7 — the producer and one consumer were wired correctly,
    /// only this shared model class was never updated to match.
    /// </summary>
    public RequirementMatchReport? RequirementMatch { get; init; }
}
