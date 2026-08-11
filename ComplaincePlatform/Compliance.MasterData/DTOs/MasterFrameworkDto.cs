using System;
using System.Collections.Generic;
using System.Text;

namespace Compliance.MasterData.DTOs
{
    public record MasterFrameworkDto(
     string Name,
     string Version,
     string Authority
 );
}
