namespace Aovias.Deployment.Contracts.Migration;

/// <summary>
/// Central registry of infrastructure contract schema versions (Sprint M2.2 D1).
/// This is the single place that names "what versions exist" and "what is current" —
/// the reader, the migrators, and the compatibility matrix all read these constants
/// instead of each holding their own copy of the version number.
/// </summary>
public static class ContractSchemaVersions
{
    /// <summary>The schema shipped through Sprint M2.0/M2.1: no envelope metadata, <c>contractVersion: "1.0"</c>.</summary>
    public const int V1 = 1;

    /// <summary>The schema introduced by Sprint M2.2: explicit envelope metadata, <c>contractVersion: "2.0"</c>.</summary>
    public const int V2 = 2;

    /// <summary>The schema version Publishing emits today.</summary>
    public const int Current = V2;

    /// <summary>The <see cref="Infrastructure.InfrastructureContract.ContractVersion"/> string a schema v1 payload must carry.</summary>
    public const string ContractVersionV1 = "1.0";

    /// <summary>The <see cref="Infrastructure.InfrastructureContract.ContractVersion"/> string a schema v2 payload must carry.</summary>
    public const string ContractVersionV2 = "2.0";

    /// <summary>Every schema version this codebase knows how to read (natively or via migration).</summary>
    public static IReadOnlyList<int> Supported { get; } = [V1, V2];
}
