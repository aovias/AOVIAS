namespace Aovias.Orchestration.Contracts.Capabilities;

/// <summary>
/// Well-known runtime capability identifiers.
/// </summary>
public static class RuntimeCapability
{
    public const string Runtime = "Runtime";
    public const string ReleaseManagement = "ReleaseManagement";
    public const string DockerProvider = "DockerProvider";
    public const string SignalR = "SignalR";
    public const string Publishing = "Publishing";
    public const string Monitoring = "Monitoring";
    public const string WorkflowEngine = "WorkflowEngine";
    public const string Rollback = "Rollback";
    public const string HealthMonitoring = "HealthMonitoring";
    public const string Observability = "Observability";
    public const string CertificateManagement = "CertificateManagement";
    public const string BackupEngine = "BackupEngine";
    public const string RemoteDeploymentOrchestration = "RemoteDeploymentOrchestration";
    public const string PlatformCertification = "PlatformCertification";
}

/// <summary>
/// Runtime capability advertisement returned by the control plane.
/// </summary>
public sealed class RuntimeCapabilities
{
    public required string RuntimeVersion { get; init; }

    public required IReadOnlyList<string> Capabilities { get; init; }

    public bool Supports(string capability)
        => Capabilities.Contains(capability, StringComparer.OrdinalIgnoreCase);
}
