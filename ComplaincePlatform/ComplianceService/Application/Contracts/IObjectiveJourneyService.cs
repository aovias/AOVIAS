using ComplianceService.Application.DTOs;
using Shared.Infrastructure.Domain.Enums;

namespace ComplianceService.Application.Contracts;

public interface IObjectiveJourneyService
{
    Task<ObjectiveJourneyStateDto> GetAsync(
        Guid journeyId,
        Guid objectiveElementId,
        Guid tenantId,
        CancellationToken cancellationToken = default);

    Task<ObjectiveJourneyStateDto> UpsertDraftAsync(
        Guid journeyId,
        Guid objectiveElementId,
        Guid tenantId,
        SaveObjectiveJourneyDraftRequest request,
        CancellationToken cancellationToken = default);

    Task<ObjectiveJourneyStateDto> CompleteAsync(
        Guid journeyId,
        Guid objectiveElementId,
        Guid tenantId,
        Guid userId,
        IReadOnlyList<string> roles,
        CompleteObjectiveJourneyRequest request,
        OperationalMode? operationalMode,
        string? auditLifecycleMode,
        CancellationToken cancellationToken = default);
}
