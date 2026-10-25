using ComplianceService.Application.Services;
using ComplianceService.Domain.Enums;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ComplianceService.Application.Services;

public sealed class PatchDeficiencyRootCauseCommandHandler
{
    private readonly ComplianceDbContext _db;
    private readonly PatchDeficiencyRcaCommandHandler _rcaHandler;

    public PatchDeficiencyRootCauseCommandHandler(ComplianceDbContext db, PatchDeficiencyRcaCommandHandler rcaHandler)
    {
        _db = db;
        _rcaHandler = rcaHandler;
    }

    public async Task HandleAsync(PatchDeficiencyRootCauseCommand command, CancellationToken ct = default)
    {
        await _rcaHandler.HandleAsync(new PatchDeficiencyRcaCommand(
            command.DeficiencyId,
            command.RootCauseCategory,
            null, null, null, null, null,
            null, null, null, null, null, null,
            command.UpdatedBy), ct);
    }
}

public sealed record PatchDeficiencyRootCauseCommand(
    Guid DeficiencyId,
    string RootCauseCategory,
    string? UpdatedBy);
