using ComplianceService.Application.Contracts;
using ComplianceService.Domain;

namespace ComplianceService.Application.Services;

/// <summary>
/// Projects compliance snapshots from domain events by delegating to <see cref="IComplianceFrameworkSnapshotRecalculationService"/>.
/// </summary>
public class ComplianceSnapshotProjector : IComplianceProjectionHandler
{
    private readonly IComplianceFrameworkSnapshotRecalculationService _recalculationService;
    private readonly ILogger<ComplianceSnapshotProjector> _logger;

    public ComplianceSnapshotProjector(
        IComplianceFrameworkSnapshotRecalculationService recalculationService,
        ILogger<ComplianceSnapshotProjector> logger)
    {
        _recalculationService = recalculationService;
        _logger = logger;
    }

    public async Task HandleAsync(ComplianceScoreUpdatedDomainEvent domainEvent, CancellationToken ct)
    {
        _logger.LogInformation(
            "Handling compliance update projection for HospitalId: {HospitalId}, FrameworkId: {FrameworkId}",
            domainEvent.HospitalId,
            domainEvent.FrameworkId);

        if (domainEvent.FrameworkId == Guid.Empty)
        {
            _logger.LogWarning(
                "Skipping snapshot projection: FrameworkId is empty for HospitalId {HospitalId}",
                domainEvent.HospitalId);
            return;
        }

        try
        {
            await _recalculationService.RecalculateAsync(domainEvent.HospitalId, domainEvent.FrameworkId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error handling compliance update projection for HospitalId: {HospitalId}, FrameworkId: {FrameworkId}",
                domainEvent.HospitalId,
                domainEvent.FrameworkId);
            throw;
        }
    }
}
