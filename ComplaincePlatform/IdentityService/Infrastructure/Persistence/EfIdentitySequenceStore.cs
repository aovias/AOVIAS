using IdentityService.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Domain;

namespace IdentityService.Infrastructure.Persistence
{
    public sealed class EfIdentitySequenceStore : IPlatformSequenceStore
    {
        private readonly IdentityDbContext _db;

        public EfIdentitySequenceStore(IdentityDbContext db)
        {
            _db = db;
        }

        public async Task<long> GetNextValueAsync(
            string prefix,
            CancellationToken ct = default)
        {
            var seq = await _db.PlatformSequences
                .FirstOrDefaultAsync(x => x.Prefix == prefix, ct);

            if (seq == null)
            {
                seq = new PlatformSequence
                {
                    Prefix = prefix,
                    CurrentValue = 0
                };
                _db.PlatformSequences.Add(seq);
            }

            seq.CurrentValue++;
            await _db.SaveChangesAsync(ct);

            return seq.CurrentValue;
        }
    }
}
