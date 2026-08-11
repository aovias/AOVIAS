namespace Aovias.ReleaseManager.Services;

/// <summary>
/// Generates REL-YYYYMMDD-#### release identifiers.
/// </summary>
public sealed class ReleaseIdentityGenerator : Abstractions.IReleaseIdentityGenerator
{
    private int sequence;

    /// <inheritdoc />
    public string GenerateReleaseId(DateTimeOffset timestamp)
    {
        var next = Interlocked.Increment(ref sequence);
        return $"REL-{timestamp:yyyyMMdd}-{next:0000}";
    }
}
