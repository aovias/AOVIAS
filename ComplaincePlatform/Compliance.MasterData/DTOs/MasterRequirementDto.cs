using System;
using System.Collections.Generic;
using System.Text;

namespace Compliance.MasterData.DTOs
{
    public class MasterRequirementDto
    {
        public string Text { get; set; }
        public string Department { get; set; }
        public string Role { get; set; }
        public string Frequency { get; set; }
        public string EvidenceType { get; set; }
    }
}
