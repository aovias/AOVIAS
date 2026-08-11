using Aovias.Deployment.Contracts.Admission;
using Aovias.Deployment.Contracts.Capabilities;
using Aovias.Deployment.Contracts.Infrastructure;

namespace Aovias.Deployment.Contracts.Planning;

/// <summary>
/// The single implementation of <see cref="IDeploymentPlanner"/> (Sprint M2.4 D2). Builds one
/// deterministic <see cref="DeploymentPlan"/> per approved deployment: a linear task chain
/// spanning the eight fixed execution phases (D5), rollback checkpoints (D7), certification
/// gates, and the requirement-capability match report already evaluated during admission.
///
/// Determinism strategy: every task id, phase order, checkpoint id, and gate id is derived
/// purely from the contract/bundle's own declared data (service id, requirement categories,
/// health probe paths) — nothing here reads a clock, a random number, or ambient state. The one
/// exception, <see cref="DeploymentPlanMetadata.GeneratedUtc"/>, is supplied by the caller
/// (Sprint M2.2's <c>ContractPublishingMetadata</c> pattern) and is excluded from
/// <see cref="DeploymentPlanIntegrity.ComputeContentHash"/> for exactly that reason.
/// </summary>
public sealed class DeploymentPlanBuilder : IDeploymentPlanner
{
    public DeploymentPlan CreatePlan(DeploymentPlanRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Admission.Decision != AdmissionDecision.Approved)
        {
            throw new InvalidOperationException(
                "A Deployment Plan can only be created from an Approved AdmissionResult. Planning must never run for a rejected deployment (Final Engineering Rule: Admission precedes planning).");
        }

        var bundle = request.Bundle;
        var contract = request.Contract;
        var matchReport = RequirementCapabilityMatcher.Evaluate(contract.Requirements, request.AvailableCapabilities);

        var services = BuildServices(bundle);
        var startupOrder = BuildStartupOrder(bundle);
        var phases = BuildExecutionPhases(bundle, contract, startupOrder);
        var healthGates = BuildHealthGates(bundle);
        var rollbackPoints = BuildRollbackPoints(bundle);
        var certificationGates = BuildCertificationGates();
        var recovery = BuildRecoveryStrategy(rollbackPoints);

        var infrastructure = new DeploymentPlanInfrastructure
        {
            Requirements = contract.Requirements,
            RequiredFilesystemDirectories = bundle.Filesystem.Directories
                .Concat(bundle.Filesystem.ReadOnlyDirectories)
                .Select(directory => directory.Path)
                .ToList(),
            RequiredSecrets = bundle.Secret.Secrets.Where(secret => secret.Required).Select(secret => secret.Name).ToList(),
            RequiredCertificates = bundle.Certificate.Certificates.Where(certificate => certificate.Required).Select(certificate => certificate.Id).ToList(),
            RequirementMatch = matchReport
        };

        var metadata = new DeploymentPlanMetadata
        {
            PlanId = $"{bundle.Manifest.ServiceId}:{bundle.Manifest.Version}:{contract.Integrity.ContentHash}",
            ServiceId = bundle.Manifest.ServiceId,
            ServiceVersion = bundle.Manifest.Version,
            SchemaVersion = contract.SchemaVersion,
            CorrelationId = request.Admission.CorrelationId,
            GeneratedUtc = request.GeneratedUtc
        };

        var environment = new DeploymentPlanEnvironment
        {
            EnvironmentId = request.EnvironmentId,
            CapabilityProfileId = request.CapabilityProfileId
        };

        var plan = new DeploymentPlan
        {
            Metadata = metadata,
            Environment = environment,
            Services = services,
            Infrastructure = infrastructure,
            Dependencies = startupOrder,
            ExecutionPhases = phases,
            HealthGates = healthGates,
            RollbackPoints = rollbackPoints,
            CertificationGates = certificationGates,
            Recovery = recovery,
            Diagnostics = new PlanDiagnostics
            {
                Warnings = request.Admission.Warnings.Select(warning => warning.Reason).ToList(),
                AdmissionCorrelationId = request.Admission.CorrelationId,
                ContentHash = string.Empty // computed below, once the rest of the plan is final
            }
        };

        // The content hash covers the plan's own diagnostics warnings list, so it must be
        // computed against a plan that already has everything else finalized, then folded back
        // in as the very last step.
        var contentHash = DeploymentPlanIntegrity.ComputeContentHash(plan);
        return new DeploymentPlan
        {
            Metadata = plan.Metadata,
            Environment = plan.Environment,
            Services = plan.Services,
            Infrastructure = plan.Infrastructure,
            Dependencies = plan.Dependencies,
            ExecutionPhases = plan.ExecutionPhases,
            HealthGates = plan.HealthGates,
            RollbackPoints = plan.RollbackPoints,
            CertificationGates = plan.CertificationGates,
            Recovery = plan.Recovery,
            Diagnostics = new PlanDiagnostics
            {
                Warnings = plan.Diagnostics.Warnings,
                AdmissionCorrelationId = plan.Diagnostics.AdmissionCorrelationId,
                ContentHash = contentHash
            }
        };
    }

    /// <summary>D2: the primary service plus every declared dependency, in declaration order.</summary>
    private static IReadOnlyList<DeploymentPlanServiceEntry> BuildServices(ServiceDeploymentBundle bundle)
    {
        var entries = new List<DeploymentPlanServiceEntry>
        {
            new() { ServiceId = bundle.Manifest.ServiceId, Required = true, Type = "primary" }
        };
        entries.AddRange(bundle.Dependency.Dependencies.Select(dependency =>
            new DeploymentPlanServiceEntry { ServiceId = dependency.Service, Required = dependency.Required, Type = dependency.Type }));
        return entries;
    }

    /// <summary>
    /// D2: service startup order. Deliberately mirrors the same (deduplication-only, not a true
    /// dependency-graph topological sort) approach <c>Planning.DeploymentPlanner</c>'s private
    /// <c>TopologicalSort</c> has used since Sprint M2.0 — see the M2.4 report's Technical Debt
    /// section for why a real multi-level topological sort remains unimplemented platform-wide.
    /// </summary>
    private static IReadOnlyList<string> BuildStartupOrder(ServiceDeploymentBundle bundle)
    {
        var dependencyIds = bundle.Dependency.Dependencies.Select(dependency => dependency.Service);
        return dependencyIds
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Append(bundle.Manifest.ServiceId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// D5: the fixed eight-phase sequence, each populated with a deterministic task list. Tasks
    /// form one long linear chain end-to-end (each task depends on the previous one; the first
    /// task of phase N+1 depends on the last task of phase N) — this is what keeps the plan
    /// trivially free of circular dependencies and orphaned task references (D9) while still
    /// faithfully representing "planning must determine execution phases, dependency ordering,
    /// service startup order, infrastructure provisioning order, health verification order."
    /// </summary>
    private static IReadOnlyList<DeploymentExecutionPhase> BuildExecutionPhases(
        ServiceDeploymentBundle bundle,
        InfrastructureContract contract,
        IReadOnlyList<string> startupOrder)
    {
        string? previousTaskId = null;
        var phases = new List<DeploymentExecutionPhase>();

        DeploymentExecutionPhase Phase(DeploymentExecutionPhaseName name, IEnumerable<(string Id, string Description)> taskSeeds)
        {
            var tasks = new List<DeploymentTask>();
            foreach (var (id, description) in taskSeeds)
            {
                var taskId = $"{name.ToString().ToLowerInvariant()}.{id}";
                tasks.Add(new DeploymentTask
                {
                    TaskId = taskId,
                    Description = description,
                    DependsOnTaskIds = previousTaskId is null ? [] : [previousTaskId]
                });
                previousTaskId = taskId;
            }

            return new DeploymentExecutionPhase { Name = name, Order = (int)name, Tasks = tasks };
        }

        phases.Add(Phase(DeploymentExecutionPhaseName.Preflight,
        [
            ("validate-package-integrity", "Confirm the package passed admission's Package Integrity check."),
            ("validate-schema-compatibility", "Confirm the contract's schema version is current and supported."),
            ("validate-admission-approval", "Confirm the deployment was Approved by the Admission Engine before proceeding.")
        ]));

        var infrastructureTasks = new List<(string, string)> { ("create-filesystem-directories", "Create required writable and read-only filesystem directories.") };
        infrastructureTasks.AddRange(bundle.Certificate.Certificates.Where(c => c.Required)
            .Select(c => ($"provision-certificate-{c.Id}", $"Provision certificate '{c.Id}'.")));
        infrastructureTasks.AddRange(bundle.Secret.Secrets.Where(s => s.Required)
            .Select(s => ($"provision-secret-{s.Id}", $"Provision secret '{s.Id}'.")));
        infrastructureTasks.AddRange(contract.Requirements
            .Select(r => ($"satisfy-requirement-{r.Id}", $"Satisfy requirement '{r.Id}' ({r.Category}) against the environment's declared capabilities.")));
        phases.Add(Phase(DeploymentExecutionPhaseName.Infrastructure, infrastructureTasks));

        phases.Add(Phase(DeploymentExecutionPhaseName.Upload,
        [
            ("extract-release", "Extract the release package to the extraction folder."),
            ("stage-release", "Stage the extracted release and its compose definition.")
        ]));

        var configurationTasks = new List<(string, string)> { ("apply-required-settings", "Apply required configuration keys.") };
        configurationTasks.AddRange(bundle.Configuration.Required
            .Select(c => ($"apply-{c.Key.ToLowerInvariant().Replace(':', '-')}", $"Apply required configuration key '{c.Key}'.")));
        phases.Add(Phase(DeploymentExecutionPhaseName.Configuration, configurationTasks));

        var deploymentTasks = new List<(string, string)>
        {
            ("backup-current-release", "Back up the currently active release before replacement."),
        };
        deploymentTasks.AddRange(startupOrder.Select(serviceId => ($"start-{serviceId}", $"Start service '{serviceId}' in resolved startup order.")));
        deploymentTasks.Add(("activate-release", "Activate the staged release as current."));
        phases.Add(Phase(DeploymentExecutionPhaseName.Deployment, deploymentTasks));

        var healthTasks = new List<(string, string)>();
        if (bundle.Health.RequiresHttpProbe)
        {
            healthTasks.Add(("verify-primary-probe", $"Verify primary health probe at '{bundle.Health.HealthUrl}'."));
        }

        healthTasks.AddRange(bundle.Health.AdditionalProbes.Where(probe => probe.Required)
            .Select((probe, index) => ($"verify-additional-probe-{index}", $"Verify additional health probe '{probe.Method} {probe.Path}'.")));
        phases.Add(Phase(DeploymentExecutionPhaseName.Health, healthTasks));

        phases.Add(Phase(DeploymentExecutionPhaseName.Certification,
        [
            ("verify-acceptance-gates", "Run deployment acceptance certification gates."),
        ]));

        phases.Add(Phase(DeploymentExecutionPhaseName.Completion,
        [
            ("register-deployment", "Register the completed deployment in the audit trail."),
            ("finalize", "Mark the deployment complete.")
        ]));

        return phases;
    }

    /// <summary>D3/D5: one health gate per required probe the bundle actually declares.</summary>
    private static IReadOnlyList<HealthGate> BuildHealthGates(ServiceDeploymentBundle bundle)
    {
        var gates = new List<HealthGate>();
        if (bundle.Health.RequiresHttpProbe)
        {
            gates.Add(new HealthGate
            {
                Id = "primary-health-probe",
                Phase = DeploymentExecutionPhaseName.Health,
                Description = $"Primary HTTP health probe at '{bundle.Health.HealthUrl}' must return {bundle.Health.ExpectedStatus}."
            });
        }

        var index = 0;
        foreach (var probe in bundle.Health.AdditionalProbes.Where(probe => probe.Required))
        {
            gates.Add(new HealthGate
            {
                Id = $"additional-health-probe-{index}",
                Phase = DeploymentExecutionPhaseName.Health,
                Description = $"Additional probe '{probe.Method} {probe.Path}' must return {probe.ExpectedStatus}."
            });
            index++;
        }

        return gates;
    }

    /// <summary>
    /// D7: rollback checkpoints. Planning only identifies where a rollback would be safe to
    /// initiate — no rollback executes here. "Before database migration" is included only when
    /// the bundle's database contract is actually required, since a service with no database has
    /// nothing to migrate.
    /// </summary>
    private static IReadOnlyList<RollbackCheckpoint> BuildRollbackPoints(ServiceDeploymentBundle bundle)
    {
        var points = new List<RollbackCheckpoint>
        {
            new() { Id = "before-upload", BeforePhase = DeploymentExecutionPhaseName.Upload, Description = "Safe to abort before the release package is extracted and staged." },
            new() { Id = "before-infrastructure-changes", BeforePhase = DeploymentExecutionPhaseName.Infrastructure, Description = "Safe to abort before any filesystem, secret, or certificate provisioning occurs." },
            new() { Id = "before-service-replacement", BeforePhase = DeploymentExecutionPhaseName.Deployment, Description = "Safe to abort before the currently active release is replaced." }
        };

        if (bundle.Database.Required)
        {
            points.Add(new RollbackCheckpoint
            {
                Id = "before-database-migration",
                BeforePhase = DeploymentExecutionPhaseName.Deployment,
                Description = "Safe to abort before the database migration policy for this release executes."
            });
        }

        points.Add(new RollbackCheckpoint
        {
            Id = "before-certification",
            BeforePhase = DeploymentExecutionPhaseName.Certification,
            Description = "Safe to abort before certification gates run, while the previous release backup is still intact."
        });

        return points;
    }

    /// <summary>D3/D5: certification gates mirroring the existing deployment acceptance checks (docker, health, active profile, deployment audit).</summary>
    private static IReadOnlyList<CertificationGate> BuildCertificationGates() =>
    [
        new() { Id = "docker-provider-available", Phase = DeploymentExecutionPhaseName.Certification, Description = "Docker provider must be available." },
        new() { Id = "health-provider-available", Phase = DeploymentExecutionPhaseName.Certification, Description = "Health provider must be available." },
        new() { Id = "active-profile-valid", Phase = DeploymentExecutionPhaseName.Certification, Description = "Active configuration profile must validate successfully." },
        new() { Id = "deployment-acceptance-health-check", Phase = DeploymentExecutionPhaseName.Certification, Description = "Deployment acceptance health checks must pass before the deployment is considered certified." }
    ];

    /// <summary>D3/D7: rollback-to-previous is the only recovery strategy this platform's execution model supports today (a real execution engine redesign is out of this sprint's scope).</summary>
    private static RecoveryStrategy BuildRecoveryStrategy(IReadOnlyList<RollbackCheckpoint> rollbackPoints) => new()
    {
        Type = RecoveryStrategyType.RollbackToPrevious,
        Description = "On failure in Deployment, Health, or Certification, restore the previously active release from its backup.",
        TriggersOnPhaseFailure = rollbackPoints
            .Select(point => point.BeforePhase)
            .Distinct()
            .Where(phase => phase is DeploymentExecutionPhaseName.Deployment or DeploymentExecutionPhaseName.Health or DeploymentExecutionPhaseName.Certification)
            .ToList()
    };
}
