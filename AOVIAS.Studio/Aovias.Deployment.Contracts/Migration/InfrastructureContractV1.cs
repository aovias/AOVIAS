using Aovias.Deployment.Contracts.Infrastructure;

namespace Aovias.Deployment.Contracts.Migration;

/// <summary>
/// Exact structural mirror of the infrastructure contract wire shape published under
/// Sprint M2.0/M2.1 (schema v1: no envelope metadata, <c>contractVersion: "1.0"</c>).
/// This type exists solely so the reader can deserialize and byte-for-byte round-trip
/// a legacy package against its *original* shape before handing it to a migrator —
/// the unified, envelope-carrying <see cref="InfrastructureContract"/> type cannot be
/// used for that round-trip check because it now serializes additional schema v2 keys
/// that a genuine v1 file on disk never contained (Sprint M2.2, Required Engineering Analysis).
/// Publishing never constructs this type; it is a read-path compatibility artifact only.
/// </summary>
public sealed class InfrastructureContractV1
{
    public string ServiceId { get; init; } = string.Empty;

    public string ContractVersion { get; init; } = ContractSchemaVersions.ContractVersionV1;

    public FilesystemContract Filesystem { get; init; } = new();

    public CertificateContract Certificates { get; init; } = new();

    public SecretContract Secrets { get; init; } = new();

    public DatabaseContract Database { get; init; } = new();

    public DockerInfrastructureContract Docker { get; init; } = new();

    public NetworkContract Network { get; init; } = new();

    public VolumeContract Storage { get; init; } = new();

    public IReadOnlyList<ExternalProviderContract> Providers { get; init; } = [];

    public DependencyContract Dependencies { get; init; } = new();

    public HealthContract Health { get; init; } = new() { HealthUrl = "/health" };
}
