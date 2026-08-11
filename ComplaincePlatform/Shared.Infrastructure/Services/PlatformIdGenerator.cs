using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.Infrastructure.Services
{
    public interface IPlatformIdGenerator
    {
        Task<string> GenerateAsync(string prefix,
            CancellationToken ct = default);
    }
    public class PlatformIdGenerator : IPlatformIdGenerator
    {
        private readonly IPlatformSequenceStore _store;

        public PlatformIdGenerator(IPlatformSequenceStore store)
        {
            _store = store;
        }

        public async Task<string> GenerateAsync(
            string prefix,
            CancellationToken ct = default)
        {
            var next = await _store.GetNextValueAsync(prefix, ct);
            return $"{prefix}{next:D6}";
        }
    }

}
