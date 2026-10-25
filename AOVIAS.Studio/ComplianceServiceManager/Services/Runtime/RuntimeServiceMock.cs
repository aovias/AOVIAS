using ComplianceServiceManager.Contracts.Runtime;
using ComplianceServiceManager.Models.Runtime;

namespace ComplianceServiceManager.Services.Runtime;

/// <summary>
/// Provides mock AOVIAS Runtime data for UI development until the Runtime Agent is available.
/// </summary>
public sealed class RuntimeServiceMock : IRuntimeService
{
    private static readonly DateTimeOffset StartedAt = DateTimeOffset.UtcNow.AddHours(-6).AddMinutes(-14);

    /// <inheritdoc />
    public Task<RuntimeInfo> GetRuntimeAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var runtime = new RuntimeInfo
        {
            Name = "AOVIAS Runtime",
            Environment = "Demo",
            Server = "aovias-demo-01",
            Version = "1.0",
            Status = "Healthy",
            Health = RuntimeHealth.Healthy,
            LastUpdated = DateTimeOffset.Now,
            Hostname = "aovias-demo-01.internal",
            DockerNetwork = "aovias-runtime-net",
            OperatingSystem = "Ubuntu 22.04 LTS",
            Kernel = "5.15.0-105-generic",
            PublicIp = "203.0.113.42",
            SslEnabled = true,
            HttpsEndpoint = "https://aovias-demo-01.aovias.cloud"
        };

        return Task.FromResult(runtime);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<RuntimeService>> GetRuntimeServicesAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyList<RuntimeService> services =
        [
            new RuntimeService
            {
                Id = "docker-engine",
                Name = "Docker",
                Category = "Container Platform",
                Version = "26.1.4",
                Status = "Running",
                Health = RuntimeHealth.Running,
                Started = StartedAt,
                Uptime = FormatUptime(DateTimeOffset.UtcNow - StartedAt)
            },
            new RuntimeService
            {
                Id = "nginx",
                Name = "Nginx",
                Category = "Reverse Proxy",
                Version = "1.25.4",
                Status = "Healthy",
                Health = RuntimeHealth.Healthy,
                Started = StartedAt.AddMinutes(2),
                Uptime = FormatUptime(DateTimeOffset.UtcNow - StartedAt.AddMinutes(2))
            },
            new RuntimeService
            {
                Id = "sql-server",
                Name = "SQL Server",
                Category = "Database",
                Version = "2022-CU12",
                Status = "Healthy",
                Health = RuntimeHealth.Healthy,
                Started = StartedAt.AddMinutes(5),
                Uptime = FormatUptime(DateTimeOffset.UtcNow - StartedAt.AddMinutes(5))
            }
        ];

        return Task.FromResult(services);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<RuntimeEvent>> GetRuntimeEventsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var now = DateTimeOffset.Now;
        IReadOnlyList<RuntimeEvent> events =
        [
            new RuntimeEvent
            {
                Timestamp = now.AddSeconds(-12),
                Level = "Info",
                Message = "Certificate Loaded"
            },
            new RuntimeEvent
            {
                Timestamp = now.AddMinutes(-2),
                Level = "Info",
                Message = "SQL Server Healthy"
            },
            new RuntimeEvent
            {
                Timestamp = now.AddMinutes(-3),
                Level = "Info",
                Message = "Docker Healthy"
            },
            new RuntimeEvent
            {
                Timestamp = now.AddMinutes(-6),
                Level = "Info",
                Message = "Nginx configuration reloaded successfully"
            },
            new RuntimeEvent
            {
                Timestamp = StartedAt.ToLocalTime(),
                Level = "Info",
                Message = "Runtime Started"
            },
            new RuntimeEvent
            {
                Timestamp = StartedAt.ToLocalTime().AddMinutes(-1),
                Level = "Info",
                Message = "Runtime agent mock connected to Operations Console"
            }
        ];

        return Task.FromResult(events);
    }

    /// <inheritdoc />
    public Task<RuntimeStatistics> GetRuntimeStatisticsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var statistics = new RuntimeStatistics
        {
            DockerEngineStatus = "Running",
            DockerEngineHealth = RuntimeHealth.Running,
            NginxStatus = "Healthy",
            NginxHealth = RuntimeHealth.Healthy,
            SqlServerStatus = "Healthy",
            SqlServerHealth = RuntimeHealth.Healthy,
            CertificatesStatus = "Valid",
            CertificatesHealth = RuntimeHealth.Valid,
            DeploymentAgentStatus = "Not Installed",
            DeploymentAgentHealth = RuntimeHealth.NotInstalled,
            DiskUsagePercent = 63.4,
            CpuUsagePercent = 28.6,
            MemoryUsagePercent = 54.2
        };

        return Task.FromResult(statistics);
    }

    private static string FormatUptime(TimeSpan uptime)
    {
        if (uptime.TotalDays >= 1)
        {
            return $"{(int)uptime.TotalDays}d {uptime.Hours}h {uptime.Minutes}m";
        }

        if (uptime.TotalHours >= 1)
        {
            return $"{(int)uptime.TotalHours}h {uptime.Minutes}m";
        }

        return $"{Math.Max(1, (int)uptime.TotalMinutes)}m";
    }
}
