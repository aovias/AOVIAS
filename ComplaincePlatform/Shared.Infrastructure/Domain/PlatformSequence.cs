using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.Infrastructure.Domain
{
    public class PlatformSequence
    {
        public int Id { get; set; }

        // Example: CFW, REG, CHP, CLS, REQ
        public string Prefix { get; set; } = default!;

        public long CurrentValue { get; set; }
    }

}
