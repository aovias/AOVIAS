namespace ComplianceService.Application.Contracts;

/// <summary>
/// Copies a compliance framework catalog (chapters → standards → objective elements)
/// from a source framework to a target framework when the target is empty.
/// </summary>
public interface IFrameworkCatalogReplicationService
{
    /// <summary>
    /// Replicates catalog rows when the target has no chapters yet. No-op when target already populated.
    /// </summary>
    Task<FrameworkCatalogReplicationResult> ReplicateIfTargetEmptyAsync(
        string sourcePlatformId,
        string targetPlatformId,
        CancellationToken cancellationToken = default);
}

public sealed record FrameworkCatalogReplicationResult(
    bool Skipped,
    string Message,
    int SourceChapters = 0,
    int SourceStandards = 0,
    int SourceObjectiveElements = 0,
    int InsertedChapters = 0,
    int InsertedStandards = 0,
    int InsertedObjectiveElements = 0);
