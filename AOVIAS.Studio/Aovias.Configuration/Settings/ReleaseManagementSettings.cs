using System.ComponentModel.DataAnnotations;

namespace Aovias.Configuration.Settings;

/// <summary>
/// Release management platform settings for package and deployment orchestration.
/// </summary>
public sealed class ReleaseManagementSettings
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "ReleaseManagement";

    public string ReleaseRoot { get; set; } = string.Empty;

    public string PackageRoot { get; set; } = string.Empty;

    public string TemporaryFolder { get; set; } = string.Empty;

    public string ArchiveFolder { get; set; } = string.Empty;

    public string IncomingFolder { get; set; } = string.Empty;

    public string StagingFolder { get; set; } = string.Empty;

    public string ExtractionFolder { get; set; } = string.Empty;

    public string ActiveFolder { get; set; } = string.Empty;

    public string RollbackFolder { get; set; } = string.Empty;

    public string AuditFolder { get; set; } = string.Empty;

    [Range(1, long.MaxValue)]
    public long MaximumPackageSizeBytes { get; set; }

    [Required]
    public string SupportedPackageVersion { get; set; } = string.Empty;

    [Required]
    public string SupportedRuntimeVersion { get; set; } = string.Empty;

    [Required]
    public string UploadEndpoint { get; set; } = string.Empty;

    [Required]
    public string ValidateEndpoint { get; set; } = string.Empty;

    [Required]
    public string PlanEndpoint { get; set; } = string.Empty;

    [Required]
    public string DeployEndpoint { get; set; } = string.Empty;

    [Required]
    public string RollbackEndpoint { get; set; } = string.Empty;

    [Required]
    public string ListEndpoint { get; set; } = string.Empty;

    [Required]
    public string HistoryEndpoint { get; set; } = string.Empty;

    [Required]
    public string DetailEndpointTemplate { get; set; } = string.Empty;

    [Range(1, 600)]
    public int TimeoutSeconds { get; set; }

    [Range(0, 10)]
    public int RetryCount { get; set; }
}
