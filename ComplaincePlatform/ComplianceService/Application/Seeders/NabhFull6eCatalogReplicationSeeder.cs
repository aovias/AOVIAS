using Compliance.MasterData.Contracts;
using ComplianceService.Application.Contracts;
using Microsoft.Extensions.Logging;

namespace ComplianceService.Application.Seeders;

/// <summary>
/// After Entry Level master data is seeded (CP00000002), copies the catalog to Full 6E (CP00000001)
/// when Full 6E has no chapters yet.
/// </summary>
public sealed class NabhFull6eCatalogReplicationSeeder : IMasterDataSeeder
{
    public const string SourcePlatformId = "CP00000002";
    public const string TargetPlatformId = "CP00000001";

    private readonly IFrameworkCatalogReplicationService _replication;
    private readonly ILogger<NabhFull6eCatalogReplicationSeeder> _logger;

    public NabhFull6eCatalogReplicationSeeder(
        IFrameworkCatalogReplicationService replication,
        ILogger<NabhFull6eCatalogReplicationSeeder> logger)
    {
        _replication = replication;
        _logger = logger;
    }

    public async Task SeedAsync(IMasterDataPersistence persistence, CancellationToken ct = default)
    {
        _ = persistence;

        var result = await _replication.ReplicateIfTargetEmptyAsync(
            SourcePlatformId,
            TargetPlatformId,
            ct);

        if (result.Skipped)
        {
            _logger.LogInformation(
                "NABH Full 6E catalog replication skipped: {Message}",
                result.Message);
            return;
        }

        _logger.LogInformation(
            "NABH Full 6E catalog replication completed: {Chapters} chapters, {Standards} standards, {Oes} OEs.",
            result.InsertedChapters,
            result.InsertedStandards,
            result.InsertedObjectiveElements);
    }
}
