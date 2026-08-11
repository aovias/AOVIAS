using ComplianceService.Application.Analytics;
using ComplianceService.Application.Contracts;
using ComplianceService.Application.DTOs;
using ComplianceService.Application.Events;
using ComplianceService.Domain;
using ComplianceService.Domain.Enums;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ComplianceService.Application.Services;

/// <summary>Sprint 10 — escalation engine (business state only, no delivery).</summary>
public sealed class EscalationService : IEscalationService
{
    private readonly ComplianceDbContext _db;
    private readonly IComplianceEventEmitter _eventEmitter;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<EscalationService> _logger;

    public EscalationService(
        ComplianceDbContext db,
        IComplianceEventEmitter eventEmitter,
        IUnitOfWork unitOfWork,
        ILogger<EscalationService> logger)
    {
        _db = db;
        _eventEmitter = eventEmitter;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<EscalationEvaluationResultDto> EvaluateAsync(
        Guid deficiencyId,
        SlaEvaluationResultDto sla,
        bool persist = true,
        CancellationToken ct = default)
    {
        var deficiency = await _db.ComplianceDeficiencies.FirstOrDefaultAsync(d => d.Id == deficiencyId, ct);
        if (deficiency == null || deficiency.Status is DeficiencyStatus.Closed or DeficiencyStatus.Cancelled)
        {
            return new EscalationEvaluationResultDto { DeficiencyId = deficiencyId };
        }

        if (!Enum.TryParse<SlaStatus>(sla.CurrentSlaStatus, out var slaStatus))
        {
            slaStatus = SlaStatus.OnTrack;
        }

        var active = await _db.DeficiencyEscalationHistories
            .Where(h => h.DeficiencyId == deficiencyId
                        && h.State != EscalationState.Resolved)
            .OrderByDescending(h => h.Level)
            .FirstOrDefaultAsync(ct);

        var result = new EscalationEvaluationResultDto
        {
            DeficiencyId = deficiencyId,
            CurrentLevel = deficiency.CurrentEscalationLevel,
            CurrentState = deficiency.CurrentEscalationState?.ToString(),
        };

        if (slaStatus == SlaStatus.OnTrack || slaStatus == SlaStatus.Closed)
        {
            return result;
        }

        if (slaStatus == SlaStatus.AtRisk && active == null)
        {
            deficiency.UpdateEscalationState(null, EscalationState.Pending);
            result.CurrentState = EscalationState.Pending.ToString();
            return result;
        }

        if (slaStatus != SlaStatus.Breached && slaStatus != SlaStatus.AtRisk)
        {
            return result;
        }

        if (active != null && active.State is EscalationState.Escalated or EscalationState.Acknowledged)
        {
            result.CurrentLevel = active.Level;
            result.CurrentState = active.State.ToString();
            result.TargetUserId = active.TargetUserId;
            result.TargetLabel = active.TargetLabel;
            return result;
        }

        var policy = deficiency.SlaPolicyId.HasValue
            ? await _db.SlaPolicies.AsNoTracking()
                .Include(p => p.EscalationPolicy!)
                .ThenInclude(p => p!.Levels)
                .FirstOrDefaultAsync(p => p.Id == deficiency.SlaPolicyId.Value, ct)
            : null;

        var levels = policy?.EscalationPolicy?.Levels.OrderBy(l => l.Level).ToList() ?? [];
        var targetLevel = levels.FirstOrDefault(l => slaStatus == SlaStatus.Breached
            ? sla.PercentConsumed >= l.TriggerAtSlaPercent
            : slaStatus == SlaStatus.AtRisk && l.Level == 1);

        if (targetLevel == null)
        {
            return result;
        }

        var task = await _db.ComplianceTasks.AsNoTracking()
            .Where(t => t.SourceDeficiencyId == deficiencyId && t.Status != ComplianceTaskStatus.Completed)
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync(ct);

        var (targetUserId, targetLabel) = await ResolveTargetAsync(
            targetLevel, deficiency, task, ct);

        var history = new DeficiencyEscalationHistory
        {
            Id = Guid.NewGuid(),
            DeficiencyId = deficiencyId,
            OrganizationId = deficiency.OrganizationId,
            HospitalId = deficiency.HospitalId,
            Level = targetLevel.Level,
            TargetUserId = targetUserId,
            TargetLabel = targetLabel,
            Reason = $"SLA {slaStatus} — {sla.PercentConsumed:F0}% consumed",
            State = slaStatus == SlaStatus.Breached ? EscalationState.Escalated : EscalationState.Pending,
            TriggeredAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            RowVersion = new byte[8],
        };

        if (persist)
        {
            await _db.DeficiencyEscalationHistories.AddAsync(history, ct);
            deficiency.UpdateEscalationState(history.Level, history.State);

            if (history.State == EscalationState.Escalated)
            {
                await _eventEmitter.EmitAsync(
                    ComplianceEventType.DeficiencyEscalated,
                    deficiencyId,
                    nameof(ComplianceDeficiency),
                    deficiency.OrganizationId,
                    null,
                    new
                    {
                        history.Level,
                        history.TargetUserId,
                        history.TargetLabel,
                        history.Reason,
                    },
                    ct);
            }

            result.WasEscalated = history.State == EscalationState.Escalated;
        }

        result.CurrentLevel = history.Level;
        result.CurrentState = history.State.ToString();
        result.TargetUserId = targetUserId;
        result.TargetLabel = targetLabel;

        _logger.LogInformation(
            "Escalation: Deficiency {DeficiencyId} level {Level} state {State} target {Target}.",
            deficiencyId, history.Level, history.State, targetLabel);

        return result;
    }

    public async Task<IReadOnlyList<DeficiencyEscalationHistoryDto>> GetHistoryAsync(
        Guid deficiencyId,
        CancellationToken ct = default) =>
        await _db.DeficiencyEscalationHistories.AsNoTracking()
            .Where(h => h.DeficiencyId == deficiencyId)
            .OrderByDescending(h => h.TriggeredAt)
            .Select(h => new DeficiencyEscalationHistoryDto
            {
                Id = h.Id,
                Level = h.Level,
                TargetUserId = h.TargetUserId,
                TargetLabel = h.TargetLabel,
                Reason = h.Reason,
                State = h.State.ToString(),
                TriggeredAt = h.TriggeredAt,
                AcknowledgedAt = h.AcknowledgedAt,
                ResolvedAt = h.ResolvedAt,
            })
            .ToListAsync(ct);

    public async Task<DeficiencyEscalationHistoryDto?> AcknowledgeAsync(
        Guid deficiencyId,
        Guid escalationId,
        string? acknowledgedBy,
        CancellationToken ct = default)
    {
        var history = await _db.DeficiencyEscalationHistories
            .FirstOrDefaultAsync(h => h.Id == escalationId && h.DeficiencyId == deficiencyId, ct);

        if (history == null || history.State is EscalationState.Acknowledged or EscalationState.Resolved)
        {
            return null;
        }

        history.State = EscalationState.Acknowledged;
        history.AcknowledgedAt = DateTime.UtcNow;
        history.AcknowledgedBy = acknowledgedBy;

        var deficiency = await _db.ComplianceDeficiencies.FirstAsync(d => d.Id == deficiencyId, ct);
        deficiency.UpdateEscalationState(history.Level, EscalationState.Acknowledged);

        await _eventEmitter.EmitAsync(
            ComplianceEventType.EscalationAcknowledged,
            deficiencyId,
            nameof(ComplianceDeficiency),
            deficiency.OrganizationId,
            null,
            new { history.Level, history.TargetUserId, AcknowledgedBy = acknowledgedBy },
            ct);

        await _unitOfWork.SaveChangesAsync(ct);

        return new DeficiencyEscalationHistoryDto
        {
            Id = history.Id,
            Level = history.Level,
            TargetUserId = history.TargetUserId,
            TargetLabel = history.TargetLabel,
            Reason = history.Reason,
            State = history.State.ToString(),
            TriggeredAt = history.TriggeredAt,
            AcknowledgedAt = history.AcknowledgedAt,
            ResolvedAt = history.ResolvedAt,
        };
    }

    public Task<SlaEscalationMetricsDto> GetMetricsAsync(Guid organizationId, CancellationToken ct = default) =>
        BuildMetricsAsync(
            _db.ComplianceDeficiencies.AsNoTracking()
                .Where(d => d.OrganizationId == organizationId
                            && d.Status != DeficiencyStatus.Closed && d.Status != DeficiencyStatus.Cancelled
                            && d.SlaPolicyId != null),
            organizationId,
            ct);

    public Task<SlaEscalationMetricsDto> GetMetricsForScopeAsync(
        Guid organizationId,
        Guid hospitalId,
        Guid frameworkId,
        CancellationToken ct = default) =>
        BuildMetricsAsync(
            _db.ComplianceDeficiencies.AsNoTracking()
                .Where(d => d.OrganizationId == organizationId
                            && d.HospitalId == hospitalId
                            && d.FrameworkId == frameworkId
                            && d.Status != DeficiencyStatus.Closed && d.Status != DeficiencyStatus.Cancelled
                            && d.SlaPolicyId != null),
            organizationId,
            hospitalId,
            frameworkId,
            ct);

    private async Task<SlaEscalationMetricsDto> BuildMetricsAsync(
        IQueryable<ComplianceDeficiency> openQuery,
        Guid organizationId,
        CancellationToken ct) =>
        await BuildMetricsAsync(openQuery, organizationId, null, null, ct);

    private async Task<SlaEscalationMetricsDto> BuildMetricsAsync(
        IQueryable<ComplianceDeficiency> openQuery,
        Guid organizationId,
        Guid? hospitalId,
        Guid? frameworkId,
        CancellationToken ct)
    {
        var open = await openQuery.ToListAsync(ct);

        var closedQuery = _db.ComplianceDeficiencies.AsNoTracking()
            .Where(d => d.OrganizationId == organizationId
                        && d.Status == DeficiencyStatus.Closed
                        && d.SlaStartDate != null
                        && d.ActualResolutionDate != null);
        if (hospitalId.HasValue)
        {
            closedQuery = closedQuery.Where(d => d.HospitalId == hospitalId);
        }

        if (frameworkId.HasValue && frameworkId.Value != Guid.Empty)
        {
            closedQuery = closedQuery.Where(d => d.FrameworkId == frameworkId);
        }

        var closedWithSla = await closedQuery.ToListAsync(ct);

        var deptMap = open.GroupBy(d => string.IsNullOrWhiteSpace(d.Department) ? "Unassigned" : d.Department!)
            .Select(g => new DeficiencyCountByLabelDto
            {
                Label = g.Key,
                Count = g.Count(d => d.CurrentSlaStatus is SlaStatus.OnTrack or SlaStatus.Closed),
            })
            .OrderByDescending(x => x.Count)
            .ToList();

        var breached = open.Count(d => d.CurrentSlaStatus == SlaStatus.Breached);
        var escalated = open.Count(d => d.CurrentEscalationState is EscalationState.Escalated or EscalationState.Acknowledged);

        return new SlaEscalationMetricsDto
        {
            TotalOpenWithSla = open.Count,
            OnTrackCount = open.Count(d => d.CurrentSlaStatus == SlaStatus.OnTrack),
            WarningCount = open.Count(d => d.CurrentSlaStatus == SlaStatus.Warning),
            AtRiskCount = open.Count(d => d.CurrentSlaStatus == SlaStatus.AtRisk),
            BreachedCount = breached,
            EscalatedCount = escalated,
            CriticalEscalations = open.Count(d =>
                d.Severity == DeficiencySeverity.Critical
                && d.CurrentEscalationState is EscalationState.Escalated or EscalationState.Acknowledged),
            BreachRate = open.Count > 0 ? (double)breached / open.Count : 0,
            EscalationRate = open.Count > 0 ? (double)escalated / open.Count : 0,
            AverageResolutionHours = closedWithSla.Count > 0
                ? closedWithSla.Average(d => (d.ActualResolutionDate!.Value - d.SlaStartDate!.Value).TotalHours)
                : 0,
            DepartmentSlaCompliance = deptMap,
        };
    }

    private async Task<(string? userId, string label)> ResolveTargetAsync(
        EscalationLevel level,
        ComplianceDeficiency deficiency,
        ComplianceTask? task,
        CancellationToken ct)
    {
        switch (level.TargetType)
        {
            case EscalationTargetType.TaskOwner:
                return (task?.AssignedToUserId, level.TargetLabel);

            case EscalationTargetType.User:
                return (level.TargetUserId, level.TargetLabel);

            case EscalationTargetType.Designation:
                return (level.TargetUserId, level.TargetDesignation ?? level.TargetLabel);

            case EscalationTargetType.DepartmentOwner:
            default:
                var deptCode = await ResolveDepartmentCodeAsync(deficiency, ct);
                if (deptCode == null)
                {
                    return (task?.AssignedToUserId, level.TargetLabel);
                }

                var dept = await _db.Departments.AsNoTracking()
                    .FirstOrDefaultAsync(
                        d => d.OrganizationId == deficiency.OrganizationId && d.Code == deptCode && d.IsActive,
                        ct);

                if (dept == null)
                {
                    return (task?.AssignedToUserId, level.TargetLabel);
                }

                var ownership = await _db.DepartmentOwnerships.AsNoTracking()
                    .Where(o => o.OrganizationId == deficiency.OrganizationId
                                && o.DepartmentId == dept.Id
                                && o.Status == DepartmentOwnershipStatus.Active
                                && (o.HospitalId == null || o.HospitalId == deficiency.HospitalId))
                    .OrderByDescending(o => o.HospitalId)
                    .ThenByDescending(o => o.EffectiveFrom)
                    .FirstOrDefaultAsync(ct);

                return (ownership?.PrimaryOwnerUserId ?? ownership?.BackupOwnerUserId, level.TargetLabel);
        }
    }

    private async Task<string?> ResolveDepartmentCodeAsync(ComplianceDeficiency deficiency, CancellationToken ct)
    {
        var oe = await _db.ObjectiveElements.AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == deficiency.ObjectiveElementId, ct);

        if (oe == null || string.IsNullOrWhiteSpace(oe.Department))
        {
            return null;
        }

        var mapping = await _db.DepartmentLabelMappings.AsNoTracking()
            .FirstOrDefaultAsync(m => m.OeDepartmentLabel == oe.Department && m.IsActive, ct);

        return mapping?.NormalizedCode
            ?? DepartmentLabelCatalog.Mappings.FirstOrDefault(m => m.Label == oe.Department).Code;
    }
}
