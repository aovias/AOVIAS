using System;
using System.Collections.Generic;
using System.Text;

namespace Compliance.MasterData.DTOs
{
    public class MasterDataManifest
    {
        public string Framework { get; set; }
        public string Version { get; set; }
        public string Authority { get; set; }
        public string Checksum { get; set; }
        public DateTime PublishedOn { get; set; }
    }
}
