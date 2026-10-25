using System.ComponentModel.DataAnnotations;

namespace Aovias.Configuration.Settings;

/// <summary>
/// AOVIAS Publishing Engine (APE) platform settings.
/// </summary>
public sealed class PublishingSettings
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "Publishing";

    [Required]
    public string DockerExecutable { get; set; } = "docker";

    [Required]
    public string DotNetExecutable { get; set; } = "dotnet";

    [Required]
    public string NodeExecutable { get; set; } = "node";

    [Required]
    public string NpmExecutable { get; set; } = "npm";

    [Required]
    public string PublishRoot { get; set; } = string.Empty;

    [Required]
    public string ArtifactRoot { get; set; } = string.Empty;

    [Required]
    public string OutputFolder { get; set; } = string.Empty;

    [Required]
    public string TemporaryFolder { get; set; } = string.Empty;

    [Required]
    public string DefaultRuntime { get; set; } = string.Empty;

    [Required]
    public string DefaultNetworkName { get; set; } = string.Empty;

    [Required]
    public string RuntimeAgentServiceId { get; set; } = "runtime-agent";

    [Required]
    public string PlatformProjectsRoot { get; set; } = string.Empty;

    [Required]
    public string DockerfileAspNetTemplatePath { get; set; } = string.Empty;

    [Required]
    public string DockerfileWorkerTemplatePath { get; set; } = string.Empty;

    [Required]
    public string DockerfileAngularTemplatePath { get; set; } = string.Empty;

    [Required]
    public string DockerfileStaticTemplatePath { get; set; } = string.Empty;

    public Dictionary<string, string> DockerBuildArgs { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    [Range(0, 10)]
    public int DefaultRetryCount { get; set; } = 2;

    [Range(30, 3600)]
    public int StepTimeoutSeconds { get; set; } = 600;

    public bool EnableDeploymentReadinessValidation { get; set; } = true;

    [Required]
    public string DeploymentValidationFolder { get; set; } = string.Empty;

    [Required]
    public string ValidationConfigRoot { get; set; } = string.Empty;

    [Required]
    public string ValidationProfile { get; set; } = "local";

    [Required]
    public string DeploymentActiveProfile { get; set; } = "demo";

    [Range(30, 3600)]
    public int DeploymentValidationTimeoutSeconds { get; set; } = 900;

    /// <summary>
    /// Publisher name stamped into <c>metadata.json</c>. Must never contain a
    /// developer or machine identity.
    /// </summary>
    [Required]
    public string PublisherName { get; set; } = "AOVIAS";

    /// <summary>
    /// Container runtime/OS the produced images target (for example
    /// <c>linux</c>). Combined with the discovered framework to stamp the
    /// deployment certificate's runtime compatibility.
    /// </summary>
    [Required]
    public string TargetRuntimeOs { get; set; } = "linux";

    /// <summary>
    /// Validation depth for deployment readiness checks.
    /// </summary>
    public string DeploymentValidationMode { get; set; } = "Balanced";

    /// <summary>
    /// When true, solution publishing runs DRV sequentially with a shared workspace.
    /// </summary>
    public bool SequentialDeploymentValidation { get; set; } = true;
}
