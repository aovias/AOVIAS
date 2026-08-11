using ComplianceService.Application.Contracts;
using ComplianceService.Application.DTOs;
using ComplianceService.Domain;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Domain.Enums;

namespace ComplianceService.Application.Services;

public sealed class PatchFindingStatusCommandHandler
{
    private readonly ComplianceDbContext _db;
    private readonly IFindingAuditTrailService _auditTrail;

    public PatchFindingStatusCommandHandler(ComplianceDbContext db, IFindingAuditTrailService auditTrail)
    {
        _db = db;
        _auditTrail = auditTrail;
    }

    public async Task HandleAsync(
        Guid organizationId,
        Guid findingId,
        Guid userId,
        string status,
        CancellationToken ct = default)
    {
        if (!Enum.TryParse<FindingStatus>(status, ignoreCase: true, out var newStatus))
        {
            throw new ArgumentException($"Invalid status: {status}");
        }

        var finding = await _db.Findings
            .FirstOrDefaultAsync(f => f.Id == findingId && f.TenantId == organizationId, ct)
            ?? throw new InvalidOperationException("Finding not found.");

        var previous = finding.Status;
        if (previous == newStatus)
        {
            return;
        }

        finding.TransitionStatus(newStatus, userId);

        await _auditTrail.LogAsync(
            findingId,
            "StatusChanged",
            userId,
            "User",
            System.Text.Json.JsonSerializer.Serialize(new { status = previous.ToString() }),
            System.Text.Json.JsonSerializer.Serialize(new { status = newStatus.ToString() }),
            ct);

        await _db.SaveChangesAsync(ct);
    }
}
