using ComplianceService.Application.Contracts;
using ComplianceService.Application.DTOs;
using ComplianceService.Domain;
using ComplianceService.Domain.Enums;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Domain.Enums;

namespace ComplianceService.Application.Services;

/// <summary>
/// Sprint 16 / Phase 4 Sprint 3 — Human-approved promotion bridge: Incident → Finding → Deficiency (CAPA SSOT).
/// Never writes the orphan Capas table. CAPA fields live on <see cref="ComplianceDeficiency"/>.
/// Incidents never auto-promote.
/// </summary>
public sealed class IncidentPromotionService : IIncidentPromotionService
{
    private readonly ComplianceDbContext _db;
    private readonly IFindingPromotionService _findingPromotion;
    private readonly ILogger<IncidentPromotionService> _logger;

    public IncidentPromotionService(
        ComplianceDbContext db,
        IFindingPromotionService findingPromotion,
        ILogger<IncidentPromotionService> logger)
    {
        _db = db;
        _findingPromotion = findingPromotion;
        _logger = logger;
    }

    public async Task<PromoteIncidentResultDto> PromoteToFindingAsync(
        Guid organizationId, Guid? hospitalId, Guid userId,
        PromoteIncidentFindingRequestDto request, CancellationToken ct = default)
    {
        var incident = await _db.Incidents
            .Include(i => i.Timeline)
            .FirstOrDefaultAsync(i => i.TenantId == organizationId && i.Id == request.IncidentId, ct)
            ?? throw new InvalidOperationException("Incident not found.");

        incident.EnsureMutable();

        if (incident.FindingId.HasValue)
            throw new InvalidOperationException("Incident is already linked to a finding.");

        var now = DateTime.UtcNow;
        var severity = MapSeverity(incident.Severity);
        var year = now.Year;
        var seq = await _db.Findings.CountAsync(
            f => f.TenantId == organizationId && f.Scope == FindingScope.Organization && f.CreatedAt.Year == year, ct);

        var finding = new Finding
        {
            Id = Guid.NewGuid(),
            TenantId = organizationId,
            Scope = FindingScope.Organization,
            FindingNumber = $"FND-{year}-{(seq + 1):D5}",
            Classification = FindingClassification.NonConformity,
            Title = incident.Title,
            Description = incident.Description,
            ClauseReference = incident.IncidentNumber,
            Observation = request.Observation ?? incident.Description,
            Recommendation = request.Recommendation,
            Severity = severity,
            Status = FindingStatus.Open,
            Department = incident.Department,
            RaisedBy = FindingRaisedBy.IncidentReview,
            DateRaised = now,
            AuditDate = incident.IncidentDateTime,
            CreatedAt = now,
            LastUpdatedAt = now,
            LastUpdatedBy = userId,
            CreatedByUserId = userId,
            EvidenceCount = 0,
            ComplianceStatus = ComplianceEvaluationStatus.NonCompliant,
            IsMandatory = false,
            RowVersion = new byte[8],
        };

        _db.Findings.Add(finding);
        incident.FindingId = finding.Id;
        // Legacy CapaId intentionally untouched — CAPA SSOT is ComplianceDeficiency via DeficiencyId.
        incident.LastUpdatedAt = now;
        incident.LastUpdatedByUserId = userId;

        AddTimelineEntry(incident.Id, userId, "PromotedToFinding", $"Promoted to finding {finding.FindingNumber}", now);

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Incident {IncId} promoted to finding {FndId}", incident.Id, finding.Id);

        return new PromoteIncidentResultDto
        {
            IncidentId = incident.Id,
            FindingId = finding.Id,
            Message = $"Finding {finding.FindingNumber} created from incident.",
        };
    }

    public async Task<PromoteIncidentResultDto> PromoteToDeficiencyAsync(
        Guid organizationId, Guid? hospitalId, Guid userId,
        PromoteIncidentDeficiencyRequestDto request, CancellationToken ct = default)
    {
        var incident = await _db.Incidents
            .Include(i => i.Timeline)
            .Include(i => i.RootCauseAnalysis)
            .FirstOrDefaultAsync(i => i.TenantId == organizationId && i.Id == request.IncidentId, ct)
            ?? throw new InvalidOperationException("Incident not found.");

        incident.EnsureMutable();

        if (incident.DeficiencyId.HasValue)
            throw new InvalidOperationException("Incident is already linked to a deficiency.");

        var findingId = request.FindingId ?? incident.FindingId
            ?? throw new InvalidOperationException("A finding must exist before promoting to deficiency. Promote to finding first.");

        // Finding → ComplianceDeficiency only (never Capas table).
        var result = await _findingPromotion.PromoteAsync(
            organizationId, findingId, hospitalId ?? Guid.Empty, userId, ct);

        var now = DateTime.UtcNow;
        incident.DeficiencyId = result.DeficiencyId;
        // Do not set CapaId — legacy field; CAPA SSOT is DeficiencyId → ComplianceDeficiency.
        incident.LastUpdatedAt = now;
        incident.LastUpdatedByUserId = userId;

        await ApplyRcaToDeficiencyCapaAsync(result.DeficiencyId, incident, userId, ct);

        AddTimelineEntry(
            incident.Id,
            userId,
            "PromotedToDeficiency",
            $"Promoted to deficiency {result.DeficiencyCode} (CAPA SSOT on ComplianceDeficiency)",
            now);

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Incident {IncId} promoted to deficiency {DefId}; Capas table not written",
            incident.Id, result.DeficiencyId);

        return new PromoteIncidentResultDto
        {
            IncidentId = incident.Id,
            FindingId = findingId,
            DeficiencyId = result.DeficiencyId,
            Message = $"Deficiency {result.DeficiencyCode} created from incident finding.",
        };
    }

    /// <summary>
    /// Maps incident RCA corrective/preventive + 5 Whys onto deficiency CAPA SSOT fields.
    /// </summary>
    private async Task ApplyRcaToDeficiencyCapaAsync(
        Guid deficiencyId, Incident incident, Guid userId, CancellationToken ct)
    {
        var deficiency = await _db.ComplianceDeficiencies
            .FirstOrDefaultAsync(d => d.Id == deficiencyId, ct);
        if (deficiency == null) return;

        deficiency.SetLinkedSourceType(nameof(DeficiencyOriginType.Incident), userId.ToString());

        var rca = incident.RootCauseAnalysis;
        if (rca == null) return;

        var fiveWhys = IncidentRcaMapper.ParseFiveWhys(rca.FiveWhysJson);
        RootCauseCategory? category = null;
        if (!string.IsNullOrWhiteSpace(rca.RootCauseCategory)
            && Enum.TryParse<RootCauseCategory>(rca.RootCauseCategory, true, out var parsed))
        {
            category = parsed;
        }

        deficiency.UpdateRca(
            category,
            problemStatement: incident.Title,
            rootCauseSummary: rca.RootCauseSummary,
            impactDescription: incident.Description,
            correctiveActionSummary: rca.CorrectiveActionSummary,
            preventiveActionSummary: rca.PreventiveActionSummary,
            why1: fiveWhys?.Why1,
            why2: fiveWhys?.Why2,
            why3: fiveWhys?.Why3,
            why4: fiveWhys?.Why4,
            why5: fiveWhys?.Why5,
            finalRootCause: fiveWhys?.FinalRootCause ?? rca.RootCauseSummary,
            updatedBy: userId.ToString());

        if (!string.IsNullOrWhiteSpace(rca.CorrectiveActionSummary))
        {
            deficiency.UpdateCorrectiveAction(
                rca.CorrectiveActionSummary,
                owner: null,
                startDate: DateTime.UtcNow,
                targetDate: null,
                completedDate: null,
                status: CapaActionStatus.InProgress,
                updatedBy: userId.ToString());
        }

        if (!string.IsNullOrWhiteSpace(rca.PreventiveActionSummary))
        {
            deficiency.UpdatePreventiveAction(
                rca.PreventiveActionSummary,
                owner: null,
                targetDate: null,
                completedDate: null,
                successCriteria: null,
                status: CapaActionStatus.InProgress,
                updatedBy: userId.ToString());
        }
    }

    /// <summary>
    /// Append timeline rows via DbSet to avoid EF collection-sync concurrency issues when
    /// existing entries were not loaded with the incident aggregate.
    /// </summary>
    private void AddTimelineEntry(Guid incidentId, Guid userId, string eventType, string description, DateTime occurredAt)
    {
        _db.IncidentTimelineEntries.Add(new IncidentTimelineEntry
        {
            Id = Guid.NewGuid(),
            IncidentId = incidentId,
            OccurredAt = occurredAt,
            EventType = eventType,
            Description = description,
            ActorUserId = userId,
            CreatedAt = occurredAt,
            RowVersion = new byte[8],
        });
    }

    private static FindingSeverity MapSeverity(IncidentSeverity severity) => severity switch
    {
        IncidentSeverity.Low => FindingSeverity.Informational,
        IncidentSeverity.Moderate => FindingSeverity.Minor,
        IncidentSeverity.Major => FindingSeverity.Major,
        IncidentSeverity.Critical => FindingSeverity.Critical,
        IncidentSeverity.Sentinel => FindingSeverity.Critical,
        _ => FindingSeverity.Minor,
    };
}
