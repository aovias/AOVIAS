using Aovias.Deployment.Contracts;
using Aovias.Deployment.Contracts.Admission;
using Aovias.Deployment.Contracts.Infrastructure;
using Aovias.Deployment.Contracts.Migration;
using Aovias.Deployment.Contracts.Planning;
using Aovias.Publishing.Contracts.Verification;

namespace Aovias.Publishing.Core.Verification;

/// <summary>
/// Sprint M3.4 — Deployment Certification &amp; Baseline Validation.
///
/// Builds one representative <see cref="DeploymentPlan"/> (via the existing M2.4
/// <see cref="DeploymentPlanBuilder"/> — no new planning logic) from a real, already-published
/// <see cref="ServiceDeploymentBundle"/>, and derives the deploy-time lifecycle readiness checks
/// from its declared sections: <see cref="DeploymentPlan.HealthGates"/>,
/// <see cref="DeploymentPlan.CertificationGates"/>, <see cref="DeploymentPlan.RollbackPoints"/>.
/// This proves the platform's planning mechanics produce a structurally complete plan; it does
/// not — and cannot, without a live SDK/target — prove a real deployment, health probe, or
/// rollback actually executes. Every method here is explicit about which half it covers.
/// </summary>
public static class DeploymentLifecycleReadinessVerifier
{
    /// <summary>
    /// Builds a representative plan for structural verification only. Uses a minimal,
    /// zero-requirement <see cref="InfrastructureContract"/> and a pre-approved
    /// <see cref="AdmissionResult"/> — this sprint verifies planning mechanics (are all eight
    /// fixed phases populated, are gates/rollback points declared), not real admission or
    /// requirement-capability matching, both of which are already covered elsewhere
    /// (Sprint M2.3/M3.3).
    /// </summary>
    public static DeploymentPlan? BuildRepresentativePlan(ServiceDeploymentBundle bundle, out string? failureReason)
    {
        try
        {
            var contract = new InfrastructureContract
            {
                ServiceId = bundle.Manifest.ServiceId,
                SchemaVersion = ContractSchemaVersions.Current,
                ContractVersion = ContractSchemaVersions.ContractVersionV2,
                Compatibility = new ContractCompatibilityMetadata
                {
                    MinimumSupportedSchemaVersion = ContractSchemaVersions.V1,
                    MaximumSupportedSchemaVersion = ContractSchemaVersions.Current
                },
                Integrity = new ContractIntegrityMetadata { ContentHash = "m3.4-verification-smoke-check" },
                Requirements = []
            };

            var admission = new AdmissionResult
            {
                Decision = AdmissionDecision.Approved,
                CorrelationId = $"m3.4-verify-{bundle.Manifest.ServiceId}"
            };

            var plan = new DeploymentPlanBuilder().CreatePlan(new DeploymentPlanRequest
            {
                Bundle = bundle,
                Contract = contract,
                Admission = admission,
                EnvironmentId = "verification",
                AvailableCapabilities = [],
                GeneratedUtc = DateTimeOffset.UtcNow
            });

            failureReason = null;
            return plan;
        }
        catch (Exception exception)
        {
            failureReason = exception.Message;
            return null;
        }
    }

    public static VerificationStageStatus VerifyHealthGateReadiness(DeploymentPlan? plan, string? buildFailureReason)
    {
        if (plan is null)
        {
            return VerificationStageStatus.Failed(
                "Could not build a representative deployment plan to check health gates.",
                buildFailureReason ?? "Unknown plan-build failure.",
                VerificationEvidenceTier.RuntimeVerification);
        }

        if (plan.HealthGates.Count == 0)
        {
            return VerificationStageStatus.Failed(
                "Representative deployment plan declares no health gates.",
                "DeploymentPlan.HealthGates was empty.",
                VerificationEvidenceTier.RuntimeVerification);
        }

        return VerificationStageStatus.Passed(
            $"Deployment plan declares {plan.HealthGates.Count} health gate(s). "
            + "Whether a live probe against a deployed target actually passes requires runtime execution.",
            VerificationEvidenceTier.RuntimeVerification);
    }

    public static VerificationStageStatus VerifyDeploymentCertificationReadiness(DeploymentPlan? plan, string? buildFailureReason)
    {
        if (plan is null)
        {
            return VerificationStageStatus.Failed(
                "Could not build a representative deployment plan to check certification gates.",
                buildFailureReason ?? "Unknown plan-build failure.",
                VerificationEvidenceTier.RuntimeVerification);
        }

        if (plan.CertificationGates.Count == 0)
        {
            return VerificationStageStatus.Failed(
                "Representative deployment plan declares no certification gates.",
                "DeploymentPlan.CertificationGates was empty.",
                VerificationEvidenceTier.RuntimeVerification);
        }

        return VerificationStageStatus.Passed(
            $"Deployment plan declares {plan.CertificationGates.Count} certification gate(s) "
            + $"across {plan.ExecutionPhases.Count} execution phases. Whether these gates actually "
            + "pass against a live deployment requires runtime execution.",
            VerificationEvidenceTier.RuntimeVerification);
    }

    public static VerificationStageStatus VerifyRollbackReadiness(DeploymentPlan? plan, string? buildFailureReason)
    {
        if (plan is null)
        {
            return VerificationStageStatus.Failed(
                "Could not build a representative deployment plan to check rollback readiness.",
                buildFailureReason ?? "Unknown plan-build failure.",
                VerificationEvidenceTier.RuntimeVerification);
        }

        if (plan.RollbackPoints.Count == 0)
        {
            return VerificationStageStatus.Failed(
                "Representative deployment plan declares no rollback checkpoints.",
                "DeploymentPlan.RollbackPoints was empty.",
                VerificationEvidenceTier.RuntimeVerification);
        }

        return VerificationStageStatus.Passed(
            $"Deployment plan declares {plan.RollbackPoints.Count} rollback checkpoint(s) and a "
            + $"'{plan.Recovery.Type}' recovery strategy. Whether a rollback actually executes "
            + "cleanly requires runtime execution against a real deployment.",
            VerificationEvidenceTier.RuntimeVerification);
    }

    /// <summary>
    /// Source-text verification that Runtime Agent's own DI wiring registers the reconciliation
    /// scheduler and the health/certification execution provisioners. This reads the checked-out
    /// repository (a CI/build-time concern for this pipeline), not a deployed artifact — the same
    /// class of check Sprint M3.2 already used for solution-membership consistency. It proves the
    /// wiring exists; it cannot prove the hosted service actually starts and ticks without a live
    /// host.
    /// </summary>
    public static VerificationStageStatus VerifyReconciliationInitializationWiring(string platformRoot)
    {
        var diPath = Path.Combine(
            platformRoot, "Aovias.Runtime.Agent", "src", "Aovias.Runtime.Agent.Infrastructure", "DependencyInjection.cs");

        if (!File.Exists(diPath))
        {
            return VerificationStageStatus.Failed(
                "Runtime Agent Infrastructure DI file not found.",
                $"Expected '{diPath}'.",
                VerificationEvidenceTier.StaticVerification);
        }

        var source = File.ReadAllText(diPath);
        var missing = new List<string>();

        if (!source.Contains("AddHostedService<ReconciliationSchedulerHostedService>", StringComparison.Ordinal))
        {
            missing.Add("ReconciliationSchedulerHostedService is not registered as a hosted service.");
        }

        if (!source.Contains("IReconciliationScheduler, ReconciliationScheduler", StringComparison.Ordinal))
        {
            missing.Add("IReconciliationScheduler is not registered.");
        }

        return missing.Count == 0
            ? VerificationStageStatus.Passed(
                "Reconciliation scheduler is registered as a hosted service in Runtime Agent's DI wiring. "
                + "Whether it actually starts and ticks on a live host requires runtime execution.",
                VerificationEvidenceTier.StaticVerification)
            : VerificationStageStatus.Failed(
                "Reconciliation initialization wiring is incomplete.",
                string.Join("; ", missing),
                VerificationEvidenceTier.StaticVerification,
                missing);
    }

    /// <summary>
    /// Source-text verification that the health/certification execution provisioners are wired
    /// into Runtime Agent's deployment execution registry, and that Runtime Agent's own startup
    /// sequence has structured bootstrap diagnostics. Same evidence class as
    /// <see cref="VerifyReconciliationInitializationWiring"/> — proves wiring exists, not that a
    /// live host starts successfully.
    /// </summary>
    public static VerificationStageStatus VerifyRuntimeStartupWiring(string platformRoot)
    {
        var programPath = Path.Combine(
            platformRoot, "Aovias.Runtime.Agent", "src", "Aovias.Runtime.Agent.Api", "Program.cs");
        var providerPath = Path.Combine(
            platformRoot, "Aovias.Runtime.Agent", "src", "Aovias.Runtime.Agent.Infrastructure", "Deployment", "ReleaseDeploymentProvider.cs");

        var missing = new List<string>();

        if (!File.Exists(programPath))
        {
            missing.Add($"Runtime Agent entry point not found at '{programPath}'.");
        }
        else
        {
            var program = File.ReadAllText(programPath);
            if (!program.Contains("RuntimeBootstrapReport", StringComparison.Ordinal))
            {
                missing.Add("Program.cs does not use RuntimeBootstrapReport for structured startup diagnostics.");
            }

            if (!program.Contains("app.StartAsync()", StringComparison.Ordinal))
            {
                missing.Add("Program.cs does not call app.StartAsync() (expected explicit start before WaitForShutdownAsync).");
            }
        }

        if (!File.Exists(providerPath))
        {
            missing.Add($"Deployment execution provider not found at '{providerPath}'.");
        }
        else
        {
            var provider = File.ReadAllText(providerPath);
            if (!provider.Contains("new HealthGateTaskProvisioner(", StringComparison.Ordinal))
            {
                missing.Add("HealthGateTaskProvisioner is not wired into the deployment execution registry.");
            }

            if (!provider.Contains("new CertificationTaskProvisioner(", StringComparison.Ordinal))
            {
                missing.Add("CertificationTaskProvisioner is not wired into the deployment execution registry.");
            }
        }

        return missing.Count == 0
            ? VerificationStageStatus.Passed(
                "Runtime Agent startup diagnostics and execution-registry wiring are present. "
                + "Whether the host actually starts and remains healthy requires runtime execution "
                + "(dotnet run / a live SDK).",
                VerificationEvidenceTier.StaticVerification)
            : VerificationStageStatus.Failed(
                "Runtime startup wiring is incomplete.",
                string.Join("; ", missing),
                VerificationEvidenceTier.StaticVerification,
                missing);
    }
}
