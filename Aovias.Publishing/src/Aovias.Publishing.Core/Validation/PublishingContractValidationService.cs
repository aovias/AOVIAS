using Aovias.Deployment.Contracts.Validation;
using Aovias.Publishing.Contracts;
using Aovias.Publishing.Core.Discovery;

namespace Aovias.Publishing.Core.Validation;

/// <summary>
/// Validates deployment metadata and generated contracts before packaging.
/// </summary>
public static class PublishingContractValidationService
{
    public sealed class ValidationResult
    {
        public required bool Passed { get; init; }

        public IReadOnlyList<string> Errors { get; init; } = [];
    }

    public static ValidationResult Validate(PublishingContext context)
    {
        var errors = new List<string>();

        if (context.DeploymentMetadata is null)
        {
            errors.Add("Deployment metadata is required.");
            return Failed(errors);
        }

        errors.AddRange(ValidateMetadata(context.DeploymentMetadata));

        if (context.Manifest is not null)
        {
            errors.AddRange(ValidateManifestConsistency(context.DeploymentMetadata, context.Manifest));
        }

        if (context.Health is not null)
        {
            errors.AddRange(ValidateHealthConsistency(context.DeploymentMetadata, context.Health));
        }

        if (context.DeploymentContracts is not null)
        {
            var contractReport = ContractValidator.Validate(context.DeploymentContracts);
            if (!contractReport.Passed)
            {
                errors.AddRange(contractReport.Results
                    .Where(result => !result.Passed)
                    .SelectMany(result => result.Errors.Select(error => $"{result.Name}: {error}")));
            }
        }

        return errors.Count == 0 ? Passed() : Failed(errors);
    }

    public static ValidationResult ValidateSolutionGraph(IReadOnlyList<DeployableProject> projects)
    {
        var errors = new List<string>();
        var duplicateIds = projects
            .GroupBy(project => project.ServiceId, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();

        foreach (var duplicateId in duplicateIds)
        {
            errors.Add($"Duplicate service identifier '{duplicateId}' discovered in solution graph.");
        }

        var graph = PublishingDependencyGraphBuilder.BuildGraph(
            projects.Select(project => (project.ServiceId, project.DependsOn)));
        var knownServices = projects.Select(project => project.ServiceId).ToHashSet(StringComparer.OrdinalIgnoreCase);

        errors.AddRange(PublishingDependencyGraphBuilder.DetectMissingDependencies(graph, knownServices));
        errors.AddRange(PublishingDependencyGraphBuilder.DetectDuplicatePorts(
            projects.Select(project => (project.ServiceId, project.ContainerPort))));

        try
        {
            PublishingDependencyGraphBuilder.DetectCircularDependencies(graph);
            PublishingDependencyGraphBuilder.TopologicalSort(graph);
        }
        catch (InvalidOperationException exception)
        {
            errors.Add(exception.Message);
        }

        return errors.Count == 0 ? Passed() : Failed(errors);
    }

    private static IEnumerable<string> ValidateMetadata(DeploymentMetadata metadata)
    {
        if (metadata.SchemaVersion <= 0)
        {
            yield return "deployment.metadata.json schemaVersion must be positive.";
        }

        if (string.IsNullOrWhiteSpace(metadata.ServiceId))
        {
            yield return "deployment.metadata.json serviceId is required.";
        }

        if (metadata.Ports.ContainerPort is < 1 or > 65535)
        {
            yield return $"Invalid container port '{metadata.Ports.ContainerPort}'.";
        }

        if (metadata.Ports.HostPort is < 1 or > 65535)
        {
            yield return $"Invalid host port '{metadata.Ports.HostPort}'.";
        }

        if (metadata.Health.TimeoutSeconds <= 0 || metadata.Health.Retries <= 0)
        {
            yield return "Health timeoutSeconds and retries must be greater than zero.";
        }

        if (metadata.Health.RequiresHttpProbe && string.IsNullOrWhiteSpace(metadata.Health.Endpoint))
        {
            yield return "Health endpoint is required when requiresHttpProbe is true.";
        }

        var duplicateDependencies = metadata.Dependencies
            .GroupBy(dependency => dependency.Service, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key);

        foreach (var duplicate in duplicateDependencies)
        {
            yield return $"Duplicate dependency declaration for '{duplicate}'.";
        }

        foreach (var dependency in metadata.Dependencies)
        {
            if (string.IsNullOrWhiteSpace(dependency.Service))
            {
                yield return "Dependency service name cannot be empty.";
            }

            if (string.Equals(dependency.Service, metadata.ServiceId, StringComparison.OrdinalIgnoreCase))
            {
                yield return $"Service '{metadata.ServiceId}' cannot depend on itself.";
            }
        }
    }

    private static IEnumerable<string> ValidateManifestConsistency(DeploymentMetadata metadata, DeployManifest manifest)
    {
        if (!string.Equals(manifest.Service, metadata.ServiceId, StringComparison.OrdinalIgnoreCase))
        {
            yield return $"Manifest service '{manifest.Service}' does not match metadata serviceId '{metadata.ServiceId}'.";
        }

        if (!manifest.Ports.Contains(metadata.Ports.ContainerPort))
        {
            yield return "Manifest ports do not match deployment metadata containerPort.";
        }

        var expectedDependencies = metadata.GetRequiredServiceDependencies();
        if (!expectedDependencies.SequenceEqual(manifest.Dependencies, StringComparer.OrdinalIgnoreCase))
        {
            yield return "Manifest dependencies do not match deployment metadata.";
        }
    }

    private static IEnumerable<string> ValidateHealthConsistency(DeploymentMetadata metadata, DeployHealthDefinition health)
    {
        if (metadata.Health.RequiresHttpProbe
            && !string.Equals(health.Endpoint, metadata.Health.Endpoint, StringComparison.OrdinalIgnoreCase))
        {
            yield return "Health endpoint does not match deployment metadata.";
        }

        if (health.ExpectedStatus != metadata.Health.ExpectedStatus
            || health.TimeoutSeconds != metadata.Health.TimeoutSeconds
            || health.Retries != metadata.Health.Retries)
        {
            yield return "Health probe settings do not match deployment metadata.";
        }
    }

    private static ValidationResult Passed()
        => new() { Passed = true };

    private static ValidationResult Failed(IReadOnlyList<string> errors)
        => new() { Passed = false, Errors = errors };
}
