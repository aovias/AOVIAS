using System;
using System.Collections.Generic;
using System.Text;

namespace Compliance.MasterData.DTOs
{
    public class MasterDataRootDto
    {
        public MasterFrameworkDto Framework { get; set; }
        public List<MasterChapterDto> Chapters { get; set; }
    }
}
