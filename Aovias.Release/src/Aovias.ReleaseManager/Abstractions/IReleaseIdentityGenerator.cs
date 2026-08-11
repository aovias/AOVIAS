namespace Aovias.ReleaseManager.Abstractions;

/// <summary>
/// Generates deployment-specific release identifiers.
/// </summary>
public interface IReleaseIdentityGenerator
{
    string GenerateReleaseId(DateTimeOffset timestamp);
}
