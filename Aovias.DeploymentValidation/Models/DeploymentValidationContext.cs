using Aovias.Deployment.Contracts;
using Aovias.Deployment.Contracts.Infrastructure;
using Aovias.Deployment.Contracts.Validation;
using Aovias.DeploymentValidation.Services;
using Aovias.Publishing.Contracts;

namespace Aovias.DeploymentValidation.Models;

/// <summary>
/// Context passed through deployment readiness validation stages.
/// </summary>
public sealed class DeploymentValidationContext
{
    public required string PackagePath { get; init; }

    public DeployPackageContents? PackageContents { get; set; }

    public string? WorkingDirectory { get; init; }

    public string? StagingDirectory { get; init; }

    public string? ServiceImageTarPath { get; init; }

    public string? ExtractedPackageDirectory { get; set; }

    public string? LoadedImageReference { get; set; }

    public string? ValidationConfigRoot { get; init; }

    public string ValidationProfile { get; init; } = "local";

    public ValidationExecutionScope ExecutionScope { get; init; } = ValidationExecutionScope.Publishing;

    public DeploymentValidationMode ValidationMode { get; init; } = DeploymentValidationMode.Full;

    public bool UseStreamingValidation { get; init; }

    public int ContainerPort { get; init; } = 7080;

    /// <summary>
    /// Host port published by Docker during validation when compose uses dynamic port mapping.
    /// </summary>
    public int? ResolvedHostProbePort { get; set; }

    public string DockerExecutable { get; init; } = "docker";

    public int TimeoutSeconds { get; init; } = 900;

    public string ExpectedDockerNetwork { get; init; } = "aovias-network";

    public string? LogFilePath { get; set; }

    public List<string> CommandLog { get; } = [];

    public List<ApiProbeResult> ApiProbes { get; } = [];

    public TemporaryDeploymentWorkspace? Workspace { get; set; }

    public DeploymentValidationLogger? ValidationLogger { get; set; }

    public DrvResourceMonitor? ResourceMonitor { get; init; }

    public ServiceDeploymentBundle? DeploymentContracts { get; set; }

    public ContractValidationReport? ContractValidationReport { get; set; }

    public InfrastructureContract? InfrastructureContract { get; set; }

    public InfrastructureReadinessReport? InfrastructureReadinessReport { get; set; }

    public Action<string, string, string, int>? ReportProgress { get; init; }

    /// <summary>
    /// Resolves the container-side service port from the package manifest.
    /// </summary>
    public int ResolveContainerPort()
    {
        var manifestPort = PackageContents?.Manifest.Ports.FirstOrDefault();
        return manifestPort is > 0 ? manifestPort.Value : ContainerPort;
    }

    /// <summary>
    /// Resolves the host port used for runtime HTTP probes during validation.
    /// </summary>
    public int ResolveProbePort()
    {
        if (ResolvedHostProbePort is int hostPort and > 0)
        {
            return hostPort;
        }

        var manifestPort = DeploymentContracts?.Manifest.Docker.Ports.FirstOrDefault()
            ?? PackageContents?.DeploymentContracts?.Manifest.Docker.Ports.FirstOrDefault();
        if (manifestPort is > 0)
        {
            return manifestPort.Value;
        }

        return ResolveContainerPort();
    }
}
