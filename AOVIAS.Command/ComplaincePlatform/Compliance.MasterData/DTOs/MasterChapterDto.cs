using System;
using System.Collections.Generic;
using System.Text;

namespace Compliance.MasterData.DTOs
{
    public class MasterChapterDto
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public int Sequence { get; set; }
        public List<MasterObjectiveElementSeedDto> ObjectiveElements { get; set; }
    }
}
