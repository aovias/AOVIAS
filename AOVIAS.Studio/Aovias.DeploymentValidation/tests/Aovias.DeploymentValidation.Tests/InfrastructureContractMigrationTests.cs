using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Aovias.Deployment.Contracts;
using Aovias.Deployment.Contracts.Building;
using Aovias.Deployment.Contracts.Infrastructure;
using Aovias.Deployment.Contracts.Migration;
using Aovias.Deployment.Contracts.Reading;
using Aovias.Deployment.Contracts.Serialization;
using Xunit;

namespace Aovias.DeploymentValidation.Tests;

/// <summary>
/// Sprint M2.2 regression tests (D9): schema v2 serialization, version detection, migration,
/// unsupported versions, downgrade rejection, corrupted schema, canonical equality, the
/// compatibility matrix, and deterministic serialization.
/// </summary>
public sealed class InfrastructureContractMigrationTests
{
    // ---- D2 / D5: schema v2 is read natively, with no migration performed ----

    [Fact]
    public void NativeSchemaV2Contract_ReadsWithoutMigration()
    {
        using var package = NativePackage.Create("gateway");
        var reader = new DeploymentPackageContractReader();

        var contract = reader.ReadFromPackage(package.Path);

        Assert.Equal(ContractSchemaVersions.Current, contract.SchemaVersion);
        Assert.Equal(ContractSchemaVersions.ContractVersionV2, contract.ContractVersion);
        Assert.Null(contract.Migration);
        Assert.Equal("gateway", contract.ServiceId);
        Assert.Equal("gateway", contract.Identity.ServiceId);
    }

    // ---- D3 / D5: schema v1 packages are detected and migrated transparently ----

    [Fact]
    public void SchemaV1Package_IsDetectedAndMigratedToCurrentSchema()
    {
        using var package = LegacyPackage.Create("audit-service");
        var reader = new DeploymentPackageContractReader();

        var contract = reader.ReadFromPackage(package.Path);

        Assert.Equal(ContractSchemaVersions.Current, contract.SchemaVersion);
        Assert.NotNull(contract.Migration);
        Assert.Equal(ContractSchemaVersions.V1, contract.Migration!.MigratedFromSchemaVersion);
        Assert.Equal(nameof(InfrastructureContractV1ToV2Migrator), contract.Migration.MigratorId);
        Assert.Equal("audit-service", contract.ServiceId);
        // v1 never carried a service version inside infrastructure.contract.json — migration
        // does not fabricate one (see the M2.2 Migration Policy).
        Assert.Null(contract.Identity.ServiceVersion);
    }

    [Fact]
    public void Migration_PreservesDomainContentExactly()
    {
        var bundle = BuildBundle("knowledge-base");
        var built = InfrastructureContractBuilder.Build(bundle);
        var legacy = ToLegacyV1(built, ContractSchemaVersions.ContractVersionV1);

        var migrated = new InfrastructureContractV1ToV2Migrator().Migrate(legacy);

        Assert.Equal(legacy.ServiceId, migrated.ServiceId);
        Assert.Same(legacy.Filesystem, migrated.Filesystem);
        Assert.Same(legacy.Certificates, migrated.Certificates);
        Assert.Same(legacy.Secrets, migrated.Secrets);
        Assert.Same(legacy.Database, migrated.Database);
        Assert.Same(legacy.Docker, migrated.Docker);
        Assert.Same(legacy.Network, migrated.Network);
        Assert.Same(legacy.Storage, migrated.Storage);
        Assert.Same(legacy.Providers, migrated.Providers);
        Assert.Same(legacy.Dependencies, migrated.Dependencies);
        Assert.Same(legacy.Health, migrated.Health);
    }

    [Fact]
    public void Migrator_IsDeterministic_AcrossIndependentInvocations()
    {
        var bundle = BuildBundle("organization-service");
        var built = InfrastructureContractBuilder.Build(bundle);
        var legacy = ToLegacyV1(built, ContractSchemaVersions.ContractVersionV1);
        var migrator = new InfrastructureContractV1ToV2Migrator();

        var first = migrator.Migrate(legacy);
        var second = migrator.Migrate(legacy);

        Assert.Equal(DeploymentContractJson.Serialize(first), DeploymentContractJson.Serialize(second));
    }

    // ---- D7: a migrated v1 contract and a natively-compiled v2 contract must be semantically equal ----

    [Fact]
    public void CanonicalEquality_MigratedV1MatchesNativeV2_ForTheSameDeclaredInfrastructure()
    {
        var bundle = BuildBundle("compliance-service");
        var built = InfrastructureContractBuilder.Build(bundle);
        var legacy = ToLegacyV1(built, ContractSchemaVersions.ContractVersionV1);

        var migrated = new InfrastructureContractV1ToV2Migrator().Migrate(legacy);
        var native = ToNativeV2(built);

        Assert.Equal(
            InfrastructureContractIntegrity.ComputeContentHash(native),
            InfrastructureContractIntegrity.ComputeContentHash(migrated));
    }

    [Fact]
    public void CanonicalEquality_DetectsADeliberateSemanticDifference()
    {
        var bundle = BuildBundle("compliance-service");
        var built = InfrastructureContractBuilder.Build(bundle);
        var legacy = ToLegacyV1(built, ContractSchemaVersions.ContractVersionV1);
        var migrated = new InfrastructureContractV1ToV2Migrator().Migrate(legacy);

        var native = ToNativeV2(built);
        var tamperedNative = new InfrastructureContract
        {
            SchemaVersion = native.SchemaVersion,
            ContractVersion = native.ContractVersion,
            ServiceId = native.ServiceId,
            Identity = native.Identity,
            Compatibility = native.Compatibility,
            Migration = native.Migration,
            Integrity = native.Integrity,
            PublishingEngineVersion = native.PublishingEngineVersion,
            SourceRevision = native.SourceRevision,
            GeneratedUtc = native.GeneratedUtc,
            Filesystem = native.Filesystem,
            Certificates = native.Certificates,
            Secrets = native.Secrets,
            Database = native.Database,
            Docker = native.Docker,
            Network = native.Network,
            Storage = native.Storage,
            Providers = native.Providers,
            Dependencies = native.Dependencies,
            // Deliberately change one domain field so the two contracts are no longer
            // semantically equal, proving the content hash is sensitive to real content.
            Health = native.Health with { HealthUrl = "/changed-health-path" }
        };

        Assert.NotEqual(
            InfrastructureContractIntegrity.ComputeContentHash(tamperedNative),
            InfrastructureContractIntegrity.ComputeContentHash(migrated));
    }

    // ---- D3 / D9: unsupported and corrupted schema versions are rejected deterministically ----

    [Fact]
    public void UnsupportedSchemaVersion_IsRejectedWithADeterministicCode()
    {
        using var package = RawPackage.Create("""{"schemaVersion":99,"contractVersion":"99.0","serviceId":"gateway"}""");

        var error = Assert.Throws<DeploymentPackageContractException>(
            () => new DeploymentPackageContractReader().ReadFromPackage(package.Path));

        Assert.Equal("ICA010", error.Code);
    }

    [Fact]
    public void CorruptedSchemaVersion_TypeMismatch_IsRejected()
    {
        using var package = RawPackage.Create("""{"schemaVersion":"not-a-number","serviceId":"gateway"}""");

        var error = Assert.Throws<DeploymentPackageContractException>(
            () => new DeploymentPackageContractReader().ReadFromPackage(package.Path));

        Assert.Equal("ICA006", error.Code);
    }

    [Fact]
    public void NegativeSchemaVersion_IsTreatedAsUnknownAndRejected()
    {
        using var package = RawPackage.Create("""{"schemaVersion":-1,"serviceId":"gateway"}""");

        var error = Assert.Throws<DeploymentPackageContractException>(
            () => new DeploymentPackageContractReader().ReadFromPackage(package.Path));

        Assert.Equal("ICA010", error.Code);
    }

    // ---- D4: the compatibility matrix, exhaustively ----

    [Theory]
    [InlineData(2, 2, ContractCompatibilityResult.Pass)]
    [InlineData(1, 2, ContractCompatibilityResult.AutoMigrate)]
    [InlineData(2, 1, ContractCompatibilityResult.Fail)]
    [InlineData(0, 2, ContractCompatibilityResult.Fail)]
    public void CompatibilityMatrix_MatchesTheDocumentedFourRows(int publisher, int runtime, ContractCompatibilityResult expected)
    {
        var evaluation = InfrastructureContractCompatibility.Evaluate(publisher, runtime, [1]);

        Assert.Equal(expected, evaluation.Result);
    }

    [Fact]
    public void CompatibilityMatrix_DowngradeScenario_NewerPublisherFailsOlderRuntime()
    {
        // "v2 publisher / v1 runtime -> FAIL": a hypothetical runtime whose ceiling is schema v1
        // (i.e. it predates this sprint) must reject a schema v2 contract rather than guess at it.
        var evaluation = InfrastructureContractCompatibility.Evaluate(
            publisherSchemaVersion: ContractSchemaVersions.V2,
            runtimeMaxSupportedSchemaVersion: ContractSchemaVersions.V1);

        Assert.Equal(ContractCompatibilityResult.Fail, evaluation.Result);
    }

    // ---- helpers ----

    private static ServiceDeploymentBundle BuildBundle(string serviceId) => ServiceContractBuilder.Build(new ServiceContractBuildRequest
    {
        ServiceId = serviceId,
        DisplayName = serviceId,
        Version = "1.0.0",
        ImageReference = $"aovias/{serviceId}:1.0.0",
        ContainerName = serviceId,
        EntryPoint = "App.dll",
        HealthEndpoint = "/health",
        Ports = [5000]
    });

    private static InfrastructureContractV1 ToLegacyV1(InfrastructureContract built, string contractVersion) => new()
    {
        ServiceId = built.ServiceId,
        ContractVersion = contractVersion,
        Filesystem = built.Filesystem,
        Certificates = built.Certificates,
        Secrets = built.Secrets,
        Database = built.Database,
        Docker = built.Docker,
        Network = built.Network,
        Storage = built.Storage,
        Providers = built.Providers,
        Dependencies = built.Dependencies,
        Health = built.Health
    };

    private static InfrastructureContract ToNativeV2(InfrastructureContract built)
    {
        var domainOnly = new InfrastructureContract
        {
            ServiceId = built.ServiceId,
            ContractVersion = ContractSchemaVersions.ContractVersionV2,
            Filesystem = built.Filesystem,
            Certificates = built.Certificates,
            Secrets = built.Secrets,
            Database = built.Database,
            Docker = built.Docker,
            Network = built.Network,
            Storage = built.Storage,
            Providers = built.Providers,
            Dependencies = built.Dependencies,
            Health = built.Health
        };

        return new InfrastructureContract
        {
            SchemaVersion = ContractSchemaVersions.Current,
            ContractVersion = ContractSchemaVersions.ContractVersionV2,
            ServiceId = built.ServiceId,
            Identity = new ContractIdentity { ServiceId = built.ServiceId, ServiceVersion = "1.0.0" },
            Compatibility = new ContractCompatibilityMetadata
            {
                MinimumSupportedSchemaVersion = ContractSchemaVersions.V1,
                MaximumSupportedSchemaVersion = ContractSchemaVersions.Current
            },
            Migration = null,
            Integrity = new ContractIntegrityMetadata
            {
                HashAlgorithm = InfrastructureContractIntegrity.Algorithm,
                ContentHash = InfrastructureContractIntegrity.ComputeContentHash(domainOnly)
            },
            PublishingEngineVersion = "9.9.9-test",
            SourceRevision = "test-revision",
            GeneratedUtc = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            Filesystem = built.Filesystem,
            Certificates = built.Certificates,
            Secrets = built.Secrets,
            Database = built.Database,
            Docker = built.Docker,
            Network = built.Network,
            Storage = built.Storage,
            Providers = built.Providers,
            Dependencies = built.Dependencies,
            Health = built.Health
        };
    }

    private sealed class NativePackage : IDisposable
    {
        private NativePackage(string path) => Path = path;

        public string Path { get; }

        public static NativePackage Create(string serviceId)
        {
            var bundle = BuildBundle(serviceId);
            var built = InfrastructureContractBuilder.Build(bundle);
            var contract = ToNativeV2(built);
            return new NativePackage(PackageFixture.Write(contract));
        }

        public void Dispose() => PackageFixture.Cleanup(Path);
    }

    private sealed class LegacyPackage : IDisposable
    {
        private LegacyPackage(string path) => Path = path;

        public string Path { get; }

        public static LegacyPackage Create(string serviceId)
        {
            var bundle = BuildBundle(serviceId);
            var built = InfrastructureContractBuilder.Build(bundle);
            var contract = ToLegacyV1(built, ContractSchemaVersions.ContractVersionV1);
            return new LegacyPackage(PackageFixture.Write(contract));
        }

        public void Dispose() => PackageFixture.Cleanup(Path);
    }

    private sealed class RawPackage : IDisposable
    {
        private RawPackage(string path) => Path = path;

        public string Path { get; }

        public static RawPackage Create(string rawJson) => new(PackageFixture.WriteRaw(rawJson));

        public void Dispose() => PackageFixture.Cleanup(Path);
    }

    private static class PackageFixture
    {
        public static string Write<T>(T contract)
        {
            var directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            System.IO.Directory.CreateDirectory(directory);
            var contractPath = System.IO.Path.Combine(directory, DeploymentPackageLayout.InfrastructureContractFile);
            DeploymentContractJson.WriteFile(contractPath, contract);
            return Finish(directory, contractPath);
        }

        public static string WriteRaw(string json)
        {
            var directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            System.IO.Directory.CreateDirectory(directory);
            var contractPath = System.IO.Path.Combine(directory, DeploymentPackageLayout.InfrastructureContractFile);
            File.WriteAllText(contractPath, json, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            return Finish(directory, contractPath);
        }

        private static string Finish(string directory, string contractPath)
        {
            var checksums = new Dictionary<string, string>
            {
                [DeploymentPackageLayout.InfrastructureContractFile] =
                    Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(contractPath))).ToLowerInvariant()
            };
            File.WriteAllText(System.IO.Path.Combine(directory, "checksums.json"), JsonSerializer.Serialize(checksums));

            var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"contract-{Guid.NewGuid():N}.aovdeploy");
            ZipFile.CreateFromDirectory(directory, path);
            System.IO.Directory.Delete(directory, recursive: true);
            return path;
        }

        public static void Cleanup(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
