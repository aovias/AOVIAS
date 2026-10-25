using Aovias.PackageBuilder.Abstractions;
using Aovias.Release.Contracts.Packages;
using Aovias.Release.Contracts.Releases;

namespace Aovias.ReleaseManager.Services;

/// <summary>
/// Validates release records using the package validator.
/// </summary>
public sealed class ReleaseValidator : Abstractions.IReleaseValidator
{
    private readonly IPackageValidator packageValidator;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReleaseValidator"/> class.
    /// </summary>
    public ReleaseValidator(IPackageValidator packageValidator)
    {
        this.packageValidator = packageValidator;
    }

    /// <inheritdoc />
    public Task<PackageValidationResult> ValidateReleaseAsync(ReleaseRecord release, CancellationToken cancellationToken = default)
        => packageValidator.ValidateAsync(release.PackagePath, cancellationToken);
}
