using Aovias.Deployment.Contracts;
using Aovias.Deployment.Contracts.Building;
using Aovias.Deployment.Contracts.Capabilities;
using Aovias.Deployment.Contracts.Infrastructure;
using Aovias.Deployment.Contracts.Infrastructure.Provisioners;
using Aovias.Deployment.Contracts.Planning;
using Aovias.Deployment.Contracts.Provisioning;
using Aovias.Deployment.Contracts.Serialization;
using Aovias.Deployment.Contracts.Specifications;
using Xunit;

namespace Aovias.DeploymentValidation.Tests;

/// <summary>
/// Sprint M2.3 D10 regression suite for the Requirement/Capability infrastructure model:
/// domain serialization, the capability registry, version constraint evaluation, the matching
/// engine, requirement validation, deployment-planner integration (D5), and the legacy-boolean
/// bridge used by the shared provisioning pipeline stage (D8).
/// </summary>
public sealed class RequirementCapabilityModelTests
{
    // ---- Serialization (round-trip through the shared DeploymentContractJson options) ----

    [Fact]
    public void Requirement_round_trips_through_json_with_all_fields_preserved()
    {
        var requirement = new Requirement
        {
            Id = "primary-database",
            Category = RequirementCategories.SqlDatabase,
            Mandatory = true,
            VersionConstraint = ">=2019.0",
            Description = "Primary relational store.",
            ValidationRules = ["connection-string-required"],
            Compatibility = new RequirementCompatibilityMetadata { AcceptedCategories = ["persistent-storage"] }
        };

        var json = DeploymentContractJson.Serialize(requirement);
        var restored = DeploymentContractJson.Deserialize<Requirement>(json);

        Assert.NotNull(restored);
        Assert.Equal(requirement.Id, restored!.Id);
        Assert.Equal(requirement.Category, restored.Category);
        Assert.Equal(requirement.Mandatory, restored.Mandatory);
        Assert.Equal(requirement.VersionConstraint, restored.VersionConstraint);
        Assert.Equal(requirement.Description, restored.Description);
        Assert.Equal(requirement.ValidationRules, restored.ValidationRules);
        Assert.Equal(requirement.Compatibility.AcceptedCategories, restored.Compatibility.AcceptedCategories);
    }

    [Fact]
    public void Capability_round_trips_through_json_with_all_fields_preserved()
    {
        var capability = new Capability
        {
            Id = "postgresql",
            Category = "sql-database",
            SatisfiesCategories = ["persistent-storage"],
            Version = "16.0.0",
            Aliases = ["postgres", "psql"],
            ProviderMapping = null,
            DependsOnCapabilityIds = ["docker-network"],
            Description = "PostgreSQL."
        };

        var json = DeploymentContractJson.Serialize(capability);
        var restored = DeploymentContractJson.Deserialize<Capability>(json);

        Assert.NotNull(restored);
        Assert.Equal(capability.Id, restored!.Id);
        Assert.Equal(capability.Category, restored.Category);
        Assert.Equal(capability.SatisfiesCategories, restored.SatisfiesCategories);
        Assert.Equal(capability.Version, restored.Version);
        Assert.Equal(capability.Aliases, restored.Aliases);
        Assert.Equal(capability.DependsOnCapabilityIds, restored.DependsOnCapabilityIds);
    }

    // ---- Capability registry (D3) ----

    [Fact]
    public void CapabilityRegistry_LoadsWithoutDuplicatesOrCycles()
    {
        // The registry validates itself at load time (LoadAndValidate); simply touching .All
        // proves the embedded catalog parses and passes those checks.
        Assert.NotEmpty(CapabilityRegistry.All);
    }

    [Fact]
    public void CapabilityRegistry_FindById_ResolvesByAliasToo()
    {
        var byPrimaryId = CapabilityRegistry.FindById("postgresql");
        var byAlias = CapabilityRegistry.FindById("psql");

        Assert.NotNull(byPrimaryId);
        Assert.NotNull(byAlias);
        Assert.Same(byPrimaryId, byAlias);
    }

    [Fact]
    public void CapabilityRegistry_FindByCategory_IncludesSatisfiesCategoriesMatches()
    {
        var eventStreamCapable = CapabilityRegistry.FindByCategory("event-stream");

        Assert.Contains(eventStreamCapable, c => c.Id == "rabbitmq");
        Assert.Contains(eventStreamCapable, c => c.Id == "azure-service-bus");
    }

    [Fact]
    public void CapabilityRegistry_IsKnownCategory_FalseForNonsenseCategory()
    {
        Assert.False(CapabilityRegistry.IsKnownCategory("not-a-real-category"));
        Assert.True(CapabilityRegistry.IsKnownCategory(RequirementCategories.MessageBroker));
    }

    [Fact]
    public void CapabilityRegistry_DetectCircularDependencies_FindsASyntheticCycle()
    {
        var a = new Capability { Id = "a", Category = "x", DependsOnCapabilityIds = ["b"] };
        var b = new Capability { Id = "b", Category = "x", DependsOnCapabilityIds = ["a"] };

        var cycle = CapabilityRegistry.DetectCircularDependencies([a, b]);

        Assert.NotEmpty(cycle);
    }

    [Fact]
    public void CapabilityRegistry_DetectCircularDependencies_EmptyForAcyclicGraph()
    {
        var cycle = CapabilityRegistry.DetectCircularDependencies(CapabilityRegistry.All);
        Assert.Empty(cycle);
    }

    // ---- Version constraints (D1/D4) ----

    [Theory]
    [InlineData(null, "1.0.0", true)]
    [InlineData(">=2019.0", "2022.0.0", true)]
    [InlineData(">=2019.0", "2016.0.0", false)]
    [InlineData("<=16.0", "16.0.0", true)]
    [InlineData("<=16.0", "17.0.0", false)]
    [InlineData(">1.0.0", "1.0.1", true)]
    [InlineData(">1.0.0", "1.0.0", false)]
    [InlineData("<2.0.0", "1.9.9", true)]
    [InlineData("==3.13.0", "3.13.0", true)]
    [InlineData("==3.13.0", "3.13.1", false)]
    [InlineData("3.13.0", "3.13.0", true)]
    [InlineData("^8.0", "8.5.0", true)]
    [InlineData("^8.0", "9.0.0", false)]
    [InlineData(">=1.0", "not-a-version", false)]
    public void VersionConstraint_IsSatisfiedBy_EvaluatesEveryOperator(string? constraint, string candidate, bool expected)
    {
        Assert.Equal(expected, VersionConstraint.IsSatisfiedBy(constraint, candidate));
    }

    // ---- Requirement-capability matcher (D4/D9) ----

    [Fact]
    public void Matcher_Satisfied_WhenCompatibleCapabilityIsAvailable()
    {
        var requirements = new List<Requirement>
        {
            new() { Id = "primary-database", Category = RequirementCategories.SqlDatabase, Mandatory = true, VersionConstraint = ">=2019.0" }
        };
        var capabilities = new List<Capability> { CapabilityRegistry.FindById("sqlserver")! };

        var report = RequirementCapabilityMatcher.Evaluate(requirements, capabilities);

        Assert.True(report.CanDeploy);
        Assert.Empty(report.Blockers);
        Assert.Single(report.Results);
        Assert.Equal(RequirementMatchStatus.Satisfied, report.Results[0].Status);
        Assert.Equal("sqlserver", report.Results[0].MatchedCapability!.Id);
    }

    [Fact]
    public void Matcher_Missing_BlocksDeploymentAndSuggestsRemediation()
    {
        var requirements = new List<Requirement>
        {
            new() { Id = "primary-database", Category = RequirementCategories.SqlDatabase, Mandatory = true }
        };

        var report = RequirementCapabilityMatcher.Evaluate(requirements, []);

        Assert.False(report.CanDeploy);
        Assert.Single(report.Blockers);
        Assert.Equal(RequirementMatchStatus.Missing, report.Results[0].Status);
        Assert.Contains("Suggested remediation", report.Blockers[0]);
    }

    [Fact]
    public void Matcher_IncompatibleVersion_BlocksDeploymentWithUpgradeGuidance()
    {
        var requirements = new List<Requirement>
        {
            new() { Id = "primary-database", Category = RequirementCategories.SqlDatabase, Mandatory = true, VersionConstraint = ">=2025.0" }
        };
        var capabilities = new List<Capability> { CapabilityRegistry.FindById("sqlserver")! };

        var report = RequirementCapabilityMatcher.Evaluate(requirements, capabilities);

        Assert.False(report.CanDeploy);
        Assert.Equal(RequirementMatchStatus.IncompatibleVersion, report.Results[0].Status);
        Assert.Contains("upgrade", report.Blockers[0]);
    }

    [Fact]
    public void Matcher_OptionalRequirement_MissingProducesWarningNotBlocker()
    {
        var requirements = new List<Requirement>
        {
            new() { Id = "ai-provider", Category = RequirementCategories.ExternalAiProvider, Mandatory = false }
        };

        var report = RequirementCapabilityMatcher.Evaluate(requirements, []);

        Assert.True(report.CanDeploy);
        Assert.Empty(report.Blockers);
        Assert.Single(report.Warnings);
    }

    [Fact]
    public void Matcher_AcceptedCategories_AllowsAlternateCategoryMatch()
    {
        var requirements = new List<Requirement>
        {
            new()
            {
                Id = "event-broker",
                Category = "event-stream",
                Mandatory = true,
                Compatibility = new RequirementCompatibilityMetadata { AcceptedCategories = [RequirementCategories.MessageBroker] }
            }
        };
        var capabilities = new List<Capability> { CapabilityRegistry.FindById("rabbitmq")! };

        var report = RequirementCapabilityMatcher.Evaluate(requirements, capabilities);

        Assert.True(report.CanDeploy);
        Assert.Equal(RequirementMatchStatus.Satisfied, report.Results[0].Status);
    }

    [Fact]
    public void Matcher_EmptyRequirements_IsBenignAndDeployable()
    {
        var report = RequirementCapabilityMatcher.Evaluate([], []);

        Assert.True(report.CanDeploy);
        Assert.Empty(report.Results);
        Assert.Empty(report.Blockers);
        Assert.Empty(report.Warnings);
    }

    [Fact]
    public void Matcher_IsDeterministic_AcrossRepeatedEvaluations()
    {
        var requirements = InfrastructureSpecificationLoader.LoadExpanded("compliance-service").Requirements;
        var capabilities = EnvironmentCapabilityProfileLoader.ResolveCapabilities("production-vps");

        var first = RequirementCapabilityMatcher.Evaluate(requirements, capabilities);
        var second = RequirementCapabilityMatcher.Evaluate(requirements, capabilities);

        var firstSignature = first.Results.Select(r => $"{r.Requirement.Id}:{r.Status}:{r.MatchedCapability?.Id}").ToList();
        var secondSignature = second.Results.Select(r => $"{r.Requirement.Id}:{r.Status}:{r.MatchedCapability?.Id}").ToList();

        Assert.Equal(firstSignature, secondSignature);
        Assert.Equal(first.CanDeploy, second.CanDeploy);
    }

    // ---- Requirement validation (D9) ----

    [Fact]
    public void RequirementValidator_Throws_ForUnknownCategory()
    {
        var requirements = new List<Requirement> { new() { Id = "x", Category = "totally-made-up-category" } };

        var exception = Assert.Throws<RequirementValidationException>(() => RequirementValidator.Validate(requirements));
        Assert.Contains(exception.Errors, e => e.Contains("unknown category"));
    }

    [Fact]
    public void RequirementValidator_Throws_ForDuplicateIds()
    {
        var requirements = new List<Requirement>
        {
            new() { Id = "dup", Category = RequirementCategories.SqlDatabase },
            new() { Id = "dup", Category = RequirementCategories.MessageBroker }
        };

        var exception = Assert.Throws<RequirementValidationException>(() => RequirementValidator.Validate(requirements));
        Assert.Contains(exception.Errors, e => e.Contains("Duplicate requirement id"));
    }

    [Fact]
    public void RequirementValidator_Throws_ForConflictingExactVersionsInSameCategory()
    {
        var requirements = new List<Requirement>
        {
            new() { Id = "a", Category = RequirementCategories.SqlDatabase, Mandatory = true, VersionConstraint = "==2019.0" },
            new() { Id = "b", Category = RequirementCategories.SqlDatabase, Mandatory = true, VersionConstraint = "==2022.0" }
        };

        var exception = Assert.Throws<RequirementValidationException>(() => RequirementValidator.Validate(requirements));
        Assert.Contains(exception.Errors, e => e.Contains("incompatible exact versions"));
    }

    [Fact]
    public void RequirementValidator_Passes_ForEveryPlatformServiceInTheCatalog()
    {
        foreach (var serviceId in new[]
                 {
                     "gateway", "identity-service", "organization-service", "compliance-service",
                     "audit-service", "ai", "knowledge-base", "portal", "outbox-worker", "runtime-agent"
                 })
        {
            var specification = InfrastructureSpecificationLoader.LoadExpanded(serviceId);
            RequirementValidator.Validate(specification.Requirements); // throws on failure
        }
    }

    // ---- Environment capability profiles (D6) ----

    [Fact]
    public void EnvironmentCapabilityProfileLoader_ResolvesEveryDeclaredProfile()
    {
        foreach (var profile in EnvironmentCapabilityProfileLoader.All)
        {
            var resolved = EnvironmentCapabilityProfileLoader.ResolveCapabilities(profile.Id);
            Assert.Equal(profile.CapabilityIds.Count, resolved.Count);
        }
    }

    [Fact]
    public void AirGappedProfile_ExcludesAzureCapabilities()
    {
        var resolved = EnvironmentCapabilityProfileLoader.ResolveCapabilities("air-gapped");
        Assert.DoesNotContain(resolved, c => c.Id.StartsWith("azure-", StringComparison.OrdinalIgnoreCase));
    }

    // ---- Deployment planner integration (D5) ----

    [Fact]
    public void DeploymentPlanner_FourArgOverload_BlocksWhenMandatoryRequirementUnmet()
    {
        var bundle = BuildBundle("identity-service", 5062, []);
        var requirements = InfrastructureSpecificationLoader.LoadExpanded("identity-service").Requirements;

        var evaluation = DeploymentPlanner.Evaluate(bundle, bundle.RuntimeCapabilities, requirements, availableCapabilities: []);

        Assert.False(evaluation.CanDeploy);
        Assert.NotNull(evaluation.RequirementMatch);
        Assert.False(evaluation.RequirementMatch!.CanDeploy);
        Assert.NotEmpty(evaluation.Blockers);
    }

    [Fact]
    public void DeploymentPlanner_FourArgOverload_DeploysWhenCapabilitiesSatisfyRequirements()
    {
        var bundle = BuildBundle("identity-service", 5062, []);
        var requirements = InfrastructureSpecificationLoader.LoadExpanded("identity-service").Requirements;
        var capabilities = EnvironmentCapabilityProfileLoader.ResolveCapabilities("production-vps");

        var runtimeCapabilities = new RuntimeCapabilities { Docker = true, SqlServer = true, Certificates = true };
        var evaluation = DeploymentPlanner.Evaluate(bundle, runtimeCapabilities, requirements, capabilities);

        Assert.True(evaluation.CanDeploy, string.Join("; ", evaluation.Blockers));
        Assert.NotNull(evaluation.RequirementMatch);
        Assert.True(evaluation.RequirementMatch!.CanDeploy);
    }

    [Fact]
    public void DeploymentPlanner_TwoArgLegacyOverload_LeavesRequirementMatchNull()
    {
        var bundle = BuildBundle("gateway", 5212, ["runtime-agent"]);

        var evaluation = DeploymentPlanner.Evaluate(bundle, bundle.RuntimeCapabilities);

        Assert.Null(evaluation.RequirementMatch);
    }

    // ---- Legacy capability bridge + shared provisioning stage (D8) ----

    [Fact]
    public void LegacyCapabilityBridge_MapsEnabledBooleanFlagsToRegisteredCapabilities()
    {
        var runtimeCapabilities = new RuntimeCapabilities { SqlServer = true, RabbitMq = true, Certificates = true };

        var bridged = LegacyCapabilityBridge.FromRuntimeCapabilities(runtimeCapabilities);

        Assert.Contains(bridged, c => c.Id == "sqlserver");
        Assert.Contains(bridged, c => c.Id == "rabbitmq");
        Assert.Contains(bridged, c => c.Id == "tls-file-certificates");
        Assert.DoesNotContain(bridged, c => c.Id == "azure-service-bus");
    }

    [Fact]
    public void LegacyCapabilityBridge_DoesNotBridgeRedis_DocumentedGap()
    {
        // Sprint G1: first real execution of this test. RuntimeCapabilities.Docker defaults to
        // true (LegacyCapabilityBridge.cs comment: "the platform's baseline runtime engine"), so
        // isolating the Redis-specific gap requires explicitly disabling it — otherwise
        // docker-network/http-health-probe are always bridged regardless of Redis.
        var runtimeCapabilities = new RuntimeCapabilities { Redis = true, Docker = false };
        var bridged = LegacyCapabilityBridge.FromRuntimeCapabilities(runtimeCapabilities);

        Assert.Empty(bridged);
    }

    [Fact]
    public async Task DependencyProvisioner_UsesBridgedCapabilities_WhenContextDoesNotPopulateThemExplicitly()
    {
        var bundle = BuildBundle("identity-service", 5062, ["gateway"]);
        var contract = new InfrastructureContract
        {
            ServiceId = bundle.Manifest.ServiceId,
            Dependencies = bundle.Dependency,
            Requirements = InfrastructureSpecificationLoader.LoadExpanded("identity-service").Requirements
        };

        var context = new InfrastructureProvisioningContext
        {
            Bundle = bundle,
            Contract = contract,
            HostPaths = new HostPathContext
            {
                HostConfigRoot = Path.GetTempPath(),
                HostLogsRoot = Path.GetTempPath(),
                HostPackagesRoot = Path.GetTempPath()
            },
            RuntimeCapabilities = new RuntimeCapabilities { Docker = true, SqlServer = true, Certificates = true }
        };

        var result = await new DependencyInfrastructureProvisioner().ProvisionAsync(context);

        Assert.True(result.Passed, string.Join("; ", result.Errors));
    }

    [Fact]
    public async Task DependencyProvisioner_Fails_WhenNeitherContextNorBridgeSatisfiesMandatoryRequirement()
    {
        var bundle = BuildBundle("identity-service", 5062, ["gateway"]);
        var contract = new InfrastructureContract
        {
            ServiceId = bundle.Manifest.ServiceId,
            Dependencies = bundle.Dependency,
            Requirements = InfrastructureSpecificationLoader.LoadExpanded("identity-service").Requirements
        };

        var context = new InfrastructureProvisioningContext
        {
            Bundle = bundle,
            Contract = contract,
            HostPaths = new HostPathContext
            {
                HostConfigRoot = Path.GetTempPath(),
                HostLogsRoot = Path.GetTempPath(),
                HostPackagesRoot = Path.GetTempPath()
            },
            RuntimeCapabilities = new RuntimeCapabilities { Docker = true } // no SqlServer, no Certificates
        };

        var result = await new DependencyInfrastructureProvisioner().ProvisionAsync(context);

        Assert.False(result.Passed);
        Assert.NotEmpty(result.Errors);
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
