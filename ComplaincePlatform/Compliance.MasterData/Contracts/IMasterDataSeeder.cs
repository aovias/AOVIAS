using System;
using System.Collections.Generic;
using System.Text;

namespace Compliance.MasterData.Contracts
{
    public interface IMasterDataSeeder
    {
        Task SeedAsync(IMasterDataPersistence persistence,
                     CancellationToken ct = default);
    }
}
