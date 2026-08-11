using Aovias.Deployment.Contracts;
using Aovias.Deployment.Contracts.Infrastructure;
using Aovias.Deployment.Contracts.Migration;
using Aovias.Deployment.Contracts.Specifications;

namespace Aovias.Publishing.Core.Generation;

/// <summary>
/// Publishing-only compiler from an authoring specification to the authoritative runtime
/// contract, at the current contract schema (Sprint M2.1 D2; schema v2 envelope added
/// Sprint M2.2 D1/D2). Always emits <see cref="ContractSchemaVersions.Current"/> — Runtime
/// Agent and DRV read whatever schema version this constant currently names, via
/// <see cref="Aovias.Deployment.Contracts.Reading.DeploymentPackageContractReader"/>, and never
/// call this compiler themselves.
/// </summary>
internal static class InfrastructureSpecificationCompiler
{
    public static InfrastructureContract Compile(ServiceDeploymentBundle bundle, ContractPublishingMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(bundle);
        ArgumentNullException.ThrowIfNull(metadata);
        var specification = InfrastructureSpecificationLoader.LoadExpanded(bundle.Manifest.ServiceId);
        var dependencies = bundle.Dependency.Dependencies.Select(item => item.Service).ToList();
        InfrastructureSpecificationValidator.Validate(specification, dependencies);

        var docker = new DockerInfrastructureContract
        {
            RequireEngine = specification.Runtime.Docker,
            RequireCompose = specification.Runtime.Docker,
            ImageReference = bundle.Manifest.Docker.Image,
            RestartPolicy = bundle.Manifest.Deployment.RestartPolicy,
            CreateNetworkIfMissing = true,
            RequiredImages = string.IsNullOrWhiteSpace(bundle.Manifest.Docker.Image)
                ? []
                : [bundle.Manifest.Docker.Image]
        };

        var contentHash = InfrastructureContractIntegrity.ComputeContentHash(new InfrastructureContract
        {
            ServiceId = bundle.Manifest.ServiceId,
            ContractVersion = ContractSchemaVersions.ContractVersionV2,
            Filesystem = specification.Filesystem,
            Certificates = specification.Certificates,
            Secrets = bundle.Secret,
            Database = specification.Database,
            Docker = docker,
            Network = specification.Network,
            Storage = specification.Volumes,
            Providers = specification.Providers,
            Dependencies = bundle.Dependency,
            Health = bundle.Health,
            Requirements = specification.Requirements
        });

        return new InfrastructureContract
        {
            SchemaVersion = ContractSchemaVersions.Current,
            ContractVersion = ContractSchemaVersions.ContractVersionV2,
            ServiceId = bundle.Manifest.ServiceId,
            Identity = new ContractIdentity
            {
                ServiceId = bundle.Manifest.ServiceId,
                ServiceVersion = bundle.Manifest.Version
            },
            Compatibility = new ContractCompatibilityMetadata
            {
                MinimumSupportedSchemaVersion = ContractSchemaVersions.V1,
                MaximumSupportedSchemaVersion = ContractSchemaVersions.Current
            },
            Migration = null,
            Integrity = new ContractIntegrityMetadata
            {
                HashAlgorithm = InfrastructureContractIntegrity.Algorithm,
                ContentHash = contentHash
            },
            PublishingEngineVersion = metadata.PublishingEngineVersion,
            SourceRevision = metadata.SourceRevision,
            GeneratedUtc = metadata.GeneratedUtc,
            Filesystem = specification.Filesystem,
            Certificates = specification.Certificates,
            Secrets = bundle.Secret,
            Database = specification.Database,
            Docker = docker,
            Network = specification.Network,
            Storage = specification.Volumes,
            Providers = specification.Providers,
            Dependencies = bundle.Dependency,
            Health = bundle.Health,
            // Sprint M2.3 D7: requirements are compiled from the specification unchanged.
            // Publishing never resolves them against an environment's capabilities — that is
            // the Deployment Planner's job, at deployment time.
            Requirements = specification.Requirements
        };
    }
}
