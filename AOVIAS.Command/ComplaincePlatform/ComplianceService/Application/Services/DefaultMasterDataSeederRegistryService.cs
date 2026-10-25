using Compliance.MasterData.Contracts;
using Compliance.MasterData.Seeders.NABH;
using ComplianceService.Application.Contracts;
using ComplianceService.Application.Seeders;

namespace ComplianceService.Application.Services
{
    public class DefaultMasterDataSeederRegistryService : IMasterDataSeederRegistry
    {
        private readonly IEnumerable<IMasterDataSeeder> _seeders;

        public DefaultMasterDataSeederRegistryService(
            IEnumerable<IMasterDataSeeder> seeders)
        {
            _seeders = seeders;
        }

        public IReadOnlyList<IMasterDataSeeder> GetSeeders()
        {
            // Explicit order: Entry Level JSON seed first, then Full 6E catalog clone.
            return _seeders
                .OrderBy(s => s switch
                {
                    NabhEntryLevelSeeder => 0,
                    NabhFull6eCatalogReplicationSeeder => 1,
                    _ => 99
                })
                .ThenBy(s => s.GetType().Name)
                .ToList();
        }
    }
}
