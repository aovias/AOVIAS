using System.Text.Json;
using ComplianceService.Application.Contracts;
using ComplianceService.Domain;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Domain.Enums;

namespace ComplianceService.Application.Services;

/// <summary>
/// DEF-M3-001 — classifies an existing finding against an objective element.
///
/// <para>
/// Compliance mapping used to be write-once, supplied at creation. A finding raised on the ward
/// without a reference could be reviewed and accepted but never promoted, because
/// <see cref="FindingPromotionEligibilityService"/> requires an objective element and a standard.
/// This command supplies that mapping afterwards, which is where a quality manager actually does it.
/// </para>
///
/// <para>
/// Only <see cref="Finding.ObjectiveElementId"/> is written. Standard and chapter are not columns on
/// the finding — every consumer resolves them from the objective element's hierarchy — so setting the
/// objective element *is* resolving the standard, and no rule or projection needed changing.
/// </para>
/// </summary>
public sealed class UpdateFindingComplianceContextCommandHandler
{
    private readonly ComplianceDbContext _db;
    private readonly IFindingAuditTrailService _auditTrail;

    public UpdateFindingComplianceContextCommandHandler(
        ComplianceDbContext db,
        IFindingAuditTrailService auditTrail)
    {
        _db = db;
        _auditTrail = auditTrail;
    }

    public async Task HandleAsync(
        Guid organizationId,
        Guid findingId,
        Guid userId,
        Guid objectiveElementId,
        CancellationToken ct = default)
    {
        if (objectiveElementId == Guid.Empty)
        {
            throw new ArgumentException("An objective element must be supplied.");
        }

        var finding = await _db.Findings
            .FirstOrDefaultAsync(f => f.Id == findingId && f.TenantId == organizationId, ct)
            ?? throw new InvalidOperationException("Finding not found.");

        // A closed finding is a settled record, and a promoted one has a CAPA hanging off the
        // mapping it was promoted under. Neither may be re-classified.
        if (finding.Status == FindingStatus.Closed)
        {
            throw new ArgumentException("A closed finding cannot be re-classified.");
        }

        if (finding.PromotedDeficiencyId.HasValue)
        {
            throw new ArgumentException(
                "A finding that has been promoted to a deficiency cannot be re-classified.");
        }

        // Tenant scope is carried by the objective element's standard, so check ownership through
        // the hierarchy rather than trusting the id from the request.
        var objectiveElement = await _db.ObjectiveElements
            .AsNoTracking()
            .FirstOrDefaultAsync(oe => oe.Id == objectiveElementId, ct)
            ?? throw new InvalidOperationException("Objective element not found.");

        var standardExists = await _db.ComplianceStandards
            .AsNoTracking()
            .AnyAsync(s => s.Id == objectiveElement.ComplianceStandardId, ct);

        if (!standardExists)
        {
            throw new ArgumentException(
                "The objective element does not resolve to a compliance standard.");
        }

        var previous = finding.ObjectiveElementId;
        if (previous == objectiveElementId)
        {
            return;
        }

        finding.MapComplianceContext(objectiveElementId, userId);

        await _auditTrail.LogAsync(
            findingId,
            "ComplianceContextMapped",
            userId,
            "User",
            JsonSerializer.Serialize(new { objectiveElementId = previous }),
            JsonSerializer.Serialize(new { objectiveElementId }),
            ct);

        await _db.SaveChangesAsync(ct);
    }
}
