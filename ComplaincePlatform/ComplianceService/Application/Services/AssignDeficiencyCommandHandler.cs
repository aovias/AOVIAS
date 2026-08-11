using ComplianceService.Application.Analytics;
using ComplianceService.Application.Commands;
using ComplianceService.Application.Contracts;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ComplianceService.Application.Services;

public class AssignDeficiencyCommandHandler
{
    private readonly IDeficiencyRepository _repository;
    private readonly IDomainEventDispatcher _eventDispatcher;
    private readonly IDeficiencyTaskOwnershipSyncService _ownershipSync;
    private readonly IDeficiencyAuditTrailService _auditTrail;
    private readonly IDeficiencyComplianceEventService _complianceEvents;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AssignDeficiencyCommandHandler> _logger;

    public AssignDeficiencyCommandHandler(
        IDeficiencyRepository repository,
        IDomainEventDispatcher eventDispatcher,
        IDeficiencyTaskOwnershipSyncService ownershipSync,
        IDeficiencyAuditTrailService auditTrail,
        IDeficiencyComplianceEventService complianceEvents,
        IUnitOfWork unitOfWork,
        ILogger<AssignDeficiencyCommandHandler> logger)
    {
        _repository = repository;
        _eventDispatcher = eventDispatcher;
        _ownershipSync = ownershipSync;
        _auditTrail = auditTrail;
        _complianceEvents = complianceEvents;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task HandleAsync(
        AssignDeficiencyCommand command,
        CancellationToken ct = default)
    {
        var deficiency = await _repository.GetByIdAsync(command.DeficiencyId, ct)
            ?? throw new InvalidOperationException($"Deficiency {command.DeficiencyId} not found.");

        var previousOwner = deficiency.AssignedTo;
        deficiency.Assign(command.AssignedTo, command.DueDate, command.UpdatedBy);

        await _auditTrail.LogAsync(
            deficiency.Id,
            string.IsNullOrWhiteSpace(previousOwner) ? "Assigned" : "OwnerChanged",
            null,
            command.UpdatedBy ?? "User",
            null,
            ct);
        var events = deficiency.DomainEvents.ToList();
        if (events.Count > 0)
        {
            await _eventDispatcher.DispatchAsync(events, ct);
            deficiency.ClearDomainEvents();
        }

        await _unitOfWork.SaveChangesAsync(ct);

        await _complianceEvents.EmitAssignedAsync(deficiency, previousOwner, command.UpdatedBy, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        await _ownershipSync.SyncTaskOwnerFromDeficiencyAsync(deficiency, command.UpdatedBy, ct);

        _logger.LogInformation(
            "AssignDeficiency: Deficiency {Id} assigned to '{AssignedTo}' due {Due}.",
            command.DeficiencyId, command.AssignedTo, command.DueDate?.ToString("yyyy-MM-dd") ?? "none");
    }
}
