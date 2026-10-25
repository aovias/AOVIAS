using ComplianceService.Application.Contracts;
using ComplianceService.Application.Events;
using ComplianceService.Application.Scheduling;
using ComplianceService.Domain;
using ComplianceService.Domain.Enums;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ComplianceService.Application.Services;

/// <summary>Sprint 10 / Phase 2 Sprint 5 — SLA clock evaluation with business-calendar awareness.</summary>
public sealed class SlaEvaluationService : ISlaEvaluationService
{
    private readonly ComplianceDbContext _db;
    private readonly ISlaPolicyService _policyService;
    private readonly IEscalationService _escalationService;
    private readonly IComplianceEventEmitter _eventEmitter;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IBusinessCalendarProvider _businessCalendar;
    private readonly ILogger<SlaEvaluationService> _logger;

    public SlaEvaluationService(
        ComplianceDbContext db,
        ISlaPolicyService policyService,
        IEscalationService escalationService,
        IComplianceEventEmitter eventEmitter,
        IUnitOfWork unitOfWork,
        IBusinessCalendarProvider businessCalendar,
        ILogger<SlaEvaluationService> logger)
    {
        _db = db;
        _policyService = policyService;
        _escalationService = escalationService;
        _eventEmitter = eventEmitter;
        _unitOfWork = unitOfWork;
        _businessCalendar = businessCalendar;
        _logger = logger;
    }

    public async Task AssignSlaToDeficiencyAsync(ComplianceDeficiency deficiency, CancellationToken ct = default)
    {
        if (deficiency.SlaPolicyId.HasValue)
        {
            return;
        }

        var policy = await _policyService.ResolvePolicyForDeficiencyAsync(deficiency, ct);
        if (policy == null)
        {
            return;
        }

        var start = deficiency.DetectedDate;
        var calendar = await _businessCalendar.GetAsync(deficiency.OrganizationId, ct);
        var due = BusinessCalendarCalculator.AddWorkingHours(
            start,
            policy.TargetResolutionHours,
            calendar.WorkingDays,
            calendar.Holidays,
            calendar.WorkingHoursStart,
            calendar.WorkingHoursEnd);

        deficiency.AssignSla(policy.Id, start, due);

        _logger.LogDebug(
            "SLA: Assigned policy {PolicyId} to deficiency {DeficiencyId}, due {Due} (working hours).",
            policy.Id, deficiency.Id, due);
    }

    public async Task<SlaEvaluationResultDto> EvaluateAsync(
        Guid deficiencyId,
        bool persist = true,
        CancellationToken ct = default)
    {
        var deficiency = await _db.ComplianceDeficiencies
            .FirstOrDefaultAsync(d => d.Id == deficiencyId, ct);

        if (deficiency == null)
        {
            return new SlaEvaluationResultDto { DeficiencyId = deficiencyId };
        }

        if (deficiency.Status == DeficiencyStatus.Closed || deficiency.Status == DeficiencyStatus.Cancelled)
        {
            return await BuildResultAsync(deficiency, null, SlaStatus.Closed, ct);
        }

        if (!deficiency.SlaPolicyId.HasValue)
        {
            await AssignSlaToDeficiencyAsync(deficiency, ct);
        }

        if (!deficiency.SlaPolicyId.HasValue || !deficiency.SlaStartDate.HasValue || !deficiency.SlaDueDate.HasValue)
        {
            if (persist)
            {
                await _unitOfWork.SaveChangesAsync(ct);
            }

            return await BuildResultAsync(deficiency, null, deficiency.CurrentSlaStatus ?? SlaStatus.OnTrack, ct);
        }

        var policy = await _db.SlaPolicies.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == deficiency.SlaPolicyId.Value, ct);

        var previousStatus = deficiency.CurrentSlaStatus ?? SlaStatus.OnTrack;
        var calendar = await _businessCalendar.GetAsync(deficiency.OrganizationId, ct);
        var newStatus = await CalculateStatusAsync(
            deficiency.SlaStartDate.Value,
            deficiency.SlaDueDate.Value,
            policy?.WarningThresholdPercent ?? 80,
            calendar,
            ct);

        if (newStatus != previousStatus)
        {
            deficiency.UpdateSlaStatus(newStatus);
            if (persist)
            {
                await EmitStatusEventAsync(deficiency, previousStatus, newStatus, ct);
            }
        }

        var result = await BuildResultAsync(deficiency, policy, newStatus, ct);

        if (persist)
        {
            await _escalationService.EvaluateAsync(deficiencyId, result, persist: true, ct);
            await _unitOfWork.SaveChangesAsync(ct);
        }

        return result;
    }

    public async Task<IReadOnlyList<SlaEvaluationResultDto>> EvaluateBatchAsync(
        IReadOnlyList<Guid> deficiencyIds,
        bool persist = true,
        CancellationToken ct = default)
    {
        var results = new List<SlaEvaluationResultDto>();
        foreach (var id in deficiencyIds)
        {
            results.Add(await EvaluateAsync(id, persist, ct));
        }

        return results;
    }

    /// <summary>Calendar-hour status (legacy / unit tests).</summary>
    internal static SlaStatus CalculateStatus(DateTime start, DateTime due, int warningThresholdPercent)
    {
        var now = DateTime.UtcNow;
        var totalHours = (due - start).TotalHours;
        if (totalHours <= 0)
        {
            return now > due ? SlaStatus.Breached : SlaStatus.OnTrack;
        }

        var elapsedHours = (now - start).TotalHours;
        var percent = elapsedHours / totalHours * 100.0;

        if (now > due)
        {
            return SlaStatus.Breached;
        }

        if (percent >= 90)
        {
            return SlaStatus.AtRisk;
        }

        if (percent >= warningThresholdPercent)
        {
            return SlaStatus.Warning;
        }

        return SlaStatus.OnTrack;
    }

    /// <summary>Working-hour aware status using business calendar.</summary>
    internal static SlaStatus CalculateStatusWithCalendar(
        DateTime start,
        DateTime due,
        int warningThresholdPercent,
        DateTime nowUtc,
        BusinessCalendarSnapshot calendar)
    {
        if (nowUtc > due)
        {
            return SlaStatus.Breached;
        }

        var totalWorking = BusinessCalendarCalculator.CountWorkingHours(
            start, due, calendar.WorkingDays, calendar.Holidays, calendar.WorkingHoursStart, calendar.WorkingHoursEnd);
        if (totalWorking <= 0)
        {
            return CalculateStatus(start, due, warningThresholdPercent);
        }

        var elapsedWorking = BusinessCalendarCalculator.CountWorkingHours(
            start, nowUtc, calendar.WorkingDays, calendar.Holidays, calendar.WorkingHoursStart, calendar.WorkingHoursEnd);
        var percent = elapsedWorking / totalWorking * 100.0;

        if (percent >= 90)
        {
            return SlaStatus.AtRisk;
        }

        if (percent >= warningThresholdPercent)
        {
            return SlaStatus.Warning;
        }

        return SlaStatus.OnTrack;
    }

    private Task<SlaStatus> CalculateStatusAsync(
        DateTime start,
        DateTime due,
        int warningThresholdPercent,
        BusinessCalendarSnapshot calendar,
        CancellationToken ct)
    {
        _ = ct;
        return Task.FromResult(CalculateStatusWithCalendar(start, due, warningThresholdPercent, DateTime.UtcNow, calendar));
    }

    private async Task<SlaEvaluationResultDto> BuildResultAsync(
        ComplianceDeficiency deficiency,
        SlaPolicy? policy,
        SlaStatus status,
        CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var start = deficiency.SlaStartDate ?? deficiency.DetectedDate;
        var due = deficiency.SlaDueDate ?? deficiency.DueDate ?? start;
        var calendar = await _businessCalendar.GetAsync(deficiency.OrganizationId, ct);

        var totalHours = Math.Max(
            BusinessCalendarCalculator.CountWorkingHours(
                start, due, calendar.WorkingDays, calendar.Holidays, calendar.WorkingHoursStart, calendar.WorkingHoursEnd),
            0.01);
        var elapsed = Math.Max(
            BusinessCalendarCalculator.CountWorkingHours(
                start, now, calendar.WorkingDays, calendar.Holidays, calendar.WorkingHoursStart, calendar.WorkingHoursEnd),
            0);
        var remaining = now >= due
            ? 0
            : Math.Max(
                BusinessCalendarCalculator.CountWorkingHours(
                    now, due, calendar.WorkingDays, calendar.Holidays, calendar.WorkingHoursStart, calendar.WorkingHoursEnd),
                0);
        var overdue = now > due
            ? BusinessCalendarCalculator.CountWorkingHours(
                due, now, calendar.WorkingDays, calendar.Holidays, calendar.WorkingHoursStart, calendar.WorkingHoursEnd)
            : 0;
        var percent = Math.Min(elapsed / totalHours * 100.0, 999);
        var hoursPerDay = Math.Max((calendar.WorkingHoursEnd - calendar.WorkingHoursStart).TotalHours, 1);

        return new SlaEvaluationResultDto
        {
            DeficiencyId = deficiency.Id,
            SlaPolicyId = deficiency.SlaPolicyId,
            PolicyName = policy?.Name,
            SlaStartDate = deficiency.SlaStartDate,
            SlaDueDate = deficiency.SlaDueDate,
            CurrentSlaStatus = status.ToString(),
            ElapsedHours = Math.Round(elapsed, 2),
            RemainingHours = Math.Round(remaining, 2),
            OverdueHours = Math.Round(overdue, 2),
            PercentConsumed = Math.Round(percent, 1),
            DaysRemaining = remaining > 0 ? (int)Math.Ceiling(remaining / hoursPerDay) : 0,
            IsOverdue = overdue > 0,
        };
    }

    private async Task EmitStatusEventAsync(
        ComplianceDeficiency deficiency,
        SlaStatus previous,
        SlaStatus current,
        CancellationToken ct)
    {
        if (previous == current)
        {
            return;
        }

        var eventType = current switch
        {
            SlaStatus.Warning => ComplianceEventType.DeficiencySlaWarning,
            SlaStatus.AtRisk => ComplianceEventType.DeficiencyAtRisk,
            SlaStatus.Breached => ComplianceEventType.DeficiencySlaBreached,
            _ => (ComplianceEventType?)null,
        };

        if (eventType == null)
        {
            return;
        }

        await _eventEmitter.EmitAsync(
            eventType.Value,
            deficiency.Id,
            nameof(ComplianceDeficiency),
            deficiency.OrganizationId,
            null,
            new
            {
                deficiency.Severity,
                PreviousStatus = previous.ToString(),
                CurrentStatus = current.ToString(),
                deficiency.SlaDueDate,
            },
            ct);
    }
}
