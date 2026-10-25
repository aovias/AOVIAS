namespace Aovias.Deployment.Contracts.Migration;

/// <summary>
/// Identifies which service and version an infrastructure contract belongs to,
/// independent of the file it happens to be stored in (Sprint M2.2 D1).
/// </summary>
public sealed class ContractIdentity
{
    /// <summary>Mirrors the root <see cref="Infrastructure.InfrastructureContract.ServiceId"/>.</summary>
    public string ServiceId { get; init; } = string.Empty;

    /// <summary>
    /// The published service version (e.g. <c>1.4.2</c>). Populated for natively compiled
    /// schema v2 contracts. Schema v1 packages never carried a service version inside
    /// <c>infrastructure.contract.json</c>, so this is <see langword="null"/> for migrated contracts —
    /// see the Migration Policy section of the M2.2 implementation report.
    /// </summary>
    public string? ServiceVersion { get; init; }
}

/// <summary>
/// Declares the range of schema versions this build of the platform can read, either
/// natively or through a migrator (Sprint M2.2 D1/D4). This is a self-describing echo of
/// <see cref="ContractSchemaVersions"/> at the moment the contract was produced or migrated;
/// the authoritative rules live in <see cref="InfrastructureContractCompatibility"/>.
/// </summary>
public sealed class ContractCompatibilityMetadata
{
    public int MinimumSupportedSchemaVersion { get; init; } = ContractSchemaVersions.V1;

    public int MaximumSupportedSchemaVersion { get; init; } = ContractSchemaVersions.V1;
}

/// <summary>
/// Present only on contracts that were produced in memory by a migrator rather than
/// authored directly by Publishing at the current schema (Sprint M2.2 D1/D3).
/// Deliberately carries no timestamp: two independent reads of the same immutable
/// package (Runtime Agent vs. DRV, or two DRV runs) must migrate to byte-identical
/// output, and a read-time wall-clock stamp would break that guarantee. See the
/// Migration Policy section of the M2.2 implementation report.
/// </summary>
public sealed class ContractMigrationMetadata
{
    public required int MigratedFromSchemaVersion { get; init; }

    public required string MigratorId { get; init; }
}

/// <summary>
/// A content hash of the domain payload only (filesystem, certificates, secrets, database,
/// docker, network, storage, providers, dependencies, health) — excluding this envelope itself.
/// Lets a caller confirm a contract instance was not altered after it left the compiler or
/// migrator, independent of the package-level checksum in <c>checksums.json</c> (Sprint M2.2 D1).
/// </summary>
public sealed class ContractIntegrityMetadata
{
    public string HashAlgorithm { get; init; } = "SHA256";

    public string ContentHash { get; init; } = string.Empty;
}
