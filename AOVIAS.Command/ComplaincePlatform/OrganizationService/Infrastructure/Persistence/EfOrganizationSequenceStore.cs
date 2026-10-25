using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Domain;

namespace OrganizationService.Infrastructure.Persistence
{
    public sealed class EfOrganizationSequenceStore : IPlatformSequenceStore
    {
        private readonly OrganizationDbContext _db;

        public EfOrganizationSequenceStore(OrganizationDbContext db)
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
