using Compliance.MasterData.Contracts;
using ComplianceService.Application.Contracts;

namespace ComplianceService.Application.Services
{
    public sealed class MasterDataSeederRunner
    {
        private readonly IMasterDataSeederRegistry _registry;
        private readonly IMasterDataPersistence _persistence;
        private readonly ILogger<MasterDataSeederRunner> _logger;

        public MasterDataSeederRunner(
            IMasterDataSeederRegistry registry,
            IMasterDataPersistence persistence,
            ILogger<MasterDataSeederRunner> logger)
        {
            _registry = registry;
            _persistence = persistence;
            _logger = logger;
        }

        public async Task RunAsync(CancellationToken ct = default)
        {
            var seeders = _registry.GetSeeders();

            _logger.LogInformation(
                "Starting master data seeding. Seeders count: {Count}",
                seeders.Count);

            foreach (var seeder in seeders)
            {
                var name = seeder.GetType().Name;

                try
                {
                    _logger.LogInformation("Running seeder: {Seeder}", name);
                    // Use ConfigureAwait(false) to avoid deadlocks
                    await seeder.SeedAsync(_persistence, ct).ConfigureAwait(false);
                    _logger.LogInformation("Seeder completed: {Seeder}", name);
                }
                catch (Exception ex)
                {
                    _logger.LogCritical(
                        ex,
                        "Seeder FAILED: {Seeder}. Aborting startup.",
                        name);

                    throw; // fail fast – compliance data must be correct
                }
            }
        }
    }

}
