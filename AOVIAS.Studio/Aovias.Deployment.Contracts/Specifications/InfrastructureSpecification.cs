using System.Reflection;
using System.Text.Json;
using Aovias.Deployment.Contracts.Capabilities;
using Aovias.Deployment.Contracts.Infrastructure;
using Aovias.Deployment.Contracts.Serialization;
namespace Aovias.Deployment.Contracts.Specifications;

public sealed class InfrastructureSpecification
{
    public string SpecificationVersion { get; init; } = "1.0";
    public required string ServiceId { get; set; }
    public required string Profile { get; init; }
    public string? Filesystem { get; init; }
    public string? Certificates { get; init; }
    public string? Database { get; init; }
    public string? Secrets { get; init; }
    public string? Configuration { get; init; }
    public string? Health { get; init; }
    public string? Volumes { get; init; }
    public bool PublishedDependencies { get; init; } = true;
    public FeatureContract Features { get; init; } = new();
    public RuntimeCapabilities Runtime { get; init; } = new();
    public IReadOnlyList<ExternalProviderContract> Providers { get; init; } = [];

    /// <summary>
    /// Provider-agnostic infrastructure requirements this service declares (Sprint M2.3 D1/D7).
    /// Authored as data, the same as every other specification field — never resolved here;
    /// resolution against an environment's capabilities happens at deployment time.
    /// </summary>
    public IReadOnlyList<Requirement> Requirements { get; init; } = [];
}

public sealed class ExpandedInfrastructureSpecification
{
    public required string ServiceId { get; init; }
    public required string Profile { get; init; }
    public required FilesystemContract Filesystem { get; init; }
    public required CertificateContract Certificates { get; init; }
    public required DatabaseContract Database { get; init; }
    public required SecretContract Secrets { get; init; }
    public required ConfigurationContract Configuration { get; init; }
    public required HealthContract Health { get; init; }
    public required NetworkContract Network { get; init; }
    public required VolumeContract Volumes { get; init; }
    public required FeatureContract Features { get; init; }
    public required RuntimeCapabilities Runtime { get; init; }
    public IReadOnlyList<ExternalProviderContract> Providers { get; init; } = [];
    public bool PublishedDependencies { get; init; }
    public IReadOnlyList<Requirement> Requirements { get; init; } = [];
}

public sealed class InfrastructureSpecificationException : InvalidOperationException
{
    public InfrastructureSpecificationException(IReadOnlyList<string> errors)
        : base("Infrastructure specification is invalid: " + string.Join("; ", errors)) => Errors = errors;
    public IReadOnlyList<string> Errors { get; }
}

public static class InfrastructureSpecificationLoader
{
    private const string ResourceName = "Aovias.Deployment.Contracts.Specifications.infrastructure-specifications.json";
    private static readonly Lazy<IReadOnlyDictionary<string, InfrastructureSpecification>> Specifications = new(LoadCatalog);

    public static InfrastructureSpecification Load(string serviceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceId);
        var normalized = serviceId.ToLowerInvariant();
        var source = Specifications.Value.TryGetValue(normalized, out var exact) ? exact : Specifications.Value["_default"];
        var copy = DeploymentContractJson.Deserialize<InfrastructureSpecification>(DeploymentContractJson.Serialize(source))
            ?? throw new InvalidOperationException("Unable to clone infrastructure specification.");
        copy.ServiceId = normalized;
        return copy;
    }

    public static ExpandedInfrastructureSpecification LoadExpanded(string serviceId)
    {
        var expanded = InfrastructureAuthoringProfiles.Expand(Load(serviceId));
        InfrastructureSpecificationValidator.Validate(expanded);
        return expanded;
    }

    private static IReadOnlyDictionary<string, InfrastructureSpecification> LoadCatalog()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Embedded infrastructure specification catalog '{ResourceName}' was not found.");
        var catalog = JsonSerializer.Deserialize<Catalog>(stream, DeploymentContractJson.Options)
            ?? throw new InvalidOperationException("Infrastructure specification catalog is invalid.");
        return catalog.Services.ToDictionary(item => item.ServiceId, StringComparer.OrdinalIgnoreCase);
    }

    private sealed class Catalog { public IReadOnlyList<InfrastructureSpecification> Services { get; init; } = []; }
}

public static class InfrastructureSpecificationValidator
{
    public static void Validate(ExpandedInfrastructureSpecification specification, IReadOnlyList<string>? dependencies = null)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(specification.ServiceId)) errors.Add("serviceId is required.");
        if (string.IsNullOrWhiteSpace(specification.Profile)) errors.Add("profile is required.");
        AddDuplicates(errors, specification.Filesystem.Directories.Concat(specification.Filesystem.ReadOnlyDirectories).Select(x => x.Path), "filesystem path");
        AddDuplicates(errors, specification.Certificates.Certificates.Select(x => x.Id), "certificate id");
        AddDuplicates(errors, specification.Secrets.Secrets.Select(x => x.Id), "secret id");
        AddDuplicates(errors, specification.Providers.Select(x => x.Id), "provider id");
        AddDuplicates(errors, specification.Volumes.Mounts.Select(x => x.ContainerPath), "volume target");

        foreach (var certificate in specification.Certificates.Certificates.Where(x => !string.IsNullOrWhiteSpace(x.SecretKey)))
            if (!specification.Secrets.Secrets.Any(x => x.Name.Equals(certificate.SecretKey, StringComparison.OrdinalIgnoreCase)))
                errors.Add($"Certificate '{certificate.Id}' references unknown secret '{certificate.SecretKey}'.");

        var paths = specification.Filesystem.Directories.Concat(specification.Filesystem.ReadOnlyDirectories)
            .Select(x => x.Path).ToHashSet(StringComparer.Ordinal);
        foreach (var mount in specification.Volumes.Mounts.Where(x => !paths.Contains(x.ContainerPath)))
            errors.Add($"Volume target '{mount.ContainerPath}' is not declared by filesystem.");

        AddDuplicates(errors, dependencies ?? [], "dependency");
        foreach (var dependency in dependencies ?? [])
        {
            if (string.IsNullOrWhiteSpace(dependency)) errors.Add("Dependencies cannot be empty.");
            if (dependency.Equals(specification.ServiceId, StringComparison.OrdinalIgnoreCase))
                errors.Add($"Service '{specification.ServiceId}' cannot depend on itself.");
        }

        var health = specification.Health;
        if (health.RequiresHttpProbe && (string.IsNullOrWhiteSpace(health.HealthUrl) || !health.HealthUrl.StartsWith('/')))
            errors.Add("HTTP healthUrl must be an absolute application path.");
        if (health.ExpectedStatus is < 100 or > 599) errors.Add("Health expectedStatus must be between 100 and 599.");
        if (health.TimeoutSeconds <= 0 || health.Retries < 0 || health.StartupTimeoutSeconds <= 0)
            errors.Add("Health timeout, retries, and startup timeout are invalid.");

        // Sprint M2.3 D9: requirement-level validation (unknown categories, duplicate ids,
        // conflicting version constraints) folds into the same error list — one invalid
        // specification, one exception, regardless of which domain the error came from.
        try
        {
            RequirementValidator.Validate(specification.Requirements);
        }
        catch (RequirementValidationException requirementErrors)
        {
            errors.AddRange(requirementErrors.Errors);
        }

        if (errors.Count > 0) throw new InfrastructureSpecificationException(errors);
    }

    private static void AddDuplicates(List<string> errors, IEnumerable<string> values, string label)
    {
        foreach (var duplicate in values.Where(x => !string.IsNullOrWhiteSpace(x))
                     .GroupBy(x => x, StringComparer.OrdinalIgnoreCase).Where(x => x.Count() > 1).Select(x => x.Key))
            errors.Add($"Duplicate {label} '{duplicate}'.");
    }
}