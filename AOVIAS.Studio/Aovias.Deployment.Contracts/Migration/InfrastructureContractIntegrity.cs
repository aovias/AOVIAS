using System.Security.Cryptography;
using System.Text;
using Aovias.Deployment.Contracts.Capabilities;
using Aovias.Deployment.Contracts.Infrastructure;
using Aovias.Deployment.Contracts.Serialization;

namespace Aovias.Deployment.Contracts.Migration;

/// <summary>
/// Computes <see cref="ContractIntegrityMetadata.ContentHash"/>: a hash of the domain payload
/// only, deliberately excluding the schema envelope (Sprint M2.2 D1/D7).
///
/// Excluding the envelope is what makes the hash useful for canonical-equality verification
/// (D7): a schema v1 contract migrated to v2 and a schema v2 contract compiled natively from
/// the same declared infrastructure have different envelopes (different <c>schemaVersion</c>,
/// different <c>migration</c> block) but, if the infrastructure they describe is identical,
/// must have the same content hash. <see cref="ContractDomainSnapshot"/> normalizes
/// <c>contractVersion</c> to the current schema's value for exactly this reason — the version
/// label is envelope metadata, not domain content.
/// </summary>
public static class InfrastructureContractIntegrity
{
    public const string Algorithm = "SHA256";

    public static string ComputeContentHash(InfrastructureContract contract)
    {
        ArgumentNullException.ThrowIfNull(contract);
        return Compute(ContractDomainSnapshot.FromV2(contract));
    }

    public static string ComputeContentHash(InfrastructureContractV1 contract)
    {
        ArgumentNullException.ThrowIfNull(contract);
        return Compute(ContractDomainSnapshot.FromV1(contract));
    }

    private static string Compute(ContractDomainSnapshot snapshot)
    {
        var json = DeploymentContractJson.Serialize(snapshot);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json))).ToLowerInvariant();
    }
}

/// <summary>
/// The domain-only projection of an infrastructure contract, used exclusively to compute
/// <see cref="ContractIntegrityMetadata.ContentHash"/>. Not part of the public wire format.
/// </summary>
internal sealed class ContractDomainSnapshot
{
    public required string ServiceId { get; init; }

    public required string ContractVersion { get; init; }

    public required FilesystemContract Filesystem { get; init; }

    public required CertificateContract Certificates { get; init; }

    public required SecretContract Secrets { get; init; }

    public required DatabaseContract Database { get; init; }

    public required DockerInfrastructureContract Docker { get; init; }

    public required NetworkContract Network { get; init; }

    public required VolumeContract Storage { get; init; }

    public required IReadOnlyList<ExternalProviderContract> Providers { get; init; }

    public required DependencyContract Dependencies { get; init; }

    public required HealthContract Health { get; init; }

    /// <summary>
    /// Sprint M2.3: requirements are domain content — two contracts declaring different
    /// infrastructure requirements are not semantically equal even if every other domain field
    /// matches — so they participate in the content hash. Schema v1 never had this concept, so
    /// <see cref="FromV1"/> always projects an empty list.
    /// </summary>
    public required IReadOnlyList<Requirement> Requirements { get; init; }

    public static ContractDomainSnapshot FromV2(InfrastructureContract source) => new()
    {
        ServiceId = source.ServiceId,
        ContractVersion = ContractSchemaVersions.ContractVersionV2,
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
        Requirements = source.Requirements
    };

    public static ContractDomainSnapshot FromV1(InfrastructureContractV1 source) => new()
    {
        ServiceId = source.ServiceId,
        ContractVersion = ContractSchemaVersions.ContractVersionV2,
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
        Requirements = []
    };
}
