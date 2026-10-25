using Aovias.Configuration.Deployment;
using Aovias.Release.Contracts.Releases;
using Aovias.ReleaseManager.Abstractions;
using MediatR;

namespace Aovias.Runtime.Agent.Application.Releases.Commands;

/// <summary>
/// Uploads a release package to the runtime release root.
/// </summary>
public sealed record UploadReleaseCommand(UploadPackageRequest Request) : IRequest<UploadPackageResult>;

/// <summary>
/// Handles <see cref="UploadReleaseCommand"/>.
/// </summary>
public sealed class UploadReleaseCommandHandler : IRequestHandler<UploadReleaseCommand, UploadPackageResult>
{
    private readonly IReleaseManager releaseManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="UploadReleaseCommandHandler"/> class.
    /// </summary>
    public UploadReleaseCommandHandler(IReleaseManager releaseManager)
    {
        this.releaseManager = releaseManager;
    }

    /// <inheritdoc />
    public Task<UploadPackageResult> Handle(UploadReleaseCommand request, CancellationToken cancellationToken)
    {
        DeploymentExecutionDiagnostics.LogPipelineStage(
            "Application",
            nameof(UploadReleaseCommandHandler),
            nameof(Handle));
        return releaseManager.UploadPackageAsync(request.Request, cancellationToken);
    }
}
