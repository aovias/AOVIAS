using ComplianceService.Application.Analytics;
using ComplianceService.Application.Commands;
using ComplianceService.Application.Contracts;
using ComplianceService.Domain;
using ComplianceService.Domain.Enums;
using Microsoft.Extensions.Logging;
using Shared.Infrastructure.Auth;

namespace ComplianceService.Application.Services;

public class PatchDeficiencyStatusCommandHandler
{
    private static readonly HashSet<string> ReviewerRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        ApplicationRoles.MANAGER,
        ApplicationRoles.ORG_ADMIN,
        ApplicationRoles.GROUP_ADMIN,
        ApplicationRoles.SUPER_ADMIN,
        ApplicationRoles.REGULATORY_ADMIN,
        ApplicationRoles.AUTHORITY,
    };

    private readonly IDeficiencyRepository _repository;
    private readonly IDomainEventDispatcher _eventDispatcher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IObligationProjectionService _obligationProjection;
    private readonly IDeficiencyComplianceEventService _complianceEvents;
    private readonly IUserContextService _userContext;
    private readonly ILogger<PatchDeficiencyStatusCommandHandler> _logger;

    public PatchDeficiencyStatusCommandHandler(
        IDeficiencyRepository repository,
        IDomainEventDispatcher eventDispatcher,
        IUnitOfWork unitOfWork,
        IObligationProjectionService obligationProjection,
        IDeficiencyComplianceEventService complianceEvents,
        IUserContextService userContext,
        ILogger<PatchDeficiencyStatusCommandHandler> logger)
    {
        _repository      = repository;
        _eventDispatcher = eventDispatcher;
        _unitOfWork      = unitOfWork;
        _obligationProjection = obligationProjection;
        _complianceEvents = complianceEvents;
        _userContext     = userContext;
        _logger          = logger;
    }

    public async Task HandleAsync(
        PatchDeficiencyStatusCommand command,
        CancellationToken ct = default)
    {
        if (!Enum.TryParse<DeficiencyStatus>(command.NewStatus, ignoreCase: true, out var newStatus))
        {
            throw new ArgumentException($"Invalid DeficiencyStatus value: '{command.NewStatus}'.");
        }

        var deficiency = await _repository.GetByIdAsync(command.DeficiencyId, ct)
            ?? throw new InvalidOperationException($"Deficiency {command.DeficiencyId} not found.");

        var obligationId = deficiency.ComplianceObligationId;
        var previousStatus = deficiency.Status;
        var hadActiveEscalation = deficiency.CurrentEscalationState
            is EscalationState.Escalated or EscalationState.Acknowledged;

        if (newStatus == DeficiencyStatus.Verified)
        {
            EnsureCanVerify(deficiency, command.UpdatedBy);
        }

        if (newStatus == DeficiencyStatus.Closed)
        {
            try
            {
                deficiency.EnsureCloseable();
            }
            catch (InvalidOperationException ex)
            {
                throw new ArgumentException(ex.Message, ex);
            }

            deficiency.Close(command.UpdatedBy);
        }
        else
        {
            deficiency.TransitionStatus(newStatus, command.UpdatedBy);
        }

        var events = deficiency.DomainEvents.ToList();
        if (events.Count > 0)
        {
            await _eventDispatcher.DispatchAsync(events, ct);
            deficiency.ClearDomainEvents();
        }

        await _unitOfWork.SaveChangesAsync(ct);

        await _complianceEvents.EmitStatusChangeAsync(
            deficiency, previousStatus, deficiency.Status, command.UpdatedBy, ct);
        if (newStatus == DeficiencyStatus.Closed && hadActiveEscalation)
        {
            await _complianceEvents.EmitEscalationResolvedAsync(
                deficiency, deficiency.CurrentEscalationLevel ?? 1, command.UpdatedBy, ct);
        }

        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation(
            "PatchDeficiencyStatus: Deficiency {Id} → {Status}.",
            command.DeficiencyId, newStatus);

        var trigger = newStatus == DeficiencyStatus.Closed
            ? ObligationProjectionTriggers.DeficiencyClosed
            : ObligationProjectionTriggers.DeficiencyReopened;

        await _obligationProjection.ProjectAsync(
            new ObligationProjectionRequest(obligationId, trigger),
            ct);
    }

    /// <summary>
    /// Only reviewer roles may set status → Verified when role claims are present.
    /// Soft block: assignee cannot self-verify.
    /// </summary>
    internal static void EnsureCanVerify(
        ComplianceDeficiency deficiency,
        string? updatedBy,
        IReadOnlyList<string> roles,
        Guid userId,
        string? email)
    {
        if (roles.Count > 0)
        {
            var canonical = roles
                .Select(RoleLegacyMap.ToCanonical)
                .Where(r => !string.IsNullOrWhiteSpace(r))
                .ToList();
            if (!canonical.Any(r => ReviewerRoles.Contains(r)))
            {
                throw new InvalidOperationException(
                    "Only a reviewer role (MANAGER, ORG_ADMIN, GROUP_ADMIN, SUPER_ADMIN, REGULATORY_ADMIN, AUTHORITY) can verify a CAPA/deficiency.");
            }
        }

        var verifierKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (userId != Guid.Empty)
        {
            verifierKeys.Add(userId.ToString());
        }

        if (!string.IsNullOrWhiteSpace(updatedBy))
        {
            verifierKeys.Add(updatedBy.Trim());
        }

        if (!string.IsNullOrWhiteSpace(email))
        {
            verifierKeys.Add(email.Trim());
        }

        if (!string.IsNullOrWhiteSpace(deficiency.AssignedTo)
            && verifierKeys.Contains(deficiency.AssignedTo.Trim()))
        {
            throw new InvalidOperationException(
                "Assignee cannot self-verify CAPA/deficiency; a different reviewer is required.");
        }
    }

    private void EnsureCanVerify(ComplianceDeficiency deficiency, string? updatedBy) =>
        EnsureCanVerify(deficiency, updatedBy, _userContext.Roles, _userContext.UserId, _userContext.Email);
}
