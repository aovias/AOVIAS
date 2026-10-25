using System.Diagnostics;
using Aovias.Deployment.Contracts;
using Aovias.Deployment.Contracts.Reading;
using Aovias.Deployment.Contracts.Validation;
using Aovias.DeploymentValidation.Contracts;
using Aovias.DeploymentValidation.Models;
using Aovias.Publishing.Contracts;

namespace Aovias.DeploymentValidation.Validators;

/// <summary>
/// Validates deployment contracts from the package manifest and contract files.
/// </summary>
public sealed class ContractValidationStage : IDeploymentValidationStage
{
    private readonly IDeploymentPackageContractReader contractReader;
    public ContractValidationStage(IDeploymentPackageContractReader contractReader) => this.contractReader = contractReader;
    public string StageName => "Contracts";

    public ValidationStageDescriptor Descriptor { get; } = new(
        ValidationCategory.Deployment,
        ValidationExecutionScope.Publishing);

    public Task<ValidationStageResult> ValidateAsync(
        DeploymentValidationContext context,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var warnings = new List<string>();
        var errors = new List<string>();

        var contents = context.PackageContents;
        if (contents is null)
        {
            return Task.FromResult(ValidationStageResult.Fail(StageName, stopwatch.Elapsed, ["Package contents are unavailable."]));
        }

        try
        {
            context.InfrastructureContract = contractReader.ReadFromPackage(context.PackagePath);
        }
        catch (DeploymentPackageContractException exception)
        {
            return Task.FromResult(ValidationStageResult.Fail(StageName, stopwatch.Elapsed, [exception.Message]));
        }

        var bundle = context.ResolveDeploymentBundle();
        if (bundle is null)
        {
            return Task.FromResult(ValidationStageResult.Fail(StageName, stopwatch.Elapsed, ["Unable to resolve deployment contracts for the package."]));
        }

        if (bundle.IsLegacySynthesized)
        {
            warnings.Add("Deployment contracts were synthesized from legacy package metadata.");
        }

        var report = ContractValidator.Validate(bundle);
        context.ContractValidationReport = report;

        foreach (var result in report.Results.Where(item => !item.Passed))
        {
            foreach (var error in result.Errors)
            {
                errors.Add($"{result.Name}: {error}");
            }
        }

        stopwatch.Stop();
        return Task.FromResult(errors.Count == 0
            ? ValidationStageResult.Pass(
                StageName,
                stopwatch.Elapsed,
                warnings,
                recommendations:
                [
                    $"Filesystem directories: {bundle.Filesystem.Directories.Count + bundle.Filesystem.ReadOnlyDirectories.Count}",
                    $"Required secrets: {bundle.Secret.Secrets.Count(item => item.Required)}",
                    $"Required certificates: {bundle.Certificate.Certificates.Count(item => item.Required)}",
                    $"Dependencies: {bundle.Dependency.Dependencies.Count}"
                ])
            : ValidationStageResult.Fail(StageName, stopwatch.Elapsed, errors, warnings));
    }
}

public static class DeploymentValidationContractResolver
{
    public static ServiceDeploymentBundle? ResolveDeploymentBundle(this DeploymentValidationContext context)
    {
        if (context.DeploymentContracts is not null)
        {
            return context.DeploymentContracts;
        }

        if (context.PackageContents?.DeploymentContracts is not null)
        {
            context.DeploymentContracts = context.PackageContents.DeploymentContracts;
            return context.DeploymentContracts;
        }

        if (context.PackageContents is null)
        {
            return null;
        }

        var contents = context.PackageContents;
        context.DeploymentContracts = DeploymentPackageContractsResolver.ResolveFromDirectory(
            context.ExtractedPackageDirectory ?? string.Empty,
            new LegacyPackageMetadata
            {
                ServiceId = contents.Manifest.Service,
                DisplayName = contents.Manifest.Service,
                Version = contents.Manifest.Version,
                ImageName = contents.Manifest.ImageName,
                ImageTag = contents.Manifest.ImageTag,
                ContainerName = contents.Manifest.Container,
                EntryPoint = contents.Manifest.EntryPoint,
                HealthEndpoint = contents.Health.Endpoint,
                Ports = contents.Manifest.Ports,
                Dependencies = contents.Manifest.Dependencies.ToList(),
                HealthTimeoutSeconds = contents.Health.TimeoutSeconds,
                HealthRetries = contents.Health.Retries,
                ConfigurationKeys = contents.ConfigurationTemplate.Keys.ToList()
            });

        return context.DeploymentContracts;
    }

    public static HealthContract ResolveHealthContract(this DeploymentValidationContext context)
    {
        var bundle = context.ResolveDeploymentBundle();
        if (bundle?.Health is not null)
        {
            return bundle.Health;
        }

        var health = context.PackageContents?.Health;
        return new HealthContract
        {
            HealthUrl = health?.Endpoint ?? "/health",
            ExpectedStatus = health?.ExpectedStatus ?? 200,
            TimeoutSeconds = health?.TimeoutSeconds ?? 15,
            Retries = health?.Retries ?? 3,
            StabilizationSeconds = health?.TimeoutSeconds ?? 15
        };
    }
}
