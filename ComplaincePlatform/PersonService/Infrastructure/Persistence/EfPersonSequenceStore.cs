using Microsoft.EntityFrameworkCore;
using PersonService.Infrastructure.Persistence;
using Shared.Infrastructure.Domain;
using Shared.Infrastructure.Services;

namespace PersonService.Infrastructure.Persistence;

public class EfPersonSequenceStore : IPlatformSequenceStore
{
    private readonly PersonDbContext _db;

    public EfPersonSequenceStore(PersonDbContext db) => _db = db;

    public async Task<long> GetNextValueAsync(string prefix, CancellationToken ct = default)
    {
        var sequence = await _db.PlatformSequences.FirstOrDefaultAsync(s => s.Prefix == prefix, ct);
        if (sequence == null)
        {
            sequence = new PlatformSequence { Prefix = prefix, CurrentValue = 0 };
            _db.PlatformSequences.Add(sequence);
        }
        sequence.CurrentValue++;
        await _db.SaveChangesAsync(ct);
        return sequence.CurrentValue;
    }
}
