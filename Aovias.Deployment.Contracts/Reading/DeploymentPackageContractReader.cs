using System.Collections.Concurrent;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Aovias.Deployment.Contracts.Infrastructure;
using Aovias.Deployment.Contracts.Migration;
using Aovias.Deployment.Contracts.Serialization;

namespace Aovias.Deployment.Contracts.Reading;

public interface IDeploymentPackageContractReader
{
    InfrastructureContract ReadFromPackage(string packagePath);
    InfrastructureContract ReadFromDirectory(string packageDirectory);
}

public sealed class DeploymentPackageContractException : InvalidOperationException
{
    public DeploymentPackageContractException(string code, string message, Exception? innerException = null)
        : base($"{code}: {message}", innerException) => Code = code;

    public string Code { get; }
}

/// <summary>
/// The single read path for published infrastructure contracts (Sprint M2.2 D5).
/// Detects the schema version of whatever was actually published, migrates schema v1
/// (M2.0/M2.1) packages to the current unified shape in memory via
/// <see cref="IInfrastructureContractMigrator"/>, and always returns the current-schema
/// <see cref="InfrastructureContract"/> — callers (Runtime Agent, DRV) never see a version
/// number or a migration decision, only the finished contract.
/// </summary>
public sealed class DeploymentPackageContractReader : IDeploymentPackageContractReader
{
    /// <summary>Retained for source compatibility; equals <see cref="ContractSchemaVersions.ContractVersionV2"/> as of Sprint M2.2.</summary>
    [Obsolete("Compare against ContractSchemaVersions.Current / ContractVersionV2 instead. Retained for source compatibility only.")]
    public const string SupportedContractVersion = ContractSchemaVersions.ContractVersionV2;

    private const string ChecksumsFile = "checksums.json";
    private readonly ConcurrentDictionary<string, CacheEntry> cache = new(StringComparer.OrdinalIgnoreCase);

    private static readonly IReadOnlyDictionary<int, IInfrastructureContractMigrator> Migrators =
        new Dictionary<int, IInfrastructureContractMigrator>
        {
            [ContractSchemaVersions.V1] = new InfrastructureContractV1ToV2Migrator()
        };

    public InfrastructureContract ReadFromPackage(string packagePath)
    {
        if (!File.Exists(packagePath))
        {
            throw Failure("ICA001", $"Deployment package was not found: {packagePath}");
        }

        using var archive = ZipFile.OpenRead(packagePath);
        var contractEntry = archive.GetEntry(DeploymentPackageLayout.InfrastructureContractFile)
            ?? throw Failure("ICA002", $"Required contract '{DeploymentPackageLayout.InfrastructureContractFile}' is missing.");
        var checksumEntry = archive.GetEntry(ChecksumsFile)
            ?? throw Failure("ICA003", $"Required checksum manifest '{ChecksumsFile}' is missing.");

        return ValidateAndDeserialize(
            $"package:{Path.GetFullPath(packagePath)}",
            ReadEntry(contractEntry),
            DeserializeChecksums(ReadEntry(checksumEntry)));
    }

    public InfrastructureContract ReadFromDirectory(string packageDirectory)
    {
        var contractPath = Path.Combine(packageDirectory, DeploymentPackageLayout.InfrastructureContractFile);
        if (!File.Exists(contractPath))
        {
            throw Failure("ICA002", $"Required contract '{DeploymentPackageLayout.InfrastructureContractFile}' is missing.");
        }

        var checksumPath = Path.Combine(packageDirectory, ChecksumsFile);
        if (!File.Exists(checksumPath))
        {
            throw Failure("ICA003", $"Required checksum manifest '{ChecksumsFile}' is missing.");
        }

        return ValidateAndDeserialize(
            $"directory:{Path.GetFullPath(packageDirectory)}",
            File.ReadAllBytes(contractPath),
            DeserializeChecksums(File.ReadAllBytes(checksumPath)));
    }

    private InfrastructureContract ValidateAndDeserialize(string cacheKey, byte[] contractBytes, IReadOnlyDictionary<string, string> checksums)
    {
        if (!checksums.TryGetValue(DeploymentPackageLayout.InfrastructureContractFile, out var expected) || string.IsNullOrWhiteSpace(expected))
        {
            throw Failure("ICA004", $"Checksum manifest does not seal '{DeploymentPackageLayout.InfrastructureContractFile}'.");
        }

        var actual = Convert.ToHexString(SHA256.HashData(contractBytes)).ToLowerInvariant();
        if (!string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase))
        {
            throw Failure("ICA005", $"Checksum mismatch for '{DeploymentPackageLayout.InfrastructureContractFile}'. Expected {expected.ToLowerInvariant()}, actual {actual}.");
        }

        if (cache.TryGetValue(cacheKey, out var cached) && cached.Checksum == actual)
        {
            return cached.Contract;
        }

        var schemaVersion = ProbeSchemaVersion(contractBytes);
        var evaluation = InfrastructureContractCompatibility.Evaluate(
            schemaVersion,
            ContractSchemaVersions.Current,
            Migrators.Keys.ToList());

        InfrastructureContract contract = evaluation.Result switch
        {
            ContractCompatibilityResult.Pass => ReadNative(contractBytes),
            ContractCompatibilityResult.AutoMigrate => Migrate(contractBytes, schemaVersion),
            _ => throw Failure("ICA010", evaluation.Reason)
        };

        if (string.IsNullOrWhiteSpace(contract.ServiceId))
        {
            throw Failure("ICA008", "Infrastructure contract serviceId is required.");
        }

        cache[cacheKey] = new CacheEntry(actual, contract);
        return contract;
    }

    /// <summary>Reads the <c>schemaVersion</c> field only, without committing to a full deserialization.</summary>
    private static int ProbeSchemaVersion(byte[] contractBytes)
    {
        try
        {
            var probe = DeploymentContractJson.Deserialize<SchemaProbe>(Encoding.UTF8.GetString(contractBytes));
            return probe?.SchemaVersion ?? ContractSchemaVersions.V1;
        }
        catch (Exception exception) when (exception is JsonException or NotSupportedException)
        {
            throw Failure("ICA006", $"Contract '{DeploymentPackageLayout.InfrastructureContractFile}' schema version could not be determined: {exception.Message}", exception);
        }
    }

    /// <summary>Schema v2 path: deserialize directly, then require a byte-for-byte round trip against the current shape.</summary>
    private static InfrastructureContract ReadNative(byte[] contractBytes)
    {
        var contract = DeserializeRoundTripped<InfrastructureContract>(contractBytes);
        if (contract.ContractVersion != ContractSchemaVersions.ContractVersionV2)
        {
            throw Failure("ICA007", $"Infrastructure contract version '{contract.ContractVersion}' is inconsistent with schema version {contract.SchemaVersion}.");
        }

        return contract;
    }

    /// <summary>
    /// Schema v1 path: deserialize and round-trip against the *legacy* shape first (so a
    /// tampered or truncated old package still fails with ICA009 exactly as it always has),
    /// then hand the untouched legacy object to the registered migrator.
    /// </summary>
    private InfrastructureContract Migrate(byte[] contractBytes, int schemaVersion)
    {
        var legacy = DeserializeRoundTripped<InfrastructureContractV1>(contractBytes);
        if (legacy.ContractVersion != ContractSchemaVersions.ContractVersionV1)
        {
            throw Failure("ICA007", $"Infrastructure contract version '{legacy.ContractVersion}' is inconsistent with schema version {schemaVersion}.");
        }

        var migrator = Migrators[schemaVersion];
        var migrated = migrator.Migrate(legacy);

        if (migrated.SchemaVersion != ContractSchemaVersions.Current || migrated.Migration is null
            || migrated.Migration.MigratedFromSchemaVersion != schemaVersion)
        {
            throw Failure("ICA011", $"Migration from schema version {schemaVersion} did not produce a valid current-schema contract.");
        }

        return migrated;
    }

    private static T DeserializeRoundTripped<T>(byte[] contractBytes) where T : class
    {
        T value;
        try
        {
            value = DeploymentContractJson.Deserialize<T>(Encoding.UTF8.GetString(contractBytes))
                ?? throw Failure("ICA006", $"Contract '{DeploymentPackageLayout.InfrastructureContractFile}' deserialized to null.");
        }
        catch (DeploymentPackageContractException)
        {
            throw;
        }
        catch (Exception exception) when (exception is JsonException or NotSupportedException)
        {
            throw Failure("ICA006", $"Contract '{DeploymentPackageLayout.InfrastructureContractFile}' cannot be deserialized: {exception.Message}", exception);
        }

        var roundTrip = Encoding.UTF8.GetBytes(DeploymentContractJson.Serialize(value));
        if (!contractBytes.AsSpan().SequenceEqual(roundTrip))
        {
            throw Failure("ICA009", $"Published contract '{DeploymentPackageLayout.InfrastructureContractFile}' is not bit-for-bit equivalent after deserialization.");
        }

        return value;
    }

    private sealed class SchemaProbe
    {
        public int SchemaVersion { get; init; } = ContractSchemaVersions.V1;
    }

    private static IReadOnlyDictionary<string, string> DeserializeChecksums(byte[] bytes)
    {
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(bytes)
                ?? throw Failure("ICA003", $"Checksum manifest '{ChecksumsFile}' is invalid.");
        }
        catch (DeploymentPackageContractException)
        {
            throw;
        }
        catch (JsonException exception)
        {
            throw Failure("ICA003", $"Checksum manifest '{ChecksumsFile}' is invalid: {exception.Message}", exception);
        }
    }

    private static byte[] ReadEntry(ZipArchiveEntry entry)
    {
        using var stream = entry.Open();
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    }

    private static DeploymentPackageContractException Failure(string code, string message, Exception? inner = null)
        => new(code, message, inner);

    private sealed record CacheEntry(string Checksum, InfrastructureContract Contract);
}