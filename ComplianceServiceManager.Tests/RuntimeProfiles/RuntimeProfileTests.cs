using System.Collections.ObjectModel;
using ComplianceServiceManager.Logging;
using ComplianceServiceManager.Models;
using ComplianceServiceManager.Models.RuntimeProfiles;
using ComplianceServiceManager.Services;
using ComplianceServiceManager.Services.RuntimeProfiles;

namespace ComplianceServiceManager.Tests.RuntimeProfiles;

public sealed class RuntimeProfileValidatorTests
{
    private readonly RuntimeProfileValidator validator = new();

    [Fact]
    public void Validate_SeedWebDevelopmentProfile_IsValidAndOrdersDeps()
    {
        var profile = RuntimeProfileRegistry.BuildDefaults().Profiles[0];
        var discovered = CreateDiscovered(
            "IdentityService",
            "OrganizationService",
            "ComplianceService",
            "Gateway",
            "WebApp");

        var result = validator.Validate(profile, discovered);

        Assert.True(result.IsValid);
        Assert.Equal(
            ["IdentityService", "OrganizationService", "ComplianceService", "Gateway", "WebApp"],
            result.StartupOrder.ToArray());
        Assert.True(IndexOf(result, "OrganizationService") < IndexOf(result, "ComplianceService"));
        Assert.True(IndexOf(result, "ComplianceService") < IndexOf(result, "Gateway"));
        Assert.True(IndexOf(result, "IdentityService") < IndexOf(result, "Gateway"));
        Assert.True(IndexOf(result, "Gateway") < IndexOf(result, "WebApp"));
    }

    [Fact]
    public void Validate_UnknownService_ProducesExactlyOneIssue()
    {
        var profile = Profile("A", ("MissingService", []));
        var result = validator.Validate(profile, CreateDiscovered("Gateway"));

        Assert.False(result.IsValid);
        Assert.Single(result.Issues);
        Assert.Equal("unknown-service", result.Issues[0].Code);
    }

    [Fact]
    public void Validate_EmptyDiscovery_ProducesSingleDiscoveryEmptyIssue()
    {
        var profile = RuntimeProfileRegistry.BuildDefaults().Profiles[0];
        var result = validator.Validate(profile, []);

        Assert.False(result.IsValid);
        Assert.Single(result.Issues);
        Assert.Equal("discovery-empty", result.Issues[0].Code);
        Assert.DoesNotContain(result.Issues, issue => issue.Code == "unknown-service");
    }

    [Fact]
    public void Validate_FiveUnknownServices_ProducesIssueCountFive()
    {
        var profile = RuntimeProfileRegistry.BuildDefaults().Profiles[0];
        var result = validator.Validate(profile, CreateDiscovered("UnrelatedService"));

        Assert.False(result.IsValid);
        Assert.Equal(5, result.Issues.Count);
        Assert.All(result.Issues, issue => Assert.Equal("unknown-service", issue.Code));
    }

    [Fact]
    public void Validate_MatchesCanonicalServiceInfoModelName_NotPathOrKind()
    {
        var profile = RuntimeProfileRegistry.BuildDefaults().Profiles[0];
        var discovered = new List<ServiceInfoModel>
        {
            new() { Name = "IdentityService", ProjectPath = @"C:\IdentityService\IdentityService.csproj", Kind = ServiceKind.WebApi },
            new() { Name = "OrganizationService", ProjectPath = @"C:\OrganizationService\OrganizationService.csproj", Kind = ServiceKind.WebApi },
            new() { Name = "ComplianceService", ProjectPath = @"C:\ComplianceService\ComplianceService.csproj", Kind = ServiceKind.WebApi },
            new() { Name = "Gateway", ProjectPath = @"C:\Gateway\Gateway.csproj", Kind = ServiceKind.WebApi },
            new() { Name = "WebApp", ProjectPath = @"C:\WebApp\package.json", Kind = ServiceKind.AngularWebApp }
        };

        var result = validator.Validate(profile, discovered);
        Assert.True(result.IsValid, string.Join("; ", result.Issues.Select(i => i.Message)));
    }

    [Fact]
    public void Validate_WrongDiscoveredName_DoesNotMatchEvenWhenPathLooksRight()
    {
        var profile = Profile("A", ("IdentityService", []));
        var discovered = new List<ServiceInfoModel>
        {
            new() { Name = "Identity", ProjectPath = @"C:\IdentityService\IdentityService.csproj" }
        };

        var result = validator.Validate(profile, discovered);
        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, issue => issue.Code == "unknown-service");
    }

    [Fact]
    public void Validate_UnknownService_IsInvalid()
    {
        var profile = Profile("A", ("MissingService", []));
        var result = validator.Validate(profile, CreateDiscovered("Gateway"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, issue => issue.Code == "unknown-service");
    }

    [Fact]
    public void Validate_MissingDependency_IsInvalid()
    {
        var profile = Profile("A", ("WebApp", ["Gateway"]));
        var result = validator.Validate(profile, CreateDiscovered("WebApp"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, issue => issue.Code == "missing-dependency");
    }

    [Fact]
    public void Validate_CircularDependency_IsInvalid()
    {
        var profile = Profile("Cycle", ("A", ["B"]), ("B", ["A"]));
        var result = validator.Validate(profile, CreateDiscovered("A", "B"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, issue => issue.Code == "circular-dependency");
    }

    [Fact]
    public void Validate_DuplicateService_IsInvalid()
    {
        var profile = new RuntimeProfileDefinition
        {
            Name = "Dupes",
            Services =
            [
                new RuntimeProfileServiceEntry { ServiceName = "Gateway", Enabled = true },
                new RuntimeProfileServiceEntry { ServiceName = "Gateway", Enabled = true }
            ]
        };

        var result = validator.Validate(profile, CreateDiscovered("Gateway"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, issue => issue.Code == "duplicate-service");
    }

    [Fact]
    public void TryTopologicalSort_ProducesDeterministicOrder()
    {
        var entries = new[]
        {
            new RuntimeProfileServiceEntry { ServiceName = "C", DependsOn = ["A", "B"] },
            new RuntimeProfileServiceEntry { ServiceName = "B", DependsOn = ["A"] },
            new RuntimeProfileServiceEntry { ServiceName = "A", DependsOn = [] }
        };

        var (order, cycle) = RuntimeProfileValidator.TryTopologicalSort(entries);

        Assert.Null(cycle);
        Assert.Equal(["A", "B", "C"], order.ToArray());
    }

    private static int IndexOf(ProfileValidationResult result, string name)
        => result.StartupOrder
            .Select((value, index) => (value, index))
            .First(pair => string.Equals(pair.value, name, StringComparison.OrdinalIgnoreCase))
            .index;

    private static RuntimeProfileDefinition Profile(string name, params (string Service, string[] DependsOn)[] services)
        => new()
        {
            Name = name,
            Services = services.Select(item => new RuntimeProfileServiceEntry
            {
                ServiceName = item.Service,
                Enabled = true,
                DependsOn = item.DependsOn.ToList()
            }).ToList()
        };

    private static List<ServiceInfoModel> CreateDiscovered(params string[] names)
        => names.Select(name => new ServiceInfoModel { Name = name, ProjectPath = $@"C:\{name}" }).ToList();
}

public sealed class RuntimeProfileRegistryTests
{
    [Fact]
    public void Registry_RoundTripsSeedAndCustomProfile()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "aovias-runtime-profiles-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        var storePath = Path.Combine(tempRoot, "runtime-profiles.json");

        try
        {
            var registry = new RuntimeProfileRegistry(storePath);
            var seeded = Assert.Single(registry.GetProfiles());
            Assert.Equal("AOVIAS Web Development", seeded.Name);
            Assert.Equal(5, seeded.Services.Count);

            registry.Upsert(new RuntimeProfileDefinition
            {
                Id = "custom-1",
                Name = "Custom",
                Description = "Test",
                MaxParallelStarts = 3,
                Services =
                [
                    new RuntimeProfileServiceEntry
                    {
                        ServiceName = "WebApp",
                        Enabled = true,
                        DependsOn = ["Gateway"]
                    },
                    new RuntimeProfileServiceEntry
                    {
                        ServiceName = "Gateway",
                        Enabled = true
                    }
                ]
            });

            var reloaded = new RuntimeProfileRegistry(storePath).GetProfiles();
            Assert.Equal(2, reloaded.Count);
            var custom = Assert.Single(reloaded, profile => profile.Id == "custom-1");
            Assert.Equal(3, custom.MaxParallelStarts);
            Assert.Contains(custom.Services, entry => entry.ServiceName == "WebApp" && entry.DependsOn.Contains("Gateway"));
        }
        finally
        {
            try
            {
                Directory.Delete(tempRoot, recursive: true);
            }
            catch
            {
            }
        }
    }
}

public sealed class RuntimeProfileOrchestratorTests
{
    [Fact]
    public async Task Start_ReusesAlreadyRunning_AndDoesNotOwnThem()
    {
        var host = new FakeServiceRuntimeHost();
        host.Services.Add(Service("IdentityService", ServiceStatus.Running));
        host.Services.Add(Service("Gateway", ServiceStatus.Stopped));

        var orchestrator = CreateOrchestrator(host, out var storePath);
        try
        {
            var profile = ProfileItem(
                ("IdentityService", []),
                ("Gateway", ["IdentityService"]));

            await orchestrator.StartAsync(profile);

            Assert.DoesNotContain("IdentityService", profile.OwnedServiceNames);
            Assert.Contains("Gateway", profile.OwnedServiceNames);
            Assert.Single(host.StartCalls);
            Assert.Equal("Gateway", host.StartCalls[0]);
            Assert.Equal(RuntimeProfileMemberStatus.Reused, profile.Members.Single(m => m.ServiceName == "IdentityService").Status);
            Assert.Equal(RuntimeProfileMemberStatus.Ready, profile.Members.Single(m => m.ServiceName == "Gateway").Status);
            Assert.Equal(RuntimeProfileStatus.Running, profile.Status);
        }
        finally
        {
            Cleanup(storePath);
        }
    }

    [Fact]
    public async Task Start_FailureBlocksDependents()
    {
        var host = new FakeServiceRuntimeHost
        {
            FailOnStart = { "OrganizationService" }
        };
        host.Services.Add(Service("OrganizationService"));
        host.Services.Add(Service("ComplianceService"));

        var orchestrator = CreateOrchestrator(host, out var storePath);
        try
        {
            var profile = ProfileItem(
                ("OrganizationService", []),
                ("ComplianceService", ["OrganizationService"]));

            await orchestrator.StartAsync(profile);

            Assert.Equal(RuntimeProfileMemberStatus.Failed, profile.Members.Single(m => m.ServiceName == "OrganizationService").Status);
            Assert.Equal(RuntimeProfileMemberStatus.Blocked, profile.Members.Single(m => m.ServiceName == "ComplianceService").Status);
            Assert.Equal(RuntimeProfileStatus.Failed, profile.Status);
            Assert.DoesNotContain("ComplianceService", host.StartCalls);
        }
        finally
        {
            Cleanup(storePath);
        }
    }

    [Fact]
    public async Task Stop_OnlyStopsOwnedServices()
    {
        var host = new FakeServiceRuntimeHost();
        host.Services.Add(Service("IdentityService", ServiceStatus.Running));
        host.Services.Add(Service("Gateway", ServiceStatus.Stopped));

        var orchestrator = CreateOrchestrator(host, out var storePath);
        try
        {
            var profile = ProfileItem(
                ("IdentityService", []),
                ("Gateway", ["IdentityService"]));

            await orchestrator.StartAsync(profile);
            await orchestrator.StopAsync(profile);

            Assert.Equal(["Gateway"], host.StopCalls);
            Assert.Empty(profile.OwnedServiceNames);
            Assert.Equal(RuntimeProfileStatus.Stopped, profile.Status);
        }
        finally
        {
            Cleanup(storePath);
        }
    }

    [Fact]
    public async Task Start_ReStart_RetainsOwnership_OfPreviouslyStartedServices()
    {
        var host = new FakeServiceRuntimeHost();
        host.Services.Add(Service("IdentityService", ServiceStatus.Running));
        host.Services.Add(Service("Gateway", ServiceStatus.Stopped));

        var orchestrator = CreateOrchestrator(host, out var storePath);
        try
        {
            var profile = ProfileItem(
                ("IdentityService", []),
                ("Gateway", ["IdentityService"]));

            await orchestrator.StartAsync(profile);
            Assert.Contains("Gateway", profile.OwnedServiceNames);
            Assert.DoesNotContain("IdentityService", profile.OwnedServiceNames);

            host.StartCalls.Clear();
            await orchestrator.StartAsync(profile);

            Assert.Contains("Gateway", profile.OwnedServiceNames);
            Assert.DoesNotContain("IdentityService", profile.OwnedServiceNames);
            Assert.Empty(host.StartCalls);

            host.StopCalls.Clear();
            await orchestrator.StopAsync(profile);
            Assert.Equal(["Gateway"], host.StopCalls);
        }
        finally
        {
            Cleanup(storePath);
        }
    }

    [Fact]
    public async Task Restart_StopsOwnedThenStartsAgain()
    {
        var host = new FakeServiceRuntimeHost();
        host.Services.Add(Service("Gateway"));

        var orchestrator = CreateOrchestrator(host, out var storePath);
        try
        {
            var profile = ProfileItem(("Gateway", []));
            await orchestrator.StartAsync(profile);
            host.StartCalls.Clear();
            host.StopCalls.Clear();

            await orchestrator.RestartAsync(profile);

            Assert.Equal(["Gateway"], host.StopCalls);
            Assert.Equal(["Gateway"], host.StartCalls);
            Assert.Equal(RuntimeProfileStatus.Running, profile.Status);
        }
        finally
        {
            Cleanup(storePath);
        }
    }

    [Fact]
    public async Task Start_InvalidProfile_DoesNotStart()
    {
        var host = new FakeServiceRuntimeHost();
        host.Services.Add(Service("WebApp"));

        var orchestrator = CreateOrchestrator(host, out var storePath);
        try
        {
            var profile = ProfileItem(("WebApp", ["Missing"]));
            await orchestrator.StartAsync(profile);

            Assert.Empty(host.StartCalls);
            Assert.Equal(RuntimeProfileStatus.Invalid, profile.Status);
        }
        finally
        {
            Cleanup(storePath);
        }
    }

    [Fact]
    public async Task IndividualStartStop_RemainCallableOnHost()
    {
        var host = new FakeServiceRuntimeHost();
        var service = Service("Gateway");
        host.Services.Add(service);

        Assert.Equal(ServiceStatus.Stopped, service.Status);
        await host.StartAsync(service, CancellationToken.None);
        Assert.Equal(ServiceStatus.Running, service.Status);
        await host.StopAsync(service, CancellationToken.None);
        Assert.Equal(ServiceStatus.Stopped, service.Status);
    }

    private static RuntimeProfileOrchestrator CreateOrchestrator(FakeServiceRuntimeHost host, out string storePath)
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "aovias-orch-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        storePath = Path.Combine(tempRoot, "runtime-profiles.json");
        var registry = new RuntimeProfileRegistry(storePath);
        var orchestrator = new RuntimeProfileOrchestrator(
            registry,
            new RuntimeProfileValidator(),
            host,
            new FakeHealthProbe(),
            new FakeLogger());
        orchestrator.ConfigureHealthWaitForTests(TimeSpan.FromMilliseconds(200), TimeSpan.FromMilliseconds(10));
        return orchestrator;
    }

    private static RuntimeProfileItem ProfileItem(params (string Name, string[] DependsOn)[] services)
        => RuntimeProfileItem.FromDefinition(new RuntimeProfileDefinition
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = "Test Profile",
            Enabled = true,
            MaxParallelStarts = 2,
            Services = services.Select(item => new RuntimeProfileServiceEntry
            {
                ServiceName = item.Name,
                Enabled = true,
                DependsOn = item.DependsOn.ToList()
            }).ToList()
        });

    private static ServiceInfoModel Service(string name, ServiceStatus status = ServiceStatus.Stopped)
        => new()
        {
            Name = name,
            ProjectPath = $@"C:\svc\{name}",
            Status = status,
            Health = status == ServiceStatus.Running ? HealthStatus.Healthy : HealthStatus.Unknown
        };

    private static void Cleanup(string storePath)
    {
        try
        {
            var dir = Path.GetDirectoryName(storePath);
            if (dir is not null && Directory.Exists(dir))
            {
                Directory.Delete(dir, recursive: true);
            }
        }
        catch
        {
        }
    }

    private sealed class FakeServiceRuntimeHost : IServiceRuntimeHost
    {
        public ObservableCollection<ServiceInfoModel> Services { get; } = [];

        public List<string> StartCalls { get; } = [];

        public List<string> StopCalls { get; } = [];

        public HashSet<string> FailOnStart { get; } = new(StringComparer.OrdinalIgnoreCase);

        public Task StartAsync(ServiceInfoModel service, CancellationToken cancellationToken)
        {
            StartCalls.Add(service.Name);
            if (FailOnStart.Contains(service.Name))
            {
                service.Status = ServiceStatus.Error;
                service.LastAction = "Simulated start failure";
                return Task.CompletedTask;
            }

            service.Status = ServiceStatus.Running;
            service.LastAction = "Started";
            return Task.CompletedTask;
        }

        public Task StopAsync(ServiceInfoModel service, CancellationToken cancellationToken)
        {
            StopCalls.Add(service.Name);
            service.Status = ServiceStatus.Stopped;
            service.LastAction = "Stopped";
            return Task.CompletedTask;
        }
    }

    private sealed class FakeHealthProbe : IHealthProbe
    {
        public Task<HealthStatus> CheckAsync(ServiceInfoModel service, CancellationToken cancellationToken)
            => Task.FromResult(service.Status == ServiceStatus.Running ? HealthStatus.Healthy : HealthStatus.Unknown);
    }

    private sealed class FakeLogger : ILoggingService
    {
        public ObservableCollection<LogEntryModel> Entries { get; } = [];

        public void Log(LogLevel level, string source, string message)
        {
        }

        public void Info(string source, string message)
        {
        }

        public void Warning(string source, string message)
        {
        }

        public void Error(string source, string message)
        {
        }

        public void Build(string source, string message)
        {
        }

        public void Process(string source, string message)
        {
        }
    }
}
