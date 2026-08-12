using System.Collections.ObjectModel;
using ComplianceServiceManager.Models;
using ComplianceServiceManager.Models.LocalResources;
using ComplianceServiceManager.Services;
using ComplianceServiceManager.Services.LocalResources;

namespace ComplianceServiceManager.Tests.LocalResources;

public sealed class LocalResourceStatusClassifierTests
{
    [Fact]
    public void Classify_NullOrNegative_IsUnknown()
    {
        Assert.Equal(LocalResourceStatus.Unknown, LocalResourceStatusClassifier.Classify(null, 80, 92));
        Assert.Equal(LocalResourceStatus.Unknown, LocalResourceStatusClassifier.Classify(-1, 80, 92));
    }

    [Theory]
    [InlineData(10.0, LocalResourceStatus.Normal)]
    [InlineData(80.0, LocalResourceStatus.Warning)]
    [InlineData(92.0, LocalResourceStatus.Critical)]
    [InlineData(99.0, LocalResourceStatus.Critical)]
    public void Classify_MemoryThresholds(double percent, LocalResourceStatus expected)
    {
        var status = LocalResourceStatusClassifier.Classify(percent, warningThreshold: 80, criticalThreshold: 92);
        Assert.Equal(expected, status);
    }

    [Fact]
    public void ComputePercent_UsesUsedOverTotal()
    {
        Assert.Equal(50, LocalResourceStatusClassifier.ComputePercent(50, 100));
        Assert.Null(LocalResourceStatusClassifier.ComputePercent(10, 0));
        Assert.Null(LocalResourceStatusClassifier.ComputePercent(-1, 100));
    }
}

public sealed class LocalResourceFormattingTests
{
    [Fact]
    public void FormatBytes_ScalesUnits()
    {
        Assert.Equal("512 B", LocalResourceFormatting.FormatBytes(512));
        Assert.Equal("1 KB", LocalResourceFormatting.FormatBytes(1024));
        Assert.Equal("1.5 GB", LocalResourceFormatting.FormatBytes((long)(1.5 * 1024 * 1024 * 1024)));
    }
}

public sealed class FakeLocalResourceCollectorTests
{
    [Fact]
    public void CaptureSnapshot_HostCpuMemoryDisk()
    {
        var collector = new FakeCollector
        {
            Host = new LocalHostResourceMetrics
            {
                CpuPercent = 42,
                MemoryTotalBytes = 16L * 1024 * 1024 * 1024,
                MemoryUsedBytes = 12L * 1024 * 1024 * 1024,
                MemoryAvailableBytes = 4L * 1024 * 1024 * 1024,
                MemoryPercent = 75,
                DiskRootPath = @"V:\",
                DiskTotalBytes = 1000,
                DiskUsedBytes = 410,
                DiskFreeBytes = 590,
                DiskPercent = 41,
                CpuStatus = LocalResourceStatus.Normal,
                MemoryStatus = LocalResourceStatus.Normal,
                DiskStatus = LocalResourceStatus.Normal
            }
        };

        using var service = new LocalResourceMonitoringService(collector, new LocalResourceMonitoringOptions
        {
            PollInterval = TimeSpan.FromSeconds(3),
            DiskProbePath = @"V:\DevelopmentRepo"
        });

        var snapshot = service.CaptureSnapshot();
        Assert.Equal(42, snapshot.Host.CpuPercent);
        Assert.Equal(75, snapshot.Host.MemoryPercent);
        Assert.Equal(41, snapshot.Host.DiskPercent);
        Assert.Equal("LocalHost", snapshot.Source);
        Assert.NotEqual("RuntimeAgent", snapshot.Source);
    }

    [Fact]
    public void CaptureSnapshot_AssociatesServiceProcess()
    {
        var collector = new FakeCollector
        {
            Processes =
            {
                [1234] = new LocalProcessResourceMetrics
                {
                    ProcessId = 1234,
                    ProcessName = "Gateway",
                    CpuPercent = 3.2,
                    MemoryWorkingSetBytes = 240 * 1024 * 1024,
                    IsAccessible = true
                }
            }
        };

        using var service = new LocalResourceMonitoringService(collector);
        var snapshot = service.CaptureSnapshot([("Gateway", 1234)]);

        var row = Assert.Single(snapshot.Services);
        Assert.Equal("Gateway", row.ServiceName);
        Assert.Equal(1234, row.ProcessId);
        Assert.Equal(3.2, row.Process!.CpuPercent);
        Assert.True(row.Process.IsAccessible);
    }

    [Fact]
    public void CaptureSnapshot_EmptyProcessSet()
    {
        using var service = new LocalResourceMonitoringService(new FakeCollector());
        var snapshot = service.CaptureSnapshot([]);
        Assert.Empty(snapshot.Services);
    }

    [Fact]
    public void CaptureSnapshot_ProcessExited()
    {
        var collector = new FakeCollector
        {
            Processes =
            {
                [99] = new LocalProcessResourceMetrics
                {
                    ProcessId = 99,
                    IsAccessible = false,
                    Error = "Process exited."
                }
            }
        };

        using var service = new LocalResourceMonitoringService(collector);
        var snapshot = service.CaptureSnapshot([("WebApp", 99)]);
        Assert.False(snapshot.Services[0].Process!.IsAccessible);
        Assert.Equal("Process exited.", snapshot.Services[0].Process!.Error);
    }

    [Fact]
    public void CaptureSnapshot_AccessDeniedProcess_DoesNotThrow()
    {
        var collector = new FakeCollector
        {
            Processes =
            {
                [1] = new LocalProcessResourceMetrics
                {
                    ProcessId = 1,
                    IsAccessible = false,
                    Error = "Access denied (5)."
                }
            }
        };

        using var service = new LocalResourceMonitoringService(collector);
        var snapshot = service.CaptureSnapshot([("Protected", 1)]);
        Assert.False(snapshot.Services[0].Process!.IsAccessible);
        Assert.Contains("Access denied", snapshot.Services[0].Process!.Error);
    }

    [Fact]
    public void CaptureSnapshot_CollectorThrowsForOneProcess_StillReturnsOthers()
    {
        var collector = new ThrowingThenOkCollector();
        using var service = new LocalResourceMonitoringService(collector);
        var snapshot = service.CaptureSnapshot([("Bad", 1), ("Good", 2)]);
        Assert.Equal(2, snapshot.Services.Count);
        Assert.False(snapshot.Services[0].Process!.IsAccessible);
        Assert.True(snapshot.Services[1].Process!.IsAccessible);
    }

    [Fact]
    public void PollingLifecycle_StartStop_IsIdempotent()
    {
        using var service = new LocalResourceMonitoringService(new FakeCollector(), new LocalResourceMonitoringOptions
        {
            PollInterval = TimeSpan.FromSeconds(2)
        });

        service.Start();
        service.Start();
        Assert.NotNull(service.LatestSnapshot);
        service.Stop();
        service.Stop();
    }

    [Fact]
    public void CaptureSnapshot_UsesServiceHostRunningProcesses()
    {
        var host = new FakeServiceHost();
        host.Services.Add(new ServiceInfoModel
        {
            Name = "OrganizationService",
            Status = ServiceStatus.Running,
            ProcessId = 5678
        });
        host.Services.Add(new ServiceInfoModel
        {
            Name = "StoppedService",
            Status = ServiceStatus.Stopped,
            ProcessId = 1
        });

        var collector = new FakeCollector
        {
            Processes =
            {
                [5678] = new LocalProcessResourceMetrics
                {
                    ProcessId = 5678,
                    ProcessName = "OrganizationService",
                    CpuPercent = 5.1,
                    MemoryWorkingSetBytes = 310 * 1024 * 1024,
                    IsAccessible = true
                }
            }
        };

        using var service = new LocalResourceMonitoringService(collector, serviceHost: host);
        var snapshot = service.CaptureSnapshot();
        var row = Assert.Single(snapshot.Services);
        Assert.Equal("OrganizationService", row.ServiceName);
        Assert.Equal(5678, row.ProcessId);
    }

    [Fact]
    public async Task CaptureSnapshot_DoesNotRequireUiThread()
    {
        using var service = new LocalResourceMonitoringService(new FakeCollector());
        var snapshot = await Task.Run(() => service.CaptureSnapshot());
        Assert.Equal("LocalHost", snapshot.Source);
    }

    private sealed class FakeCollector : ILocalResourceCollector
    {
        public LocalHostResourceMetrics Host { get; set; } = new()
        {
            CpuPercent = 1,
            MemoryTotalBytes = 1024,
            MemoryUsedBytes = 512,
            MemoryAvailableBytes = 512,
            MemoryPercent = 50,
            DiskRootPath = @"C:\",
            DiskTotalBytes = 100,
            DiskUsedBytes = 40,
            DiskFreeBytes = 60,
            DiskPercent = 40,
            CpuStatus = LocalResourceStatus.Normal,
            MemoryStatus = LocalResourceStatus.Normal,
            DiskStatus = LocalResourceStatus.Normal
        };

        public Dictionary<int, LocalProcessResourceMetrics> Processes { get; } = new();

        public LocalHostResourceMetrics CollectHost(string diskPath, LocalResourceThresholds thresholds) => Host;

        public LocalProcessResourceMetrics CollectProcess(int processId)
            => Processes.TryGetValue(processId, out var metrics)
                ? metrics
                : new LocalProcessResourceMetrics { ProcessId = processId, IsAccessible = false, Error = "missing" };
    }

    private sealed class ThrowingThenOkCollector : ILocalResourceCollector
    {
        public LocalHostResourceMetrics CollectHost(string diskPath, LocalResourceThresholds thresholds)
            => new() { CpuPercent = 1, MemoryPercent = 1, DiskPercent = 1 };

        public LocalProcessResourceMetrics CollectProcess(int processId)
        {
            if (processId == 1)
            {
                throw new InvalidOperationException("boom");
            }

            return new LocalProcessResourceMetrics
            {
                ProcessId = processId,
                ProcessName = "ok",
                CpuPercent = 1,
                MemoryWorkingSetBytes = 1024,
                IsAccessible = true
            };
        }
    }

    private sealed class FakeServiceHost : IServiceRuntimeHost
    {
        public ObservableCollection<ServiceInfoModel> Services { get; } = [];

        public Task StartAsync(ServiceInfoModel service, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task StopAsync(ServiceInfoModel service, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}

public sealed class WindowsLocalResourceCollectorSmokeTests
{
    [Fact]
    public void CollectHost_ReturnsMemoryAndDiskWithoutThrowing()
    {
        using var collector = new WindowsLocalResourceCollector();
        var first = collector.CollectHost(@"V:\DevelopmentRepo", LocalResourceThresholds.Default);
        Assert.True(first.MemoryTotalBytes > 0);
        Assert.True(first.MemoryAvailableBytes >= 0);
        Assert.True(first.MemoryUsedBytes >= 0);
        Assert.NotNull(first.MemoryPercent);
        // First CPU sample is warm-up.
        Assert.Null(first.CpuPercent);

        Thread.Sleep(50);
        var second = collector.CollectHost(@"V:\DevelopmentRepo", LocalResourceThresholds.Default);
        Assert.True(second.CpuPercent is null or >= 0 and <= 100);
        Assert.False(string.IsNullOrWhiteSpace(second.DiskRootPath));
    }

    [Fact]
    public void CollectProcess_CurrentProcess_IsAccessible()
    {
        using var collector = new WindowsLocalResourceCollector();
        var pid = Environment.ProcessId;
        _ = collector.CollectProcess(pid); // warm-up
        var sample = collector.CollectProcess(pid);
        Assert.True(sample.IsAccessible);
        Assert.Equal(pid, sample.ProcessId);
        Assert.True(sample.MemoryWorkingSetBytes > 0);
    }

    [Fact]
    public void CollectProcess_MissingPid_ReportsExited()
    {
        using var collector = new WindowsLocalResourceCollector();
        var sample = collector.CollectProcess(int.MaxValue - 10);
        Assert.False(sample.IsAccessible);
        Assert.Contains("Process exited", sample.Error ?? string.Empty);
    }
}
