using Aovias.Release.Contracts.Packages;
using Aovias.ReleaseManager.Abstractions;
using MediatR;

namespace Aovias.Runtime.Agent.Application.Releases.Commands;

/// <summary>
/// Validates a release package.
/// </summary>
public sealed record ValidateReleaseCommand(string ReleaseId) : IRequest<PackageValidationResult>;

/// <summary>
/// Handles <see cref="ValidateReleaseCommand"/>.
/// </summary>
public sealed class ValidateReleaseCommandHandler : IRequestHandler<ValidateReleaseCommand, PackageValidationResult>
{
    private readonly IReleaseManager releaseManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidateReleaseCommandHandler"/> class.
    /// </summary>
    public ValidateReleaseCommandHandler(IReleaseManager releaseManager)
    {
        this.releaseManager = releaseManager;
    }

    /// <inheritdoc />
    public Task<PackageValidationResult> Handle(ValidateReleaseCommand request, CancellationToken cancellationToken)
        => releaseManager.ValidatePackageAsync(request.ReleaseId, cancellationToken);
}
