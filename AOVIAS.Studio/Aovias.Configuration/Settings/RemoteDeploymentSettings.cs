namespace Aovias.Configuration.Settings;

/// <summary>
/// Remote Deployment Orchestrator API settings for platform deployment over HTTPS.
/// </summary>
public sealed class RemoteDeploymentSettings
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "RemoteDeployment";

    public bool Enabled { get; set; } = true;

    public string PackageStoreRoot { get; set; } = string.Empty;

    public string UploadEndpoint { get; set; } = "/api/deployment/upload";

    public string DeployEndpoint { get; set; } = "/api/deployment/deploy";

    public string PlatformEndpoint { get; set; } = "/api/deployment/platform";

    public string RollbackEndpoint { get; set; } = "/api/deployment/rollback";

    public string CancelEndpoint { get; set; } = "/api/deployment/cancel";

    public string StatusEndpointTemplate { get; set; } = "/api/deployment/status/{deploymentId}";

    public string LogsEndpointTemplate { get; set; } = "/api/deployment/logs/{deploymentId}";

    public string HistoryEndpoint { get; set; } = "/api/deployment/history";

    public string ReportEndpointTemplate { get; set; } = "/api/deployment/report/{deploymentId}";

    public string RegisterEndpoint { get; set; } = "/api/deployment/register";

    public string HealthEndpointTemplate { get; set; } = "/api/deployment/health/{environmentId}";

    public string DriftEndpointTemplate { get; set; } = "/api/deployment/drift/{environmentId}";

    /// <summary>Sprint M2.7: lightweight fleet heartbeat — health/reconciliation/deployment summary, runtime version, capabilities, desired state version.</summary>
    public string HeartbeatEndpoint { get; set; } = "/api/deployment/heartbeat";

    /// <summary>Sprint M2.7: same endpoint the M2.6 <c>PlatformReconciliationService.ReconcileAsync</c> already exposed — reused, not duplicated, by the Command Center's "trigger reconciliation" API.</summary>
    public string ReconcileEndpoint { get; set; } = "/api/deployment/reconcile";

    public string MaintenanceEndpoint { get; set; } = "/api/deployment/maintenance";

    public string MaintenanceResumeEndpoint { get; set; } = "/api/deployment/maintenance/resume";

    public int TimeoutSeconds { get; set; } = 300;

    public int UploadChunkSizeBytes { get; set; } = 4 * 1024 * 1024;

    public int SmallUploadTimeoutSeconds { get; set; } = 30;

    public int MediumUploadTimeoutSeconds { get; set; } = 120;

    public int LargeUploadTimeoutSeconds { get; set; } = 600;

    public long MediumPackageThresholdBytes { get; set; } = 50L * 1024 * 1024;

    public long LargePackageThresholdBytes { get; set; } = 250L * 1024 * 1024;

    public int UploadTransientRetryCount { get; set; } = 3;

    public int UploadRetryBaseDelayMilliseconds { get; set; } = 500;

    public int PackageRetentionCount { get; set; } = 5;
}
