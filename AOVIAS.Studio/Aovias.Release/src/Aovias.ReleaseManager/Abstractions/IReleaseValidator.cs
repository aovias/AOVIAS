using Aovias.Release.Contracts.Packages;
using Aovias.Release.Contracts.Releases;

namespace Aovias.ReleaseManager.Abstractions;

/// <summary>
/// Validates release compatibility against runtime configuration.
/// </summary>
public interface IReleaseValidator
{
    Task<PackageValidationResult> ValidateReleaseAsync(ReleaseRecord release, CancellationToken cancellationToken = default);
}
