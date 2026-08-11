using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.Infrastructure.Domain
{
    public interface IPlatformSequenceStore
    {
        Task<long> GetNextValueAsync(
            string prefix,
            CancellationToken ct = default);
    }
}
