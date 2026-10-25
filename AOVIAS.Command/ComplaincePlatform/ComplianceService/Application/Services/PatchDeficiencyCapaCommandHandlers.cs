using ComplianceService.Application.Analytics;
using ComplianceService.Application.Contracts;
using ComplianceService.Application.Events;
using ComplianceService.Domain.Enums;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Domain;

namespace ComplianceService.Application.Services;

public sealed class PatchDeficiencyCorrectiveActionCommandHandler
{
    private readonly ComplianceDbContext _db;
    private readonly IDeficiencyAuditTrailService _auditTrail;
    private readonly ISlaEvaluationService _slaEvaluation;
    private readonly IDeficiencyComplianceEventService _complianceEvents;
    private readonly IPlatformSequenceStore _sequenceStore;

    public PatchDeficiencyCorrectiveActionCommandHandler(
        ComplianceDbContext db,
        IDeficiencyAuditTrailService auditTrail,
        ISlaEvaluationService slaEvaluation,
        IDeficiencyComplianceEventService complianceEvents,
        IPlatformSequenceStore sequenceStore)
    {
        _db = db;
        _auditTrail = auditTrail;
        _slaEvaluation = slaEvaluation;
        _complianceEvents = complianceEvents;
        _sequenceStore = sequenceStore;
    }

    public async Task HandleAsync(PatchDeficiencyCorrectiveActionCommand command, CancellationToken ct = default)
    {
        // Tenant filter deliberately applies here (Wave 2.2C).
        //
        // This loaded the deficiency by Id with `IgnoreQueryFilters()` and then mutated it, so a
        // caller holding another tenant's deficiency GUID could write to that tenant's record.
        // With the filter in place the row is simply not found and the load throws — which is also
        // the right response to a probe, since it does not reveal whether the id exists elsewhere.
        var deficiency = await _db.ComplianceDeficiencies
            .FirstOrDefaultAsync(d => d.Id == command.DeficiencyId, ct)
            ?? throw new InvalidOperationException($"Deficiency {command.DeficiencyId} not found.");

        CapaActionStatus? status = null;
        if (!string.IsNullOrWhiteSpace(command.Status)
            && Enum.TryParse<CapaActionStatus>(command.Status, ignoreCase: true, out var parsed))
        {
            status = parsed;
        }

        var wasCompleted = deficiency.CorrectiveActionStatus == CapaActionStatus.Completed;
        deficiency.UpdateCorrectiveAction(
            command.Summary,
            command.Owner,
            command.StartDate,
            command.TargetDate,
            command.CompletedDate,
            status,
            command.UpdatedBy);

        if (string.IsNullOrWhiteSpace(deficiency.CapaNumber) && deficiency.HasCapaActivity())
        {
            var capaNumber = await CapaNumberAllocator.AllocateAsync(_sequenceStore, deficiency.HospitalId, ct);
            deficiency.AssignCapaNumber(capaNumber);
        }

        var action = deficiency.CorrectiveActionStatus == CapaActionStatus.Completed && !wasCompleted
            ? "CorrectiveActionCompleted"
            : wasCompleted ? "CorrectiveActionUpdated" : "CorrectiveActionCreated";

        await _auditTrail.LogAsync(deficiency.Id, action, null, command.UpdatedBy ?? "User", command.Summary, ct);
        await _complianceEvents.EmitCapaUpdatedAsync(
            deficiency,
            ComplianceEventType.CorrectiveActionUpdated,
            command.Summary,
            command.UpdatedBy,
            ct);
        await _db.SaveChangesAsync(ct);
        await _slaEvaluation.EvaluateAsync(deficiency.Id, persist: true, ct);
    }
}

public sealed record PatchDeficiencyCorrectiveActionCommand(
    Guid DeficiencyId,
    string? Summary,
    string? Owner,
    DateTime? StartDate,
    DateTime? TargetDate,
    DateTime? CompletedDate,
    string? Status,
    string? UpdatedBy);

public sealed class PatchDeficiencyPreventiveActionCommandHandler
{
    private readonly ComplianceDbContext _db;
    private readonly IDeficiencyAuditTrailService _auditTrail;
    private readonly ISlaEvaluationService _slaEvaluation;
    private readonly IDeficiencyComplianceEventService _complianceEvents;

    public PatchDeficiencyPreventiveActionCommandHandler(
        ComplianceDbContext db,
        IDeficiencyAuditTrailService auditTrail,
        ISlaEvaluationService slaEvaluation,
        IDeficiencyComplianceEventService complianceEvents)
    {
        _db = db;
        _auditTrail = auditTrail;
        _slaEvaluation = slaEvaluation;
        _complianceEvents = complianceEvents;
    }

    public async Task HandleAsync(PatchDeficiencyPreventiveActionCommand command, CancellationToken ct = default)
    {
        var deficiency = await _db.ComplianceDeficiencies
            .FirstOrDefaultAsync(d => d.Id == command.DeficiencyId, ct)
            ?? throw new InvalidOperationException($"Deficiency {command.DeficiencyId} not found.");

        CapaActionStatus? status = null;
        if (!string.IsNullOrWhiteSpace(command.Status)
            && Enum.TryParse<CapaActionStatus>(command.Status, ignoreCase: true, out var parsed))
        {
            status = parsed;
        }

        var wasCompleted = deficiency.PreventiveActionStatus == CapaActionStatus.Completed;
        deficiency.UpdatePreventiveAction(
            command.Summary,
            command.Owner,
            command.TargetDate,
            command.CompletedDate,
            command.SuccessCriteria,
            status,
            command.UpdatedBy);

        var action = deficiency.PreventiveActionStatus == CapaActionStatus.Completed && !wasCompleted
            ? "PreventiveActionCompleted"
            : wasCompleted ? "PreventiveActionUpdated" : "PreventiveActionCreated";

        await _auditTrail.LogAsync(deficiency.Id, action, null, command.UpdatedBy ?? "User", command.Summary, ct);
        await _complianceEvents.EmitCapaUpdatedAsync(
            deficiency,
            ComplianceEventType.PreventiveActionUpdated,
            command.Summary,
            command.UpdatedBy,
            ct);
        await _db.SaveChangesAsync(ct);
        await _slaEvaluation.EvaluateAsync(deficiency.Id, persist: true, ct);
    }
}

public sealed record PatchDeficiencyPreventiveActionCommand(
    Guid DeficiencyId,
    string? Summary,
    string? Owner,
    DateTime? TargetDate,
    DateTime? CompletedDate,
    string? SuccessCriteria,
    string? Status,
    string? UpdatedBy);

public sealed class PatchDeficiencyEffectivenessReviewCommandHandler
{
    private readonly ComplianceDbContext _db;
    private readonly IDeficiencyAuditTrailService _auditTrail;
    private readonly IDeficiencyComplianceEventService _complianceEvents;

    public PatchDeficiencyEffectivenessReviewCommandHandler(
        ComplianceDbContext db,
        IDeficiencyAuditTrailService auditTrail,
        IDeficiencyComplianceEventService complianceEvents)
    {
        _db = db;
        _auditTrail = auditTrail;
        _complianceEvents = complianceEvents;
    }

    public async Task HandleAsync(PatchDeficiencyEffectivenessReviewCommand command, CancellationToken ct = default)
    {
        var deficiency = await _db.ComplianceDeficiencies
            .FirstOrDefaultAsync(d => d.Id == command.DeficiencyId, ct)
            ?? throw new InvalidOperationException($"Deficiency {command.DeficiencyId} not found.");

        EffectivenessReviewStatus? status = null;
        if (!string.IsNullOrWhiteSpace(command.Status)
            && Enum.TryParse<EffectivenessReviewStatus>(command.Status, ignoreCase: true, out var parsed))
        {
            status = parsed;
        }

        DateTime? scheduledDate = command.ScheduledDate;
        if (command.ReviewDaysOffset.HasValue && !scheduledDate.HasValue)
        {
            scheduledDate = DateTime.UtcNow.AddDays(command.ReviewDaysOffset.Value);
            status ??= EffectivenessReviewStatus.Scheduled;
        }

        deficiency.UpdateEffectivenessReview(
            status,
            scheduledDate,
            command.ReviewDaysOffset,
            command.ReviewDate,
            command.Reviewer,
            command.Notes,
            command.Outcome,
            command.UpdatedBy);

        var action = status switch
        {
            EffectivenessReviewStatus.Scheduled => "EffectivenessReviewScheduled",
            EffectivenessReviewStatus.Effective or EffectivenessReviewStatus.NotEffective => "EffectivenessReviewCompleted",
            _ => "EffectivenessReviewUpdated",
        };

        await _auditTrail.LogAsync(deficiency.Id, action, null, command.UpdatedBy ?? "User", command.Notes, ct);
        await _complianceEvents.EmitCapaUpdatedAsync(
            deficiency,
            ComplianceEventType.EffectivenessReviewUpdated,
            command.Notes,
            command.UpdatedBy,
            ct);
        await _db.SaveChangesAsync(ct);
    }
}

public sealed record PatchDeficiencyEffectivenessReviewCommand(
    Guid DeficiencyId,
    string? Status,
    DateTime? ScheduledDate,
    int? ReviewDaysOffset,
    DateTime? ReviewDate,
    string? Reviewer,
    string? Notes,
    string? Outcome,
    string? UpdatedBy);
