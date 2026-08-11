using Compliance.MasterData.Contracts;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Compliance.MasterData.Seeders.NABH
{
    public sealed class NabhEntryLevelSeeder : IMasterDataSeeder
    {
        private readonly IMasterDataProvider _provider;
        private readonly ILogger<NabhEntryLevelSeeder> _logger;

        public NabhEntryLevelSeeder(
            IMasterDataProvider provider,
            ILogger<NabhEntryLevelSeeder> logger)
        {
            _provider = provider;
            _logger = logger;
        }

        public async Task SeedAsync(IMasterDataPersistence persistence,
                                    CancellationToken ct = default)
        {
            var manifest = _provider.GetManifest();
            var data = _provider.GetData();

            // Check existence first (operations are idempotent, so transaction not strictly needed)
            if (await persistence.FrameworkExistsAsync(
                    manifest.Framework,
                    manifest.Version,
                    ct))
            {
                _logger.LogInformation(
                    "Master data already exists: {Framework} {Version}",
                    manifest.Framework,
                    manifest.Version);
                return;
            }

            try
            {
                // CreateFrameworkAsync is idempotent, so no transaction needed
                // Each operation handles its own idempotency
                var frameworkId = await persistence.CreateFrameworkAsync(
                    data.Framework, ct);

                foreach (var chapter in data.Chapters)
                {
                    var chapterId = await persistence.CreateChapterAsync(
                        frameworkId, chapter, ct);

                    foreach (var objectiveElementSeed in chapter.ObjectiveElements)
                    {
                        var objectiveElementId = await persistence.CreateObjectiveElementAsync(
                            chapterId, objectiveElementSeed, ct);

                        foreach (var req in objectiveElementSeed.Requirements)
                        {
                            await persistence.CreateRequirementAsync(
                                objectiveElementId, req, ct);
                        }
                    }
                }

                _logger.LogInformation(
                    "Seeded NABH Entry Level successfully ({Version})",
                    manifest.Version);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "NABH Entry Level seeding failed");
                throw;
            }
        }
    }

}
