using Shared.Infrastructure.Domain;

namespace ComplianceService.Application.Services;

/// <summary>Allocates human-readable CAPA numbers via platform sequences.</summary>
public static class CapaNumberAllocator
{
    public static async Task<string> AllocateAsync(
        IPlatformSequenceStore sequenceStore,
        Guid hospitalId,
        CancellationToken ct = default)
    {
        var prefix = $"CAPA-{hospitalId:N}";
        var seq = await sequenceStore.GetNextValueAsync(prefix, ct);
        return $"CAPA-{DateTime.UtcNow:yyyy}-{seq:D5}";
    }
}
