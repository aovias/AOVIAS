namespace Aovias.Publishing.Contracts.ReleaseLibrary;

/// <summary>
/// The newest published artifact for a service that is present on disk in the Release Library.
/// </summary>
public sealed record PublishedArtifactSelection
{
    public required ReleaseArtifactRecord Record { get; init; }

    public required string PackagePath { get; init; }
}
