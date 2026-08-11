using Compliance.MasterData.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace Compliance.MasterData.Contracts
{
    public interface IMasterDataProvider
    {
        MasterDataManifest GetManifest();
        MasterDataRootDto GetData();
    }
}
