using ComplianceService.Application.DTOs;

namespace ComplianceService.Application.Contracts;

/// <summary>
/// Resolves program overview snapshot data: persisted <see cref="Domain.ComplianceSnapshot"/> row when present,
/// otherwise live OE-based scores for the active <see cref="Domain.ComplianceJourney"/> (and queues materialization).
/// </summary>
public interface IProgramOverviewSnapshotReadService
{
    Task<ProgramOverviewSnapshotDto> GetProgramOverviewAsync(
        Guid hospitalId,
        Guid? frameworkId,
        CancellationToken cancellationToken = default);
}
