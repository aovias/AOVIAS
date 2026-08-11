using Aovias.Deployment.Contracts.Infrastructure;

namespace Aovias.Deployment.Contracts.Migration;

/// <summary>
/// The only migrator registered at Sprint M2.2: schema v1 (M2.0/M2.1) to schema v2.
/// Every domain field carries straight across unchanged — v2 is a metadata envelope around
/// the same infrastructure model, not a redesign of it (out of scope per the M2.2 brief).
/// Fields the v1 wire shape never carried (service version, publishing engine version, source
/// revision, generated-UTC) are left at their honest empty/default value rather than fabricated;
/// see the M2.2 Migration Policy for why.
/// </summary>
public sealed class InfrastructureContractV1ToV2Migrator : IInfrastructureContractMigrator
{
    public int FromSchemaVersion => ContractSchemaVersions.V1;

    public InfrastructureContract Migrate(InfrastructureContractV1 source)
    {
        ArgumentNullException.ThrowIfNull(source);

        var contentHash = InfrastructureContractIntegrity.ComputeContentHash(source);

        return new InfrastructureContract
        {
            SchemaVersion = ContractSchemaVersions.Current,
            ContractVersion = ContractSchemaVersions.ContractVersionV2,
            ServiceId = source.ServiceId,
            Identity = new ContractIdentity
            {
                ServiceId = source.ServiceId,
                ServiceVersion = null
            },
            Compatibility = new ContractCompatibilityMetadata
            {
                MinimumSupportedSchemaVersion = ContractSchemaVersions.V1,
                MaximumSupportedSchemaVersion = ContractSchemaVersions.Current
            },
            Migration = new ContractMigrationMetadata
            {
                MigratedFromSchemaVersion = ContractSchemaVersions.V1,
                MigratorId = nameof(InfrastructureContractV1ToV2Migrator)
            },
            Integrity = new ContractIntegrityMetadata
            {
                HashAlgorithm = InfrastructureContractIntegrity.Algorithm,
                ContentHash = contentHash
            },
            PublishingEngineVersion = string.Empty,
            SourceRevision = string.Empty,
            GeneratedUtc = default,
            Filesystem = source.Filesystem,
            Certificates = source.Certificates,
            Secrets = source.Secrets,
            Database = source.Database,
            Docker = source.Docker,
            Network = source.Network,
            Storage = source.Storage,
            Providers = source.Providers,
            Dependencies = source.Dependencies,
            Health = source.Health,
            // Schema v1 (M2.0/M2.1) predates the Requirement model entirely (Sprint M2.3) —
            // there is nothing to migrate, so this is explicitly empty rather than inferred.
            Requirements = []
        };
    }
}
