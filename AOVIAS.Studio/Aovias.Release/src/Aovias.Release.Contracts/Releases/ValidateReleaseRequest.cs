namespace Aovias.Release.Contracts.Releases;

/// <summary>
/// Validate release request payload.
/// </summary>
public sealed class ValidateReleaseRequest
{
    public required string ReleaseId { get; init; }
}
