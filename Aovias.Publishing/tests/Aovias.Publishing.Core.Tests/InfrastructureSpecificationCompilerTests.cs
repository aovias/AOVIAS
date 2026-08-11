using Aovias.Deployment.Contracts;
using Aovias.Deployment.Contracts.Building;
using Aovias.Deployment.Contracts.Infrastructure;
using Aovias.Deployment.Contracts.Migration;
using Aovias.Deployment.Contracts.Serialization;
using Aovias.Deployment.Contracts.Specifications;
using Aovias.Publishing.Core.Generation;
using Aovias.Publishing.Core.Packaging;
using Xunit;

namespace Aovias.Publishing.Core.Tests;

/// <summary>
/// Sprint M2.1 regression tests: golden equivalence between the specification-driven
/// <see cref="InfrastructureSpecificationCompiler"/> and the pre-M2.1 hardcoded
/// <see cref="InfrastructureContractBuilder"/>, plus deterministic-compilation and
/// sole-writer regressions (D6/D7).
///
/// Updated for Sprint M2.2: the compiler now emits the schema v2 envelope (schema version,
/// identity, compatibility, integrity, publishing engine version, source revision, generated
/// UTC) that <see cref="InfrastructureContractBuilder"/> — retained only for older unit tests —
/// never gained. Equivalence is therefore asserted over the domain payload (via the same
/// content-hash mechanism the M2.2 migration framework uses for its own canonical-equality
/// proof) rather than over the full serialized object, since the two paths now intentionally
/// differ in envelope but must never differ in the infrastructure they describe.
///
/// Updated for Sprint M2.3: <see cref="InfrastructureContractBuilder"/> predates the
/// Requirement model entirely and always produces <c>Requirements = []</c>, while the
/// specification-driven compiler now compiles real requirements from
/// <c>infrastructure-specifications.json</c> for every platform service. Comparing full-contract
/// content hashes (which now include <see cref="InfrastructureContract.Requirements"/>, per
/// <see cref="InfrastructureContractIntegrity"/>) would therefore report a false equivalence
/// failure — not a real regression, just two paths disagreeing about a field one of them never
/// knew existed. Golden equivalence is now asserted over the pre-M2.3 domain shape only (via
/// <see cref="InfrastructureContractV1"/>, which structurally excludes Requirements), and
/// Requirements compilation is verified by its own dedicated assertion instead.
/// </summary>
public sealed class InfrastructureSpecificationCompilerTests
{
    private static readonly ContractPublishingMetadata FixedMetadata = new()
    {
        SourceRevision = "test-revision",
        PublishingEngineVersion = "9.9.9-test",
        GeneratedUtc = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)
    };

    public static IEnumerable<object[]> PlatformServices =>
    [
        ["runtime-agent", 7080, new string[0]],
        ["gateway", 5212, new[] { "runtime-agent" }],
        ["identity-service", 5062, new[] { "gateway" }],
        ["organization-service", 8081, new[] { "gateway", "identity-service" }],
        ["compliance-service", 5001, new[] { "gateway", "organization-service" }],
        ["audit-service", 5072, new[] { "gateway", "compliance-service" }],
        ["ai", 5080, new[] { "gateway", "compliance-service" }],
        ["knowledge-base", 5090, new[] { "gateway" }],
        ["portal", 4200, new[] { "gateway" }],
        ["outbox-worker", 0, new[] { "compliance-service" }]
    ];

    [Theory]
    [MemberData(nameof(PlatformServices))]
    public void Compiler_output_is_functionally_equivalent_to_previous_hardcoded_builder(
        string serviceId, int port, string[] dependencies)
    {
        var bundle = BuildBundle(serviceId, port, dependencies);

        var compiled = InfrastructureSpecificationCompiler.Compile(bundle, FixedMetadata);
        var legacy = InfrastructureContractBuilder.Build(bundle);

        // Golden equivalence (D6, M2.1 / D7, M2.2 / updated M2.3): compared over the pre-M2.3
        // domain shape only. InfrastructureContractV1 structurally has no Requirements field, so
        // this hash cannot be perturbed by the compiler now legitimately populating Requirements
        // while the legacy builder (predating the Requirement model) never does.
        Assert.Equal(
            InfrastructureContractIntegrity.ComputeContentHash(ToV1(legacy)),
            InfrastructureContractIntegrity.ComputeContentHash(ToV1(compiled)));

        Assert.Equal(ContractSchemaVersions.Current, compiled.SchemaVersion);
        Assert.Equal(ContractSchemaVersions.ContractVersionV2, compiled.ContractVersion);
        Assert.Null(compiled.Migration);

        // Sprint M2.3 D7: requirements are compiled from the specification catalog, unresolved,
        // verbatim — the compiler must neither invent nor drop any of them.
        var expectedRequirements = InfrastructureSpecificationLoader.LoadExpanded(serviceId).Requirements;
        Assert.Equal(DeploymentContractJson.Serialize(expectedRequirements), DeploymentContractJson.Serialize(compiled.Requirements));
        Assert.Empty(legacy.Requirements);
    }

    private static InfrastructureContractV1 ToV1(InfrastructureContract contract) => new()
    {
        ServiceId = contract.ServiceId,
        ContractVersion = ContractSchemaVersions.ContractVersionV1,
        Filesystem = contract.Filesystem,
        Certificates = contract.Certificates,
        Secrets = contract.Secrets,
        Database = contract.Database,
        Docker = contract.Docker,
        Network = contract.Network,
        Storage = contract.Storage,
        Providers = contract.Providers,
        Dependencies = contract.Dependencies,
        Health = contract.Health
    };

    [Theory]
    [MemberData(nameof(PlatformServices))]
    public void Compiler_is_deterministic_across_independent_specification_loads(
        string serviceId, int port, string[] dependencies)
    {
        var bundle = BuildBundle(serviceId, port, dependencies);

        var first = InfrastructureSpecificationCompiler.Compile(bundle, FixedMetadata);
        var second = InfrastructureSpecificationCompiler.Compile(bundle, FixedMetadata);

        Assert.Equal(DeploymentContractJson.Serialize(first), DeploymentContractJson.Serialize(second));
    }

    [Fact]
    public void Publisher_writes_exactly_what_the_compiler_produces()
    {
        var bundle = BuildBundle("compliance-service", 5001, ["gateway", "organization-service"]);
        var expected = InfrastructureSpecificationCompiler.Compile(bundle, FixedMetadata);

        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var written = InfrastructureContractPublisher.Write(directory, bundle, buildIdentity: null);

            // Write() derives its own metadata via ContractPublishingMetadata.FromBuildIdentity(null),
            // so compare schema-relevant invariants rather than the exact source revision/engine version.
            Assert.Equal(ContractSchemaVersions.Current, written.SchemaVersion);
            Assert.Equal(
                InfrastructureContractIntegrity.ComputeContentHash(expected),
                InfrastructureContractIntegrity.ComputeContentHash(written));

            var path = Path.Combine(directory, DeploymentPackageLayout.InfrastructureContractFile);
            Assert.True(File.Exists(path));

            var fromDisk = DeploymentContractJson.ReadFile<InfrastructureContract>(path);
            Assert.Equal(DeploymentContractJson.Serialize(written), DeploymentContractJson.Serialize(fromDisk));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Compiler_carries_declared_providers_for_ai_without_hardcoded_service_check()
    {
        var bundle = BuildBundle("ai", 5080, ["gateway", "compliance-service"]);

        var compiled = InfrastructureSpecificationCompiler.Compile(bundle, FixedMetadata);

        Assert.Equal(2, compiled.Providers.Count);
        Assert.Contains(compiled.Providers, provider => provider.Id == "azure-openai");
        Assert.Contains(compiled.Providers, provider => provider.Id == "sarvam");
    }

    private static ServiceDeploymentBundle BuildBundle(string serviceId, int port, IReadOnlyList<string> dependencies)
        => ServiceContractBuilder.Build(new ServiceContractBuildRequest
        {
            ServiceId = serviceId,
            DisplayName = serviceId,
            Version = "1.0.0",
            ImageReference = $"aovias/{serviceId}:1.0.0",
            ContainerName = serviceId,
            EntryPoint = "App.dll",
            HealthEndpoint = "/health",
            Ports = port > 0 ? [port] : [],
            Dependencies = dependencies
        });
}
