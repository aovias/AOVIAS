namespace Aovias.Deployment.Contracts.Migration;

/// <summary>Outcome of evaluating a publisher schema version against a runtime's capability (Sprint M2.2 D4).</summary>
public enum ContractCompatibilityResult
{
    /// <summary>The runtime understands this schema version natively; no migration needed.</summary>
    Pass,

    /// <summary>The runtime is newer than the publisher; a migrator can upgrade the contract in memory.</summary>
    AutoMigrate,

    /// <summary>The contract cannot be safely consumed by this runtime.</summary>
    Fail
}

/// <summary>The compatibility decision plus a human-readable reason, suitable for the ICA-coded reader failures.</summary>
public sealed record ContractCompatibilityEvaluation(ContractCompatibilityResult Result, string Reason);

/// <summary>
/// The single, authoritative implementation of the M2.2 compatibility matrix (D4):
/// <code>
/// Publisher   Runtime   Result
/// v2          v2        Pass
/// v1          v2        AutoMigrate
/// v2          v1        Fail
/// Unknown     Any       Fail
/// </code>
/// <see cref="Aovias.Deployment.Contracts.Reading.DeploymentPackageContractReader"/> calls this to decide whether to read a contract
/// natively, migrate it, or reject it — it never re-implements this decision inline. Because the
/// runtime's ceiling and migratable-from set are parameters rather than constants baked into this
/// method, the same evaluator will keep working unchanged when a v3 schema is introduced: only
/// <see cref="ContractSchemaVersions"/> and the registered migrators grow, this method does not.
/// </summary>
public static class InfrastructureContractCompatibility
{
    public static ContractCompatibilityEvaluation Evaluate(
        int publisherSchemaVersion,
        int runtimeMaxSupportedSchemaVersion,
        IReadOnlyCollection<int>? runtimeMigratableFromVersions = null)
    {
        var migratable = runtimeMigratableFromVersions
            ?? ContractSchemaVersions.Supported.Where(version => version < runtimeMaxSupportedSchemaVersion).ToList();

        if (publisherSchemaVersion <= 0)
        {
            return new ContractCompatibilityEvaluation(
                ContractCompatibilityResult.Fail,
                $"Schema version '{publisherSchemaVersion}' is not a recognized contract schema version.");
        }

        if (publisherSchemaVersion == runtimeMaxSupportedSchemaVersion)
        {
            return new ContractCompatibilityEvaluation(
                ContractCompatibilityResult.Pass,
                $"Publisher schema {publisherSchemaVersion} matches the runtime's schema {runtimeMaxSupportedSchemaVersion} exactly.");
        }

        if (publisherSchemaVersion > runtimeMaxSupportedSchemaVersion)
        {
            return new ContractCompatibilityEvaluation(
                ContractCompatibilityResult.Fail,
                $"Publisher schema {publisherSchemaVersion} is newer than this runtime's maximum supported schema {runtimeMaxSupportedSchemaVersion}.");
        }

        if (migratable.Contains(publisherSchemaVersion))
        {
            return new ContractCompatibilityEvaluation(
                ContractCompatibilityResult.AutoMigrate,
                $"Publisher schema {publisherSchemaVersion} is older than runtime schema {runtimeMaxSupportedSchemaVersion}; a registered migrator will upgrade it.");
        }

        return new ContractCompatibilityEvaluation(
            ContractCompatibilityResult.Fail,
            $"Publisher schema {publisherSchemaVersion} has no migration path to runtime schema {runtimeMaxSupportedSchemaVersion}.");
    }
}
