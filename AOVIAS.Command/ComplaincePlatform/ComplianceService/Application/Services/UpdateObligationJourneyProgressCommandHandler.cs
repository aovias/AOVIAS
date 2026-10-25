using ComplianceService.Application.Contracts;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ComplianceService.Application.Services;

/// <summary>
/// Triggers obligation projection after journey wizard progress is persisted on
/// <see cref="Domain.ObjectiveJourney"/>. Operational fields are no longer written directly here.
/// </summary>
public class UpdateObligationJourneyProgressCommandHandler
{
    private readonly ComplianceDbContext _db;
    private readonly IObligationProjectionService _projectionService;
    private readonly ILogger<UpdateObligationJourneyProgressCommandHandler> _logger;

    public UpdateObligationJourneyProgressCommandHandler(
        ComplianceDbContext db,
        IObligationProjectionService projectionService,
        ILogger<UpdateObligationJourneyProgressCommandHandler> logger)
    {
        _db = db;
        _projectionService = projectionService;
        _logger = logger;
    }

    public async Task HandleAsync(
        Commands.UpdateObligationJourneyProgressCommand command,
        CancellationToken ct = default)
    {
        var obligation = await _db.OrganizationComplianceObligations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                o => o.OrganizationId == command.OrganizationId
                  && o.ObjectiveElementId == command.ObjectiveElementId,
                ct);

        if (obligation == null)
        {
            _logger.LogWarning(
                "ObligationJourneyUpdate: No obligation found for OrganizationId={OrgId} OeId={OeId}. " +
                "Sync may not have run yet.",
                command.OrganizationId, command.ObjectiveElementId);
            return;
        }

        await _projectionService.ProjectAsync(
            new ObligationProjectionRequest(
                obligation.Id,
                ObligationProjectionTriggers.JourneyUpdated),
            ct);

        _logger.LogInformation(
            "ObligationJourneyUpdate: Projected obligation {ObligationId} after journey progress change.",
            obligation.Id);
    }
}
