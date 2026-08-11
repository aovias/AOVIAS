using ComplianceService.Application.Analytics;
using ComplianceService.Application.Commands;
using ComplianceService.Application.Contracts;
using ComplianceService.Application.Events;
using Microsoft.Extensions.Logging;

namespace ComplianceService.Application.Services;

public sealed class AcceptDeficiencyCommandHandler
{
    private readonly IDeficiencyRepository _repository;
    private readonly IDomainEventDispatcher _eventDispatcher;
    private readonly IDeficiencyAuditTrailService _auditTrail;
    private readonly IDeficiencyComplianceEventService _complianceEvents;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AcceptDeficiencyCommandHandler> _logger;

    public AcceptDeficiencyCommandHandler(
        IDeficiencyRepository repository,
        IDomainEventDispatcher eventDispatcher,
        IDeficiencyAuditTrailService auditTrail,
        IDeficiencyComplianceEventService complianceEvents,
        IUnitOfWork unitOfWork,
        ILogger<AcceptDeficiencyCommandHandler> logger)
    {
        _repository = repository;
        _eventDispatcher = eventDispatcher;
        _auditTrail = auditTrail;
        _complianceEvents = complianceEvents;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task HandleAsync(AcceptDeficiencyCommand command, CancellationToken ct = default)
    {
        var deficiency = await _repository.GetByIdAsync(command.DeficiencyId, ct)
            ?? throw new InvalidOperationException($"Deficiency {command.DeficiencyId} not found.");

        deficiency.AcceptOwnership(command.AcceptedBy);

        await _auditTrail.LogAsync(
            deficiency.Id,
            "OwnershipAccepted",
            null,
            command.AcceptedBy,
            null,
            ct);

        var events = deficiency.DomainEvents.ToList();
        if (events.Count > 0)
        {
            await _eventDispatcher.DispatchAsync(events, ct);
            deficiency.ClearDomainEvents();
        }

        await _unitOfWork.SaveChangesAsync(ct);

        await _complianceEvents.EmitCapaUpdatedAsync(
            deficiency,
            ComplianceEventType.DeficiencyAcceptedByOwner,
            $"Accepted by {command.AcceptedBy}",
            command.AcceptedBy,
            ct);

        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation(
            "AcceptDeficiency: Deficiency {Id} ownership accepted by {Actor}.",
            command.DeficiencyId, command.AcceptedBy);
    }
}

public sealed class RejectDeficiencyOwnershipCommandHandler
{
    private readonly IDeficiencyRepository _repository;
    private readonly IDeficiencyAuditTrailService _auditTrail;
    private readonly IDeficiencyComplianceEventService _complianceEvents;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RejectDeficiencyOwnershipCommandHandler> _logger;

    public RejectDeficiencyOwnershipCommandHandler(
        IDeficiencyRepository repository,
        IDeficiencyAuditTrailService auditTrail,
        IDeficiencyComplianceEventService complianceEvents,
        IUnitOfWork unitOfWork,
        ILogger<RejectDeficiencyOwnershipCommandHandler> logger)
    {
        _repository = repository;
        _auditTrail = auditTrail;
        _complianceEvents = complianceEvents;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task HandleAsync(RejectDeficiencyOwnershipCommand command, CancellationToken ct = default)
    {
        var deficiency = await _repository.GetByIdAsync(command.DeficiencyId, ct)
            ?? throw new InvalidOperationException($"Deficiency {command.DeficiencyId} not found.");

        deficiency.RejectOwnership(command.RejectedBy, command.Reason);

        await _auditTrail.LogAsync(
            deficiency.Id,
            "OwnershipRejected",
            null,
            command.RejectedBy,
            command.Reason,
            ct);

        await _unitOfWork.SaveChangesAsync(ct);

        await _complianceEvents.EmitCapaUpdatedAsync(
            deficiency,
            ComplianceEventType.DeficiencyOwnershipRejected,
            command.Reason ?? $"Rejected by {command.RejectedBy}",
            command.RejectedBy,
            ct);

        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation(
            "RejectDeficiencyOwnership: Deficiency {Id} ownership rejected by {Actor}.",
            command.DeficiencyId, command.RejectedBy);
    }
}
