using Aovias.Publishing.Contracts;
using Aovias.Publishing.Contracts.Verification;
using Aovias.Publishing.Core.Verification;
using Xunit;

namespace Aovias.Publishing.Core.Tests.Verification;

/// <summary>
/// Sprint M3.3 regression tests for the manifest identity consistency check.
/// </summary>
public sealed class PackageManifestConsistencyValidatorTests
{
    [Fact]
    public void Validate_passes_when_manifest_identity_and_checksums_match_expectations()
    {
        var contents = CreateContents(service: "gateway", version: "1.2.3", checksumCount: 3, entryPoint: "gateway.dll");

        var status = PackageManifestConsistencyValidator.Validate(contents, expectedServiceId: "gateway", expectedVersion: "1.2.3");

        Assert.Equal(VerificationOutcome.Passed, status.Outcome);
        Assert.Equal(VerificationEvidenceTier.StaticVerification, status.EvidenceTier);
    }

    [Fact]
    public void Validate_is_case_insensitive_for_service_and_version()
    {
        var contents = CreateContents(service: "Gateway", version: "1.2.3", checksumCount: 1, entryPoint: "gateway.dll");

        var status = PackageManifestConsistencyValidator.Validate(contents, expectedServiceId: "gateway", expectedVersion: "1.2.3");

        Assert.True(status.IsPassed);
    }

    [Fact]
    public void Validate_fails_when_manifest_service_does_not_match_expected_service()
    {
        var contents = CreateContents(service: "wrong-service", version: "1.2.3", checksumCount: 1, entryPoint: "gateway.dll");

        var status = PackageManifestConsistencyValidator.Validate(contents, expectedServiceId: "gateway", expectedVersion: "1.2.3");

        Assert.Equal(VerificationOutcome.Failed, status.Outcome);
        Assert.Contains("service", status.RootCause, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_fails_when_manifest_version_does_not_match_expected_version()
    {
        var contents = CreateContents(service: "gateway", version: "9.9.9", checksumCount: 1, entryPoint: "gateway.dll");

        var status = PackageManifestConsistencyValidator.Validate(contents, expectedServiceId: "gateway", expectedVersion: "1.2.3");

        Assert.Equal(VerificationOutcome.Failed, status.Outcome);
        Assert.Contains("version", status.RootCause, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_fails_when_package_has_zero_checksums()
    {
        var contents = CreateContents(service: "gateway", version: "1.2.3", checksumCount: 0, entryPoint: "gateway.dll");

        var status = PackageManifestConsistencyValidator.Validate(contents, expectedServiceId: "gateway", expectedVersion: "1.2.3");

        Assert.Equal(VerificationOutcome.Failed, status.Outcome);
        Assert.Contains("checksum", status.RootCause, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_fails_when_manifest_has_no_entry_point()
    {
        var contents = CreateContents(service: "gateway", version: "1.2.3", checksumCount: 1, entryPoint: "");

        var status = PackageManifestConsistencyValidator.Validate(contents, expectedServiceId: "gateway", expectedVersion: "1.2.3");

        Assert.Equal(VerificationOutcome.Failed, status.Outcome);
        Assert.Contains("entry point", status.RootCause, StringComparison.OrdinalIgnoreCase);
    }

    private static DeployPackageContents CreateContents(string service, string version, int checksumCount, string entryPoint)
    {
        var checksums = new Dictionary<string, string>();
        for (var i = 0; i < checksumCount; i++)
        {
            checksums[$"file{i}.bin"] = $"sha256:{i:x8}";
        }

        return new DeployPackageContents
        {
            PackagePath = $"/out/{service}-{version}.aovdeploy",
            Manifest = new DeployManifest
            {
                Service = service,
                Version = version,
                Type = "AspNetCore",
                Container = service,
                Health = "/health",
                EntryPoint = entryPoint,
                Configuration = "config.json",
                ImageName = service,
                ImageTag = version
            },
            Health = new DeployHealthDefinition { Endpoint = "/health" },
            Metadata = new DeployPackageMetadata
            {
                Publisher = "test",
                PublishingEngineVersion = "1.0.0",
                PackageVersion = version,
                PackageType = "Service",
                CreatedUtc = DateTimeOffset.UtcNow
            },
            Checksums = checksums,
            ConfigurationTemplate = new Dictionary<string, string>()
        };
    }
}
