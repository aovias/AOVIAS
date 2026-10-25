using ComplianceService.Application.Contracts;
using ComplianceService.Application.Events;
using ComplianceService.Domain;
using ComplianceService.Domain.Enums;

namespace ComplianceService.Application.Analytics;

/// <summary>
/// Emits standardized ComplianceEvents for deficiency lifecycle and CAPA actions (Sprint 12B-0).
/// </summary>
public interface IDeficiencyComplianceEventService
{
    Task EmitCreatedAsync(ComplianceDeficiency deficiency, CancellationToken ct = default);

    Task EmitStatusChangeAsync(
        ComplianceDeficiency deficiency,
        DeficiencyStatus previousStatus,
        DeficiencyStatus newStatus,
        string? actor,
        CancellationToken ct = default);

    Task EmitAssignedAsync(ComplianceDeficiency deficiency, string? previousOwner, string? actor, CancellationToken ct = default);

    Task EmitCapaUpdatedAsync(
        ComplianceDeficiency deficiency,
        ComplianceEventType eventType,
        string? detail,
        string? actor,
        CancellationToken ct = default);

    Task EmitSlaStatusChangedAsync(
        ComplianceDeficiency deficiency,
        SlaStatus? previous,
        SlaStatus current,
        CancellationToken ct = default);

    Task EmitEscalationResolvedAsync(ComplianceDeficiency deficiency, int level, string? actor, CancellationToken ct = default);
}

public sealed class DeficiencyComplianceEventService : IDeficiencyComplianceEventService
{
    private readonly IComplianceEventEmitter _emitter;

    public DeficiencyComplianceEventService(IComplianceEventEmitter emitter)
    {
        _emitter = emitter;
    }

    public Task EmitCreatedAsync(ComplianceDeficiency deficiency, CancellationToken ct = default) =>
        _emitter.EmitAsync(
            ComplianceEventType.DeficiencyCreated,
            deficiency.Id,
            nameof(ComplianceDeficiency),
            deficiency.OrganizationId,
            null,
            new { deficiency.DeficiencyCode, deficiency.Severity, deficiency.Status },
            ct);

    public async Task EmitStatusChangeAsync(
        ComplianceDeficiency deficiency,
        DeficiencyStatus previousStatus,
        DeficiencyStatus newStatus,
        string? actor,
        CancellationToken ct = default)
    {
        var eventType = newStatus switch
        {
            DeficiencyStatus.Closed => ComplianceEventType.DeficiencyClosed,
            DeficiencyStatus.Cancelled => ComplianceEventType.DeficiencyCancelled,
            DeficiencyStatus.Verified => ComplianceEventType.DeficiencyVerified,
            _ => ComplianceEventType.DeficiencyUpdated,
        };

        await _emitter.EmitAsync(
            eventType,
            deficiency.Id,
            nameof(ComplianceDeficiency),
            deficiency.OrganizationId,
            null,
            new { PreviousStatus = previousStatus.ToString(), NewStatus = newStatus.ToString(), Actor = actor },
            ct);
    }

    public Task EmitAssignedAsync(ComplianceDeficiency deficiency, string? previousOwner, string? actor, CancellationToken ct = default) =>
        _emitter.EmitAsync(
            ComplianceEventType.DeficiencyAssigned,
            deficiency.Id,
            nameof(ComplianceDeficiency),
            deficiency.OrganizationId,
            null,
            new { deficiency.AssignedTo, PreviousOwner = previousOwner, Actor = actor },
            ct);

    public Task EmitCapaUpdatedAsync(
        ComplianceDeficiency deficiency,
        ComplianceEventType eventType,
        string? detail,
        string? actor,
        CancellationToken ct = default) =>
        _emitter.EmitAsync(
            eventType,
            deficiency.Id,
            nameof(ComplianceDeficiency),
            deficiency.OrganizationId,
            null,
            new { Detail = detail, Actor = actor },
            ct);

    public Task EmitSlaStatusChangedAsync(
        ComplianceDeficiency deficiency,
        SlaStatus? previous,
        SlaStatus current,
        CancellationToken ct = default) =>
        _emitter.EmitAsync(
            ComplianceEventType.SlaStatusChanged,
            deficiency.Id,
            nameof(ComplianceDeficiency),
            deficiency.OrganizationId,
            null,
            new { Previous = previous?.ToString(), Current = current.ToString() },
            ct);

    public Task EmitEscalationResolvedAsync(ComplianceDeficiency deficiency, int level, string? actor, CancellationToken ct = default) =>
        _emitter.EmitAsync(
            ComplianceEventType.EscalationResolved,
            deficiency.Id,
            nameof(ComplianceDeficiency),
            deficiency.OrganizationId,
            null,
            new { Level = level, Actor = actor },
            ct);
}
