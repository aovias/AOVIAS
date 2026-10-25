using Aovias.Deployment.Contracts;
using Aovias.Deployment.Contracts.Serialization;
using Aovias.Deployment.Contracts.Specifications;
using Xunit;

namespace Aovias.DeploymentValidation.Tests;

/// <summary>
/// Sprint M2.1 regression tests for the authoring specification language (D7):
/// specification loading, validation, profile expansion and deterministic compilation.
/// </summary>
public sealed class InfrastructureSpecificationTests
{
    public static IEnumerable<object[]> PlatformServiceIds =>
    [
        ["runtime-agent"], ["gateway"], ["identity-service"], ["organization-service"],
        ["compliance-service"], ["audit-service"], ["ai"], ["knowledge-base"],
        ["portal"], ["outbox-worker"]
    ];

    [Theory]
    [MemberData(nameof(PlatformServiceIds))]
    public void LoadExpanded_every_platform_service_expands_without_error(string serviceId)
    {
        var expanded = InfrastructureSpecificationLoader.LoadExpanded(serviceId);

        Assert.Equal(serviceId, expanded.ServiceId);
        Assert.False(string.IsNullOrWhiteSpace(expanded.Profile));
        Assert.True(expanded.Filesystem.Directories.Count > 0 || expanded.Filesystem.ReadOnlyDirectories.Count > 0);
    }

    [Fact]
    public void Load_unknown_service_falls_back_to_the_default_authoring_profile()
    {
        var specification = InfrastructureSpecificationLoader.Load("brand-new-fixture-service");

        Assert.Equal("brand-new-fixture-service", specification.ServiceId);
        Assert.Equal("http-api", specification.Profile);
    }

    [Fact]
    public void Load_normalizes_service_id_case()
    {
        var upper = InfrastructureSpecificationLoader.Load("AI");
        var lower = InfrastructureSpecificationLoader.Load("ai");

        Assert.Equal("ai", upper.ServiceId);
        Assert.Equal(DeploymentContractJson.Serialize(lower), DeploymentContractJson.Serialize(upper));
    }

    [Fact]
    public void LoadExpanded_is_deterministic_across_independent_loads()
    {
        var first = InfrastructureSpecificationLoader.LoadExpanded("compliance-service");
        var second = InfrastructureSpecificationLoader.LoadExpanded("compliance-service");

        Assert.Equal(DeploymentContractJson.Serialize(first), DeploymentContractJson.Serialize(second));
    }

    [Theory]
    [InlineData("http-api")]
    [InlineData("worker-service")]
    [InlineData("static-web")]
    [InlineData("runtime-agent")]
    public void AuthoringProfiles_all_four_documented_profiles_expand_successfully(string profile)
    {
        var specification = new InfrastructureSpecification
        {
            ServiceId = "fixture-service",
            Profile = profile
        };

        var expanded = InfrastructureAuthoringProfiles.Expand(specification);

        Assert.Equal(profile, expanded.Profile);
        Assert.NotNull(expanded.Health);
    }

    [Fact]
    public void AuthoringProfiles_unknown_profile_throws()
    {
        var specification = new InfrastructureSpecification { ServiceId = "fixture-service", Profile = "unknown-profile" };

        Assert.Throws<InfrastructureSpecificationException>(() => InfrastructureAuthoringProfiles.Expand(specification));
    }

    [Fact]
    public void AuthoringProfiles_unknown_filesystem_template_throws()
    {
        var specification = new InfrastructureSpecification
        {
            ServiceId = "fixture-service",
            Profile = "http-api",
            Filesystem = "no-such-template"
        };

        Assert.Throws<InfrastructureSpecificationException>(() => InfrastructureAuthoringProfiles.Expand(specification));
    }

    [Fact]
    public void Validate_detects_duplicate_filesystem_paths()
    {
        var spec = Baseline(directories:
        [
            new FilesystemDirectoryContract { Path = "/data", Purpose = "a" },
            new FilesystemDirectoryContract { Path = "/data", Purpose = "b" }
        ]);

        var exception = Assert.Throws<InfrastructureSpecificationException>(() => InfrastructureSpecificationValidator.Validate(spec));
        Assert.Contains(exception.Errors, error => error.Contains("Duplicate filesystem path", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_detects_certificate_referencing_unknown_secret()
    {
        var spec = Baseline(certificates:
        [
            new CertificateRequirementContract { Id = "signing", Required = true, SecretKey = "missing-secret" }
        ]);

        var exception = Assert.Throws<InfrastructureSpecificationException>(() => InfrastructureSpecificationValidator.Validate(spec));
        Assert.Contains(exception.Errors, error => error.Contains("references unknown secret", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_detects_volume_target_not_declared_by_filesystem()
    {
        var spec = Baseline(mounts:
        [
            new VolumeMountContract { HostToken = "${TOKEN}", ContainerPath = "/not-declared" }
        ]);

        var exception = Assert.Throws<InfrastructureSpecificationException>(() => InfrastructureSpecificationValidator.Validate(spec));
        Assert.Contains(exception.Errors, error => error.Contains("is not declared by filesystem", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_detects_self_dependency()
    {
        var spec = Baseline();

        var exception = Assert.Throws<InfrastructureSpecificationException>(
            () => InfrastructureSpecificationValidator.Validate(spec, ["fixture-service"]));
        Assert.Contains(exception.Errors, error => error.Contains("cannot depend on itself", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_detects_duplicate_dependencies()
    {
        var spec = Baseline();

        var exception = Assert.Throws<InfrastructureSpecificationException>(
            () => InfrastructureSpecificationValidator.Validate(spec, ["service-a", "service-a"]));
        Assert.Contains(exception.Errors, error => error.Contains("Duplicate dependency", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_detects_invalid_health_definition()
    {
        var spec = Baseline(health: new HealthContract
        {
            HealthUrl = "not-absolute",
            RequiresHttpProbe = true,
            ExpectedStatus = 999,
            TimeoutSeconds = 0,
            Retries = -1,
            StartupTimeoutSeconds = 0
        });

        var exception = Assert.Throws<InfrastructureSpecificationException>(() => InfrastructureSpecificationValidator.Validate(spec));
        Assert.Contains(exception.Errors, error => error.Contains("absolute application path", StringComparison.Ordinal));
        Assert.Contains(exception.Errors, error => error.Contains("expectedStatus", StringComparison.Ordinal));
        Assert.Contains(exception.Errors, error => error.Contains("timeout, retries", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_accepts_a_well_formed_specification()
    {
        var spec = Baseline();

        var exception = Record.Exception(() => InfrastructureSpecificationValidator.Validate(spec, ["upstream-service"]));

        Assert.Null(exception);
    }

    private static ExpandedInfrastructureSpecification Baseline(
        IReadOnlyList<FilesystemDirectoryContract>? directories = null,
        IReadOnlyList<CertificateRequirementContract>? certificates = null,
        IReadOnlyList<SecretRequirementContract>? secrets = null,
        IReadOnlyList<VolumeMountContract>? mounts = null,
        HealthContract? health = null)
        => new()
        {
            ServiceId = "fixture-service",
            Profile = "http-api",
            Filesystem = new FilesystemContract { Directories = directories ?? [new FilesystemDirectoryContract { Path = "/data", Purpose = "data" }] },
            Certificates = new CertificateContract { Certificates = certificates ?? [] },
            Database = new DatabaseContract(),
            Secrets = new SecretContract { Secrets = secrets ?? [] },
            Configuration = new ConfigurationContract(),
            Health = health ?? new HealthContract { HealthUrl = "/health", RequiresHttpProbe = true },
            Network = new NetworkContract(),
            Volumes = new VolumeContract { Mounts = mounts ?? [new VolumeMountContract { HostToken = "${TOKEN}", ContainerPath = "/data" }] },
            Features = new FeatureContract(),
            Runtime = new RuntimeCapabilities(),
            Providers = [],
            PublishedDependencies = true
        };
}
