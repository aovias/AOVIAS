namespace ComplianceServiceManager.Models.Runtime;

/// <summary>
/// Represents operational statistics and component status summaries for a runtime host.
/// </summary>
public sealed class RuntimeStatistics
{
    public string DockerEngineStatus { get; init; } = string.Empty;
    public RuntimeHealth DockerEngineHealth { get; init; } = RuntimeHealth.Unknown;
    public string NginxStatus { get; init; } = string.Empty;
    public RuntimeHealth NginxHealth { get; init; } = RuntimeHealth.Unknown;
    public string SqlServerStatus { get; init; } = string.Empty;
    public RuntimeHealth SqlServerHealth { get; init; } = RuntimeHealth.Unknown;
    public string CertificatesStatus { get; init; } = string.Empty;
    public RuntimeHealth CertificatesHealth { get; init; } = RuntimeHealth.Unknown;
    public string DeploymentAgentStatus { get; init; } = string.Empty;
    public RuntimeHealth DeploymentAgentHealth { get; init; } = RuntimeHealth.Unknown;
    public double DiskUsagePercent { get; init; }
    public double CpuUsagePercent { get; init; }
    public double MemoryUsagePercent { get; init; }
    public string DockerVersion { get; init; } = string.Empty;
    public int ContainerCount { get; init; }
    public int RunningContainerCount { get; init; }
    public int NetworkCount { get; init; }
    public int VolumeCount { get; init; }
    public int ImageCount { get; init; }
    public string OperatingSystem { get; init; } = string.Empty;
    public string Kernel { get; init; } = string.Empty;
    public string Hostname { get; init; } = string.Empty;
    public string Environment { get; init; } = string.Empty;
    public string PublicUrl { get; init; } = string.Empty;
}
