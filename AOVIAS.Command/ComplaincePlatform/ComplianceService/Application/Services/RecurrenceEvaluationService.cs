using ComplianceService.Application.Contracts;
using ComplianceService.Application.DTOs;
using ComplianceService.Domain;
using ComplianceService.Domain.Enums;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ComplianceService.Application.Services;

/// <summary>Sprint 11 — detect recurring deficiencies by OE, root cause, department, type.</summary>
public sealed class RecurrenceEvaluationService : IRecurrenceEvaluationService
{
    private readonly ComplianceDbContext _db;
    private readonly IDeficiencyAuditTrailService _auditTrail;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RecurrenceEvaluationService> _logger;

    public RecurrenceEvaluationService(
        ComplianceDbContext db,
        IDeficiencyAuditTrailService auditTrail,
        IUnitOfWork unitOfWork,
        ILogger<RecurrenceEvaluationService> logger)
    {
        _db = db;
        _auditTrail = auditTrail;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<RecurrenceEvaluationResultDto> EvaluateAsync(
        Guid deficiencyId,
        int lookbackDays = 90,
        bool persist = true,
        CancellationToken ct = default)
    {
        var deficiency = await _db.ComplianceDeficiencies
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == deficiencyId, ct);

        if (deficiency == null)
        {
            return new RecurrenceEvaluationResultDto { DeficiencyId = deficiencyId, LookbackDays = lookbackDays };
        }

        var since = DateTime.UtcNow.AddDays(-lookbackDays);
        var query = _db.ComplianceDeficiencies.AsNoTracking()
            .Where(d => d.Id != deficiencyId
                        && d.HospitalId == deficiency.HospitalId
                        && d.ObjectiveElementId == deficiency.ObjectiveElementId
                        && d.DetectedDate >= since
                        && d.Status != DeficiencyStatus.Cancelled);

        if (deficiency.RootCauseCategory.HasValue && deficiency.RootCauseCategory != RootCauseCategory.None)
        {
            query = query.Where(d => d.RootCauseCategory == deficiency.RootCauseCategory);
        }

        if (!string.IsNullOrWhiteSpace(deficiency.Department))
        {
            query = query.Where(d => d.Department == deficiency.Department);
        }

        if (deficiency.DeficiencyType != default)
        {
            query = query.Where(d => d.DeficiencyType == deficiency.DeficiencyType);
        }

        var matches = await query.CountAsync(ct);
        var isRecurring = matches > 0;
        var reason = isRecurring
            ? $"{matches + 1} similar issue(s) on OE {deficiency.ObjectiveElementId} in last {lookbackDays} days"
            : null;

        if (persist && isRecurring)
        {
            var tracked = await _db.ComplianceDeficiencies.FirstOrDefaultAsync(d => d.Id == deficiencyId, ct);
            if (tracked != null && !tracked.IsRecurringIssue)
            {
                tracked.MarkRecurrence(true, reason, "recurrence-engine");
                await _auditTrail.LogAsync(
                    deficiencyId,
                    "RecurrenceDetected",
                    null,
                    "System",
                    reason,
                    ct);
                await _unitOfWork.SaveChangesAsync(ct);

                _logger.LogInformation(
                    "Recurrence: Deficiency {Id} flagged recurring ({Matches} prior matches).",
                    deficiencyId, matches);
            }
        }

        return new RecurrenceEvaluationResultDto
        {
            DeficiencyId = deficiencyId,
            IsRecurring = isRecurring || deficiency.IsRecurringIssue,
            Reason = reason ?? deficiency.RecurrenceReason,
            MatchCount = matches,
            LookbackDays = lookbackDays,
        };
    }
}
