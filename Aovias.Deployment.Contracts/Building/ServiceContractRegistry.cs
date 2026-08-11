using Aovias.Deployment.Contracts.Specifications;

namespace Aovias.Deployment.Contracts.Building;

internal sealed class ServiceContractTemplate
{
    public required FilesystemContract Filesystem { get; init; }
    public required CertificateContract Certificate { get; init; }
    public required DatabaseContract Database { get; init; }
    public required SecretContract Secret { get; init; }
    public required ConfigurationContract Configuration { get; init; }
    public required DependencyContract Dependency { get; init; }
    public required HealthContract Health { get; init; }
    public required NetworkContract Network { get; init; }
    public required VolumeContract Volume { get; init; }
    public required FeatureContract Feature { get; init; }
    public required RuntimeCapabilities RuntimeCapabilities { get; init; }
}

/// <summary>
/// Compatibility adapter from declarative authoring specifications to the existing bundle model.
/// It contains no service-specific construction logic.
/// </summary>
internal static class ServiceContractRegistry
{
    public static ServiceContractTemplate GetTemplate(string serviceId, ServiceContractBuildRequest request)
    {
        var specification = InfrastructureSpecificationLoader.LoadExpanded(serviceId);
        var dependencies = specification.PublishedDependencies ? request.Dependencies : [];
        InfrastructureSpecificationValidator.Validate(specification, dependencies);

        return new ServiceContractTemplate
        {
            Filesystem = specification.Filesystem,
            Certificate = specification.Certificates,
            Database = specification.Database,
            Secret = specification.Secrets,
            Configuration = specification.Configuration,
            Dependency = new DependencyContract
            {
                Dependencies = dependencies.Select(item => new DependencyRequirementContract
                {
                    Service = item,
                    Required = true,
                    Type = "service"
                }).ToList()
            },
            Health = specification.Health,
            Network = specification.Network,
            Volume = specification.Volumes,
            Feature = specification.Features,
            RuntimeCapabilities = specification.Runtime
        };
    }
}
