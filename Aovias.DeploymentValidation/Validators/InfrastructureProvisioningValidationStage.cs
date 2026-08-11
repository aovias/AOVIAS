using System.Diagnostics;
using Aovias.Deployment.Contracts.Infrastructure;
using Aovias.Deployment.Contracts.Reading;
using Aovias.Deployment.Contracts.Provisioning;
using Aovias.DeploymentValidation.Contracts;
using Aovias.DeploymentValidation.Models;
using Aovias.Publishing.Contracts;

namespace Aovias.DeploymentValidation.Validators;

/// <summary>
/// Simulates infrastructure provisioning and produces a readiness report before container startup.
/// </summary>
public sealed class InfrastructureProvisioningValidationStage : IDeploymentValidationStage
{
    private readonly IDeploymentPackageContractReader contractReader;
    public InfrastructureProvisioningValidationStage(IDeploymentPackageContractReader contractReader) => this.contractReader = contractReader;
    public string StageName => "Infrastructure";

    public ValidationStageDescriptor Descriptor { get; } = new(
        ValidationCategory.Infrastructure,
        ValidationExecutionScope.Deployment);

    public async Task<ValidationStageResult> ValidateAsync(
        DeploymentValidationContext context,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var bundle = context.ResolveDeploymentBundle();
        if (bundle is null)
        {
            return ValidationStageResult.Fail(StageName, stopwatch.Elapsed, ["Unable to resolve deployment contracts."]);
        }

        InfrastructureContract contract;
        try
        {
            contract = context.InfrastructureContract ?? contractReader.ReadFromPackage(context.PackagePath);
            context.InfrastructureContract = contract;
        }
        catch (DeploymentPackageContractException exception)
        {
            return ValidationStageResult.Fail(StageName, stopwatch.Elapsed, [exception.Message]);
        }

        var configRoot = context.Workspace?.ValidationConfigRoot ?? context.ValidationConfigRoot;
        if (string.IsNullOrWhiteSpace(configRoot) || !Directory.Exists(configRoot))
        {
            return ValidationStageResult.Fail(StageName, stopwatch.Elapsed, ["Validation config root is unavailable."]);
        }

        var hostPaths = new HostPathContext
        {
            HostConfigRoot = configRoot,
            HostLogsRoot = Path.Combine(configRoot, "logs"),
            HostPackagesRoot = Path.Combine(configRoot, "packages"),
            HostDeploymentsRoot = Path.Combine(configRoot, "deployments")
        };

        var provisioningContext = new InfrastructureProvisioningContext
        {
            Bundle = bundle,
            Contract = contract,
            HostPaths = hostPaths,
            RuntimeCapabilities = bundle.RuntimeCapabilities
        };

        var readiness = await InfrastructureProvisioningPipeline.ExecuteAsync(provisioningContext, cancellationToken);
        context.InfrastructureReadinessReport = readiness;

        stopwatch.Stop();
        if (!readiness.DeploymentAllowed)
        {
            var recovery = InfrastructureRecoveryCoordinator.DescribeRecoveryActions(readiness);
            var errors = readiness.Blockers.ToList();
            errors.AddRange(recovery.Select(action => $"Recovery: {action}"));
            return ValidationStageResult.Fail(StageName, stopwatch.Elapsed, errors);
        }

        var recommendations = readiness.Stages
            .Select(stage => $"{stage.Stage}: {stage.State}")
            .ToList();

        return ValidationStageResult.Pass(
            StageName,
            stopwatch.Elapsed,
            recommendations: recommendations);
    }
}
