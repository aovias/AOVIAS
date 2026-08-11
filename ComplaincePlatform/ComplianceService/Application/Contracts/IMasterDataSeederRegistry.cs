using Compliance.MasterData.Contracts;

namespace ComplianceService.Application.Contracts
{
    public interface IMasterDataSeederRegistry
    {
        IReadOnlyList<IMasterDataSeeder> GetSeeders();
    }

}
