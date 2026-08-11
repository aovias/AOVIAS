using Aovias.Deployment.Contracts.Capabilities;
using Aovias.Deployment.Contracts.Planning;
using Aovias.Deployment.Contracts.Validation;

namespace Aovias.Deployment.Contracts.Infrastructure.Provisioners;

/// <summary>
/// Sprint M2.3 D8: this is the one shared pipeline stage both DRV and Runtime Agent execute
/// (see <see cref="InfrastructureProvisioningPipeline"/>), so it is the seam through which
/// requirement-capability matching reaches both consumers with zero changes to their own source.
/// Neither ever evaluates <see cref="InfrastructureContract.Requirements"/> itself; this stage
/// calls <see cref="DeploymentPlanner"/> and fails on <see cref="Planning.DeploymentPlanEvaluation.CanDeploy"/>
/// being <see langword="false"/>.
/// </summary>
public sealed class DependencyInfrastructureProvisioner : IInfrastructureProvisioner
{
    public string StageName => "Dependencies";

    public Task<InfrastructureStageResult> ProvisionAsync(
        InfrastructureProvisioningContext context,
        CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        var dependencies = context.Contract.Dependencies.Dependencies.Where(item => item.Required).ToList();

        // Sprint M2.3 D8: available capabilities come from the context when the caller has
        // populated them explicitly; otherwise they are bridged from the legacy RuntimeCapabilities
        // booleans so requirement matching still runs for every existing DRV/Runtime Agent caller.
        var availableCapabilities = context.AvailableCapabilities.Count > 0
            ? context.AvailableCapabilities
            : LegacyCapabilityBridge.FromRuntimeCapabilities(context.RuntimeCapabilities);

        var plan = DeploymentPlanner.Evaluate(
            context.Bundle,
            context.RuntimeCapabilities,
            context.Contract.Requirements,
            availableCapabilities);

        if (dependencies.Count == 0 && context.Contract.Requirements.Count == 0)
        {
            return Task.FromResult(InfrastructureStageResult.Skipped(StageName, "No service dependencies or infrastructure requirements declared."));
        }

        if (!plan.CanDeploy)
        {
            return Task.FromResult(InfrastructureStageResult.Failed(StageName, plan.Blockers.ToList()));
        }

        var order = string.Join(" → ", plan.StartupOrder);
        var messages = new List<string> { $"Startup order resolved: {order}." };
        messages.AddRange(dependencies.Select(item => $"Requires {item.Type} '{item.Service}'."));
        if (plan.RequirementMatch is not null)
        {
            messages.AddRange(plan.RequirementMatch.Results
                .Where(result => result.Status == RequirementMatchStatus.Satisfied)
                .Select(result => result.Message));
        }

        return Task.FromResult(InfrastructureStageResult.Ready(StageName, messages.ToArray()));
    }
}

public sealed class ValidationInfrastructureProvisioner : IInfrastructureProvisioner
{
    public string StageName => "Validation";

    public Task<InfrastructureStageResult> ProvisionAsync(
        InfrastructureProvisioningContext context,
        CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        var report = ContractValidator.Validate(context.Bundle);
        if (!report.Passed)
        {
            var errors = report.Results
                .Where(item => !item.Passed)
                .SelectMany(item => item.Errors.Select(error => $"{item.Name}: {error}"))
                .ToList();
            return Task.FromResult(InfrastructureStageResult.Failed(StageName, errors));
        }

        return Task.FromResult(InfrastructureStageResult.Ready(
            StageName,
            ["Deployment contracts validated."]));
    }
}
