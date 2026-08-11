using System.Security.Cryptography;
using System.Text;
using Aovias.Deployment.Contracts.Serialization;

namespace Aovias.Deployment.Contracts.Planning;

/// <summary>
/// Computes <see cref="PlanDiagnostics.ContentHash"/> (Sprint M2.4 D10): a hash of the plan's
/// deterministic content only, deliberately excluding <see cref="DeploymentPlanMetadata.GeneratedUtc"/>
/// and <see cref="DeploymentPlanMetadata.CorrelationId"/> — the same discipline
/// <see cref="Migration.InfrastructureContractIntegrity"/> established in Sprint M2.2 for exactly
/// the same reason: two plans built from the same admission-approved inputs at two different
/// wall-clock moments (or under two different correlation ids) must still prove they are the
/// same plan, which is what the "deterministic planning" acceptance criterion actually requires.
/// </summary>
public static class DeploymentPlanIntegrity
{
    public const string Algorithm = "SHA256";

    public static string ComputeContentHash(DeploymentPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        var snapshot = new PlanContentSnapshot
        {
            ServiceId = plan.Metadata.ServiceId,
            ServiceVersion = plan.Metadata.ServiceVersion,
            SchemaVersion = plan.Metadata.SchemaVersion,
            Environment = plan.Environment,
            Services = plan.Services,
            Infrastructure = plan.Infrastructure,
            Dependencies = plan.Dependencies,
            ExecutionPhases = plan.ExecutionPhases,
            HealthGates = plan.HealthGates,
            RollbackPoints = plan.RollbackPoints,
            CertificationGates = plan.CertificationGates,
            Recovery = plan.Recovery
        };

        var json = DeploymentContractJson.Serialize(snapshot);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json))).ToLowerInvariant();
    }

    private sealed class PlanContentSnapshot
    {
        public required string ServiceId { get; init; }

        public required string ServiceVersion { get; init; }

        public required int SchemaVersion { get; init; }

        public required DeploymentPlanEnvironment Environment { get; init; }

        public required IReadOnlyList<DeploymentPlanServiceEntry> Services { get; init; }

        public required DeploymentPlanInfrastructure Infrastructure { get; init; }

        public required IReadOnlyList<string> Dependencies { get; init; }

        public required IReadOnlyList<DeploymentExecutionPhase> ExecutionPhases { get; init; }

        public required IReadOnlyList<HealthGate> HealthGates { get; init; }

        public required IReadOnlyList<RollbackCheckpoint> RollbackPoints { get; init; }

        public required IReadOnlyList<CertificationGate> CertificationGates { get; init; }

        public required RecoveryStrategy Recovery { get; init; }
    }
}
