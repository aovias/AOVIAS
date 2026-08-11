using System;
using System.Collections.Generic;
using System.Text;

namespace Compliance.MasterData.DTOs
{
    /// <summary>
    /// Seed payload for a single NABH objective element (and nested requirement lines) under a chapter.
    /// </summary>
    public class MasterObjectiveElementSeedDto
    {
        public string Code { get; set; }
        public string Description { get; set; }
        public string Severity { get; set; }
        public List<MasterRequirementDto> Requirements { get; set; }
    }
}
