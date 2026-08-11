using ComplianceService.Application.Analytics;
using ComplianceService.Application.Contracts;
using ComplianceService.Application.DTOs;
using ComplianceService.Application.Journey;
using ComplianceService.Domain.Enums;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ComplianceService.Application.Services;

public sealed class AuditorTraceabilityService : IAuditorTraceabilityService
{
    private readonly ComplianceDbContext _db;
    private readonly IAnalyticsAggregationService _analytics;

    public AuditorTraceabilityService(ComplianceDbContext db, IAnalyticsAggregationService analytics)
    {
        _db = db;
        _analytics = analytics;
    }

    public async Task<EvidenceTraceabilityDto?> GetTraceabilityAsync(
        Guid organizationId, Guid hospitalId, Guid objectiveElementId, CancellationToken ct = default)
    {
        var oe = await _db.ObjectiveElements.AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == objectiveElementId, ct);
        if (oe == null) return null;

        var timeline = new List<TraceabilityTimelineEntryDto>();

        timeline.Add(new TraceabilityTimelineEntryDto
        {
            Layer = "ObjectiveElement",
            Label = oe.Code,
            Status = "Active",
            Detail = oe.Description,
            EntityId = oe.Id,
        });

        var obligations = await _db.OrganizationComplianceObligations.AsNoTracking()
            .Where(o => o.ObjectiveElementId == objectiveElementId && o.OrganizationId == organizationId)
            .ToListAsync(ct);

        foreach (var ob in obligations)
        {
            timeline.Add(new TraceabilityTimelineEntryDto
            {
                Layer = "Obligation",
                Label = ob.Code,
                Status = ob.AssessmentStatus.ToString(),
                EntityId = ob.Id,
                Detail = ob.Description ?? ob.Title,
            });
        }

        var evidenceLinks = await _db.ObjectiveElementEvidences.AsNoTracking()
            .Include(e => e.Evidence)
            .Where(e => e.ObjectiveElementId == objectiveElementId)
            .OrderBy(e => e.LinkedAt)
            .ToListAsync(ct);

        foreach (var link in evidenceLinks)
        {
            timeline.Add(new TraceabilityTimelineEntryDto
            {
                Layer = "Evidence",
                Label = link.Evidence?.Title ?? "Evidence",
                Status = link.VerificationStatus.ToString(),
                Timestamp = link.VerifiedAt ?? link.LinkedAt,
                EntityId = link.EvidenceId,
                Detail = link.ReviewComment,
            });
        }

        var deficiencies = await _db.ComplianceDeficiencies.AsNoTracking()
            .Where(d => d.ObjectiveElementId == objectiveElementId && d.HospitalId == hospitalId)
            .OrderBy(d => d.CreatedAt)
            .ToListAsync(ct);

        foreach (var def in deficiencies)
        {
            timeline.Add(new TraceabilityTimelineEntryDto
            {
                Layer = "Deficiency",
                Label = def.DeficiencyCode,
                Status = def.Status.ToString(),
                Timestamp = def.CreatedAt,
                EntityId = def.Id,
                Detail = def.Title,
            });

            if (def.CorrectiveActionStatus != CapaActionStatus.NotStarted
                || def.PreventiveActionStatus != CapaActionStatus.NotStarted)
            {
                timeline.Add(new TraceabilityTimelineEntryDto
                {
                    Layer = "CAPA",
                    Label = $"CAPA for {def.DeficiencyCode}",
                    Status = $"CA:{def.CorrectiveActionStatus} PA:{def.PreventiveActionStatus}",
                    Timestamp = def.CorrectiveActionCompletedDate ?? def.CreatedAt,
                    EntityId = def.Id,
                    Detail = def.CorrectiveActionSummary,
                });
            }
        }

        decimal? readiness = null;
        var journey = await GetActiveJourneyForTraceAsync(hospitalId, ct);
        if (journey != null)
        {
            var score = await _db.ObjectiveElementScores.AsNoTracking()
                .FirstOrDefaultAsync(s => s.JourneyId == journey.Id && s.ObjectiveElementId == objectiveElementId, ct);
            readiness = score?.EvidenceSampleCompliancePercent;

            timeline.Add(new TraceabilityTimelineEntryDto
            {
                Layer = "Readiness",
                Label = "Current Readiness",
                Status = readiness.HasValue ? $"{readiness:F1}%" : "Not Assessed",
                Timestamp = score?.AssessedAt,
            });
        }

        return new EvidenceTraceabilityDto
        {
            ObjectiveElementId = objectiveElementId,
            ObjectiveElementCode = oe.Code,
            ObjectiveElementDescription = oe.Description,
            Timeline = timeline,
            CurrentReadinessScore = readiness,
        };
    }

    private async Task<Domain.ComplianceJourney?> GetActiveJourneyForTraceAsync(Guid hospitalId, CancellationToken ct) =>
        await _db.ComplianceJourneys.IgnoreQueryFilters().AsNoTracking()
            .Where(j => j.HospitalId == hospitalId)
            .OrderByDescending(j => j.UpdatedAt)
            .FirstOrDefaultAsync(ct);
}
