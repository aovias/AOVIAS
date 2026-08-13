using ComplianceServiceManager.Models.LocalResources;
using ComplianceServiceManager.Models.ResourceIntelligence;
using ComplianceServiceManager.Models.RuntimeProfiles;
using ComplianceServiceManager.Services.LocalResources;
using ComplianceServiceManager.Services.ResourceIntelligence;
using ComplianceServiceManager.Services.RuntimeProfiles;
using System.Collections.ObjectModel;

namespace ComplianceServiceManager.Tests.ResourceIntelligence;

public sealed class ResourceIntelligenceServiceTests
{
    [Theory]
    [InlineData(50, ResourcePressureLevel.Normal)]
    [InlineData(75, ResourcePressureLevel.Warning)]
    [InlineData(90, ResourcePressureLevel.High)]
    [InlineData(97, ResourcePressureLevel.Critical)]
    public void MemoryPressure_ClassifiedByPolicy(double memoryPercent, ResourcePressureLevel expected)
    {
        using var resources = new FakeResources(CreateSnapshot(memoryPercent: memoryPercent, cpuPercent: 20, diskPercent: 20));
        using var service = new ResourceIntelligenceService(resources, ResourceThresholdPolicy.Default);
        var assessment = service.Assess();
        Assert.Equal(expected, assessment.MemoryPressure);
    }

    [Fact]
    public void CpuPressure_UsesWindowAverage_NotSingleSpikeAloneWhenHistoryWarm()
    {
        using var resources = new FakeResources(CreateSnapshot(memoryPercent: 40, cpuPercent: 20, diskPercent: 20));
        using var service = new ResourceIntelligenceService(resources, new ResourceThresholdPolicy { CpuWindowSampleCount = 3 });

        service.AssessFromSnapshot(CreateSnapshot(memoryPercent: 40, cpuPercent: 20, diskPercent: 20));
        service.AssessFromSnapshot(CreateSnapshot(memoryPercent: 40, cpuPercent: 25, diskPercent: 20));
        var assessment = service.AssessFromSnapshot(CreateSnapshot(memoryPercent: 40, cpuPercent: 90, diskPercent: 20));

        Assert.NotNull(assessment.CpuWindowAveragePercent);
        Assert.InRange(assessment.CpuWindowAveragePercent!.Value, 40, 50);
        Assert.Equal(ResourcePressureLevel.Normal, assessment.CpuPressure);
    }

    [Fact]
    public void HighMemory_ProducesLowMemoryRecommendation_WithExplanation()
    {
        using var resources = new FakeResources(CreateSnapshot(memoryPercent: 92, cpuPercent: 30, diskPercent: 30));
        using var service = new ResourceIntelligenceService(resources);
        var assessment = service.Assess();
        var recommendation = Assert.Single(assessment.Recommendations, item => item.Kind == ResourceRecommendationKind.LowMemory);
        Assert.Contains("Memory usage is currently 92", recommendation.Explanation, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("High memory pressure", recommendation.Title);
    }

    [Fact]
    public void ProfileEstimate_ComparativeByServiceCount()
    {
        using var resources = new FakeResources(CreateSnapshot(40, 20, 20));
        using var service = new ResourceIntelligenceService(resources);

        var light = service.EstimateProfile(new RuntimeProfileDefinition
        {
            Id = "light",
            Name = "Light",
            Services = [new RuntimeProfileServiceEntry { ServiceName = "A", Enabled = true }]
        });
        var heavy = service.EstimateProfile(new RuntimeProfileDefinition
        {
            Id = "heavy",
            Name = "Heavy",
            Services =
            [
                new() { ServiceName = "1", Enabled = true },
                new() { ServiceName = "2", Enabled = true },
                new() { ServiceName = "3", Enabled = true },
                new() { ServiceName = "4", Enabled = true },
                new() { ServiceName = "5", Enabled = true },
                new() { ServiceName = "6", Enabled = true }
            ]
        });

        Assert.Equal(RuntimeProfileResourceClass.Light, light.MemoryClass);
        Assert.Equal(RuntimeProfileResourceClass.Heavy, heavy.MemoryClass);
        Assert.Contains("Estimated resource level", light.Explanation, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RequestedHeavyProfile_UnderWarningMemory_SuggestsLighter()
    {
        using var resources = new FakeResources(CreateSnapshot(memoryPercent: 88, cpuPercent: 20, diskPercent: 20));
        using var service = new ResourceIntelligenceService(resources, profiles: new FakeProfiles());
        var assessment = service.Assess("heavy-profile");
        var recommendation = Assert.Single(assessment.Recommendations, item => item.Kind == ResourceRecommendationKind.ProfileTooHeavy);
        Assert.Equal("light-profile", recommendation.RelatedProfileId);
        Assert.Contains("less likely to increase memory pressure", recommendation.Explanation, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void NormalState_CapacityOkRecommendation()
    {
        using var resources = new FakeResources(CreateSnapshot(40, 20, 20));
        using var service = new ResourceIntelligenceService(resources);
        var assessment = service.Assess();
        Assert.Equal(ResourcePressureLevel.Normal, assessment.OverallPressure);
        Assert.Contains(assessment.Recommendations, item => item.Kind == ResourceRecommendationKind.CapacityOk);
    }

    [Fact]
    public void History_IsBounded()
    {
        using var resources = new FakeResources(CreateSnapshot(40, 20, 20));
        var policy = new ResourceThresholdPolicy { HistoryCapacity = 3, CpuWindowSampleCount = 3 };
        using var service = new ResourceIntelligenceService(resources, policy);
        for (var i = 0; i < 10; i++)
        {
            service.AssessFromSnapshot(CreateSnapshot(40, 20 + i, 20));
        }

        // No exception and assessment still works.
        var assessment = service.Assess();
        Assert.NotNull(assessment);
        Assert.True(assessment.CpuWindowAveragePercent is not null);
    }

    [Fact]
    public void ThresholdPolicy_IsCentralizedAndConfigurable()
    {
        var policy = new ResourceThresholdPolicy
        {
            MemoryNormalMaxPercent = 10,
            MemoryWarningMaxPercent = 20,
            MemoryHighMaxPercent = 30
        };
        using var resources = new FakeResources(CreateSnapshot(25, 10, 10));
        using var service = new ResourceIntelligenceService(resources, policy);
        Assert.Equal(ResourcePressureLevel.High, service.Assess().MemoryPressure);
    }

    [Fact]
    public void Service_DoesNotExposeMutationApis()
    {
        var methods = typeof(IResourceIntelligenceService).GetMethods().Select(method => method.Name).ToHashSet();
        Assert.DoesNotContain("StartProfile", methods);
        Assert.DoesNotContain("StopProfile", methods);
        Assert.DoesNotContain("KillProcess", methods);
        Assert.DoesNotContain("Merge", methods);
        Assert.Contains("Assess", methods);
    }

    private static LocalResourceSnapshot CreateSnapshot(double memoryPercent, double cpuPercent, double diskPercent)
    {
        var total = 16L * 1024 * 1024 * 1024;
        var used = (long)(total * (memoryPercent / 100.0));
        return new LocalResourceSnapshot
        {
            CapturedAt = DateTimeOffset.UtcNow,
            SampleInterval = TimeSpan.FromSeconds(3),
            Source = "LocalHost",
            Host = new LocalHostResourceMetrics
            {
                CpuPercent = cpuPercent,
                MemoryPercent = memoryPercent,
                MemoryTotalBytes = total,
                MemoryUsedBytes = used,
                MemoryAvailableBytes = total - used,
                DiskPercent = diskPercent,
                DiskRootPath = @"V:\",
                DiskTotalBytes = 1000,
                DiskUsedBytes = (long)(10 * diskPercent),
                DiskFreeBytes = 1000 - (long)(10 * diskPercent),
                CpuStatus = LocalResourceStatus.Normal,
                MemoryStatus = LocalResourceStatus.Normal,
                DiskStatus = LocalResourceStatus.Normal
            },
            Services = []
        };
    }

    private sealed class FakeResources : ILocalResourceMonitoringService
    {
        public FakeResources(LocalResourceSnapshot latest)
        {
            LatestSnapshot = latest;
        }

        public LocalResourceMonitoringOptions Options { get; } = new();

        public LocalResourceSnapshot? LatestSnapshot { get; private set; }

        public event Action<LocalResourceSnapshot>? SnapshotUpdated;

        public void Start()
        {
        }

        public void Stop()
        {
        }

        public LocalResourceSnapshot CaptureSnapshot(IEnumerable<(string ServiceName, int? ProcessId)>? services = null)
            => LatestSnapshot!;

        public void Dispose()
        {
        }

        public void Publish(LocalResourceSnapshot snapshot)
        {
            LatestSnapshot = snapshot;
            SnapshotUpdated?.Invoke(snapshot);
        }
    }

    private sealed class FakeProfiles : IRuntimeProfileOrchestrator
    {
        private readonly List<RuntimeProfileItem> profiles =
        [
            RuntimeProfileItem.FromDefinition(new RuntimeProfileDefinition
            {
                Id = "light-profile",
                Name = "AOVIAS Web Development",
                Services =
                [
                    new() { ServiceName = "Gateway", Enabled = true },
                    new() { ServiceName = "WebApp", Enabled = true }
                ]
            }),
            RuntimeProfileItem.FromDefinition(new RuntimeProfileDefinition
            {
                Id = "heavy-profile",
                Name = "AOVIAS Full Development",
                Services =
                [
                    new() { ServiceName = "1", Enabled = true },
                    new() { ServiceName = "2", Enabled = true },
                    new() { ServiceName = "3", Enabled = true },
                    new() { ServiceName = "4", Enabled = true },
                    new() { ServiceName = "5", Enabled = true },
                    new() { ServiceName = "6", Enabled = true }
                ]
            })
        ];

        public event Action? ProfilesChanged;

        public IReadOnlyList<RuntimeProfileItem> GetProfiles() => profiles;

        public void Reload()
        {
        }

        public ProfileValidationResult Validate(RuntimeProfileItem profile)
            => ProfileValidationResult.Valid(profile.Services.Select(entry => entry.ServiceName).ToList());

        public void ValidateAll()
        {
        }

        public Task StartAsync(RuntimeProfileItem profile, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("D5 tests must not start profiles.");

        public Task StopAsync(RuntimeProfileItem profile, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("D5 tests must not stop profiles.");

        public Task RestartAsync(RuntimeProfileItem profile, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("D5 tests must not restart profiles.");

        public void SaveProfile(RuntimeProfileItem profile)
        {
            ProfilesChanged?.Invoke();
        }
    }
}
