using System.IO.Compression;
using System.Security.Cryptography;
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
/// M2.0 contract-authority regressions. <see cref="TestPackage"/> builds a schema v1
/// (M2.0/M2.1-shaped) package on disk — via <see cref="InfrastructureContractV1"/>, the type
/// Sprint M2.2 introduced specifically to preserve this exact legacy wire shape — so these
/// tests keep proving what they always proved: that a real, historically-shaped package still
/// reads correctly. Sprint M2.2 adds its own migration-specific regressions in
/// <c>InfrastructureContractMigrationTests</c>.
/// </summary>
public sealed class DeploymentPackageContractReaderTests
{
    [Fact]
    public void LoadsChecksumSealedContractAndCachesImmutableInstance()
    {
        using var package = TestPackage.Create();
        var reader = new DeploymentPackageContractReader();

        var first = reader.ReadFromPackage(package.Path);
        var second = reader.ReadFromPackage(package.Path);

        Assert.Equal("gateway", first.ServiceId);
        Assert.Same(first, second);

        // A schema v1 package is migrated in memory to the current schema (D5); the migration
        // is recorded, but Publishing never emits Migration metadata for a package it writes itself.
        Assert.NotNull(first.Migration);
        Assert.Equal(ContractSchemaVersions.V1, first.Migration!.MigratedFromSchemaVersion);
        Assert.Equal(ContractSchemaVersions.Current, first.SchemaVersion);
    }

    [Fact]
    public void RejectsCorruptedContract()
    {
        using var package = TestPackage.Create(corruptAfterChecksum: true);
        var error = Assert.Throws<DeploymentPackageContractException>(
            () => new DeploymentPackageContractReader().ReadFromPackage(package.Path));
        Assert.Equal("ICA005", error.Code);
    }

    [Fact]
    public void RejectsMissingContract()
    {
        using var package = TestPackage.Create(includeContract: false);
        var error = Assert.Throws<DeploymentPackageContractException>(
            () => new DeploymentPackageContractReader().ReadFromPackage(package.Path));
        Assert.Equal("ICA002", error.Code);
    }

    [Fact]
    public void RejectsUnsupportedVersion()
    {
        using var package = TestPackage.Create(version: "2.0");
        var error = Assert.Throws<DeploymentPackageContractException>(
            () => new DeploymentPackageContractReader().ReadFromPackage(package.Path));
        Assert.Equal("ICA007", error.Code);
    }

    [Fact]
    public void RuntimeAndDrvReadsAreBitForBitEquivalent()
    {
        using var package = TestPackage.Create();
        var reader = new DeploymentPackageContractReader();
        var runtime = reader.ReadFromPackage(package.Path);
        var drv = reader.ReadFromDirectory(package.Directory);
        Assert.Equal(DeploymentContractJson.Serialize(runtime), DeploymentContractJson.Serialize(drv));
    }

    [Fact]
    public void RejectsInvalidJsonEvenWhenChecksumMatches()
    {
        using var package = TestPackage.Create(invalidJson: true);
        var error = Assert.Throws<DeploymentPackageContractException>(
            () => new DeploymentPackageContractReader().ReadFromPackage(package.Path));
        Assert.Equal("ICA006", error.Code);
    }
    [Fact]
    public void InfrastructureContractIsRequiredPackageContent()
        => Assert.Contains(DeploymentPackageLayout.InfrastructureContractFile, DeploymentPackageLayout.ContractFiles);

    private sealed class TestPackage : IDisposable
    {
        private TestPackage(string directory, string path)
        {
            Directory = directory;
            Path = path;
        }

        public string Directory { get; }
        public string Path { get; }

        public static TestPackage Create(bool includeContract = true, bool corruptAfterChecksum = false, string version = "1.0", bool invalidJson = false)
        {
            var directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            System.IO.Directory.CreateDirectory(directory);
            var bundle = ServiceContractBuilder.Build(new ServiceContractBuildRequest
            {
                ServiceId = "gateway",
                DisplayName = "Gateway",
                Version = "1.0.0",
                ImageReference = "aovias/gateway:1.0.0",
                ContainerName = "gateway",
                EntryPoint = "Gateway.dll",
                HealthEndpoint = "/health",
                Ports = [5212]
            });
            ServiceDeploymentBundleIO.WriteDirectory(directory, bundle);

            var built = InfrastructureContractBuilder.Build(bundle);
            var contract = new InfrastructureContractV1
            {
                ServiceId = built.ServiceId,
                ContractVersion = version,
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
            var contractPath = System.IO.Path.Combine(directory, DeploymentPackageLayout.InfrastructureContractFile);
            var checksums = new Dictionary<string, string>();
            if (includeContract)
            {
                if (invalidJson)
                {
                    File.WriteAllText(contractPath, "{ invalid");
                }
                else
                {
                    DeploymentContractJson.WriteFile(contractPath, contract);
                }
                checksums[DeploymentPackageLayout.InfrastructureContractFile] =
                    Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(contractPath))).ToLowerInvariant();
            }

            File.WriteAllText(System.IO.Path.Combine(directory, "checksums.json"), JsonSerializer.Serialize(checksums));
            if (corruptAfterChecksum)
            {
                File.AppendAllText(contractPath, " ");
            }

            var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"contract-{Guid.NewGuid():N}.aovdeploy");
            ZipFile.CreateFromDirectory(directory, path);
            return new TestPackage(directory, path);
        }

        public void Dispose()
        {
            if (File.Exists(Path))
            {
                File.Delete(Path);
            }

            if (System.IO.Directory.Exists(Directory))
            {
                System.IO.Directory.Delete(Directory, recursive: true);
            }
        }
    }
}