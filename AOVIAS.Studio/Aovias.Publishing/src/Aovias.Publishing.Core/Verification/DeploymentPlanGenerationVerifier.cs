using Aovias.Deployment.Contracts;
using Aovias.Deployment.Contracts.Planning;
using Aovias.Publishing.Contracts;
using Aovias.Publishing.Contracts.Verification;

namespace Aovias.Publishing.Core.Verification;

/// <summary>
/// Sprint M3.3 — proves that <see cref="DeploymentPlanner.Evaluate(ServiceDeploymentBundle, RuntimeCapabilities)"/> (M2.4) executes cleanly
/// against the contracts a package actually shipped with. This is a publish-time smoke check,
/// not a real target-capability negotiation: it evaluates against a capability-permissive
/// <see cref="RuntimeCapabilities"/> instance so the check exercises plan-generation mechanics
/// (topological ordering, contract validation, requirement matching wiring) without asserting
/// deployability against any specific environment. Real target-capability matching remains
/// exactly where it already lives — Runtime Agent, at actual deployment time — unchanged by
/// this sprint.
/// </summary>
public static class DeploymentPlanGenerationVerifier
{
    private static readonly RuntimeCapabilities PermissiveCapabilities = new()
    {
        Docker = true,
        SqlServer = true,
        Redis = true,
        RabbitMq = true,
        ServiceBus = true,
        Certificates = true,
        SignalR = true,
        Nginx = true
    };

    public static VerificationStageStatus Verify(DeployPackageContents contents)
    {
        var bundle = contents.DeploymentContracts;
        if (bundle is null)
        {
            return VerificationStageStatus.Skipped(
                "Package carries no deployment contracts (legacy-synthesized package); deployment plan generation not applicable.");
        }

        try
        {
            var evaluation = DeploymentPlanner.Evaluate(bundle, PermissiveCapabilities);
            if (evaluation.Blockers.Count > 0)
            {
                return VerificationStageStatus.Failed(
                    "Deployment plan generation produced blockers even under permissive capabilities.",
                    string.Join("; ", evaluation.Blockers),
                    VerificationEvidenceTier.RuntimeVerification,
                    evaluation.Blockers);
            }

            return VerificationStageStatus.Passed(
                $"Deployment plan generated for {evaluation.ServiceId} v{evaluation.Version} "
                + $"({evaluation.StartupOrder.Count} startup-order entries, CanDeploy={evaluation.CanDeploy}).",
                VerificationEvidenceTier.RuntimeVerification,
                evaluation.Warnings);
        }
        catch (Exception exception)
        {
            return VerificationStageStatus.Failed(
                "Deployment plan generation threw an exception.",
                exception.Message,
                VerificationEvidenceTier.RuntimeVerification);
        }
    }
}
