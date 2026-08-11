using Aovias.Configuration.Settings;
using Aovias.Deployment.Contracts.Reconciliation;
using Aovias.Orchestration.Contracts.Providers;
using Microsoft.Extensions.Options;

namespace Aovias.Runtime.Agent.Infrastructure.Reconciliation;

/// <summary>
/// The one real <see cref="IReconciliationTargetProvider"/> (Sprint M2.6 D8), reading from
/// <see cref="LastDeploymentPlanStore"/> (D1's authoritative source: the last successfully
/// executed Deployment Plan per service — never anything observed) and composing a default,
/// environment-aware <see cref="RepairPolicySet"/> plus a <see cref="SafetyContext"/> factory
/// backed by the real providers already used elsewhere in Runtime Agent (Sprint M2.5's
/// <c>RuntimeGateCheck</c> established the same "compose real providers at this seam" pattern).
/// </summary>
public sealed class RuntimeReconciliationTargetProvider : IReconciliationTargetProvider
{
    private readonly LastDeploymentPlanStore planStore;
    private readonly IDockerProvider dockerProvider;
    private readonly IOptionsMonitor<RuntimeSettings> runtimeSettings;
    private readonly IOptionsMonitor<ProviderSettings> providerSettings;
    private readonly MaintenanceModeStore maintenanceModeStore;

    public RuntimeReconciliationTargetProvider(
        LastDeploymentPlanStore planStore,
        IDockerProvider dockerProvider,
        IOptionsMonitor<RuntimeSettings> runtimeSettings,
        IOptionsMonitor<ProviderSettings> providerSettings,
        MaintenanceModeStore maintenanceModeStore)
    {
        this.planStore = planStore;
        this.dockerProvider = dockerProvider;
        this.runtimeSettings = runtimeSettings;
        this.providerSettings = providerSettings;
        this.maintenanceModeStore = maintenanceModeStore;
    }

    public IReadOnlyList<ReconciliationTarget> GetCurrentTargets()
    {
        var policySet = BuildDefaultPolicySet();

        return planStore.ServiceIds
            .Select(planStore.TryGet)
            .Where(record => record is not null)
            .Select(record => new ReconciliationTarget
            {
                Plan = record!.Plan,
                Contract = record.Contract,
                Manifest = record.Manifest,
                PolicySet = policySet,
                ResolvedNetworkName = record.ResolvedNetworkName,
                SafetyContextFactory = task => new SafetyContext
                {
                    CapabilityAvailable = ResolveCapabilityAvailable(task),
                    // The plan stored in LastDeploymentPlanStore is, by construction, the plan
                    // this environment's most recent successful execution used — so it is always
                    // "current" relative to itself. A future sprint that lets an operator approve
                    // a newer plan without redeploying would need this to compare against a
                    // separately-tracked "latest approved plan id" (Sprint M2.6 Technical Debt).
                    DeploymentPlanIsCurrent = true,
                    // Package integrity was already verified by the Admission Engine (Sprint
                    // M2.4) before this plan was ever produced — a plan reaching
                    // LastDeploymentPlanStore has definitionally passed that gate.
                    PackageIntegrityVerified = true,
                    // Sprint M2.7: consult the one in-memory MaintenanceModeStore so an operator
                    // (typically via the Command Center) can suspend automatic repair for this
                    // environment without touching the Reconciliation Planner or Safety Framework
                    // itself — see MaintenanceModeStore's doc comment for the full rationale.
                    MaintenanceModeActive = maintenanceModeStore.GetStatus().Active
                }
            })
            .ToList();
    }

    private bool ResolveCapabilityAvailable(RepairTask task) => task.ActionType switch
    {
        RepairActionType.RecreateDockerNetwork or RepairActionType.RestartContainer => dockerProvider.IsAvailable,
        RepairActionType.RecreateDirectory or RepairActionType.RestorePermissions or RepairActionType.RecreateSymbolicLink => providerSettings.CurrentValue.FileSystemEnabled,
        RepairActionType.ReloadConfiguration => true,
        _ => true
    };

    private RepairPolicySet BuildDefaultPolicySet()
    {
        var environment = ResolveEnvironmentClass(runtimeSettings.CurrentValue.Environment);

        return new RepairPolicySet
        {
            Environment = environment,
            DefaultPolicy = RepairPolicyType.AskOperator,
            Rules =
            [
                // Low-risk, brief-mandated "allowed automatically" actions repair themselves in
                // every environment without waiting on an operator.
                new RepairPolicyRule { ActionType = RepairActionType.RecreateDockerNetwork, PolicyType = RepairPolicyType.AlwaysRepair },
                new RepairPolicyRule { ActionType = RepairActionType.RecreateDirectory, PolicyType = RepairPolicyType.AlwaysRepair },
                new RepairPolicyRule { ActionType = RepairActionType.RestartContainer, PolicyType = RepairPolicyType.AlwaysRepair },
                new RepairPolicyRule { ActionType = RepairActionType.RestorePermissions, PolicyType = RepairPolicyType.AlwaysRepair },
                new RepairPolicyRule { ActionType = RepairActionType.RecreateSymbolicLink, PolicyType = RepairPolicyType.AlwaysRepair },

                // Configuration Drift is explicitly "Policy Based / Policy Based" in the brief's
                // own Drift Detection Matrix example — Production requires an operator; every
                // other environment may reload automatically.
                new RepairPolicyRule { ActionType = RepairActionType.ReloadConfiguration, Severity = DriftSeverity.Warning, PolicyType = RepairPolicyType.ProductionOnly },

                // The brief's fixed "require operator approval" list — restated as explicit rules
                // (rather than relying solely on the matrix default) so this policy set is
                // self-documenting about environment-independent, always-unsafe actions.
                new RepairPolicyRule { ActionType = RepairActionType.DeleteVolume, PolicyType = RepairPolicyType.NeverRepair },
                new RepairPolicyRule { ActionType = RepairActionType.DatabaseChange, PolicyType = RepairPolicyType.NeverRepair },
                new RepairPolicyRule { ActionType = RepairActionType.CertificateReplacement, PolicyType = RepairPolicyType.NeverRepair },
                new RepairPolicyRule { ActionType = RepairActionType.SecretReplacement, PolicyType = RepairPolicyType.NeverRepair },
                new RepairPolicyRule { ActionType = RepairActionType.ImageDowngrade, PolicyType = RepairPolicyType.NeverRepair },
                new RepairPolicyRule { ActionType = RepairActionType.RollbackDeployment, PolicyType = RepairPolicyType.NeverRepair }
            ]
        };
    }

    /// <summary>
    /// Maps Runtime Agent's free-text <c>RuntimeSettings.Environment</c> onto <see cref="EnvironmentClass"/>
    /// by substring match — this codebase has no strongly-typed environment enum available to
    /// <c>Aovias.Deployment.Contracts</c> without adding a new project reference (see
    /// <see cref="EnvironmentClass"/>'s remarks), so this deliberately simple mapping is the seam
    /// where Runtime Agent's own environment concept meets the reconciliation policy model.
    /// </summary>
    private static EnvironmentClass ResolveEnvironmentClass(string? environmentId)
    {
        if (string.IsNullOrWhiteSpace(environmentId))
        {
            return EnvironmentClass.Unknown;
        }

        return environmentId.ToLowerInvariant() switch
        {
            var id when id.Contains("prod") => EnvironmentClass.Production,
            var id when id.Contains("demo") => EnvironmentClass.Demo,
            var id when id.Contains("qa") => EnvironmentClass.Qa,
            var id when id.Contains("uat") => EnvironmentClass.Uat,
            var id when id.Contains("dev") || id.Contains("local") => EnvironmentClass.Development,
            _ => EnvironmentClass.Unknown
        };
    }
}
