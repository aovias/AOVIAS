using ComplianceService.Application.Aggregation.Models;
using ComplianceService.Domain;

namespace ComplianceService.Application.Aggregation
{
    public interface IAggregationService
    {
        Task<FrameworkAggregateResult> ComputeFrameworkAsync(
            Guid frameworkId,
            Guid journeyId,
            IReadOnlyCollection<ObjectiveElementClassification> includedClassifications,
            CancellationToken cancellationToken = default);

    Task<FrameworkAggregateResult> ComputeFrameworkAsync(
        Guid frameworkId,
        Guid journeyId,
        IReadOnlyList<ComplianceChapter> preloadedChapters,
        IReadOnlyList<ComplianceStandard> preloadedStandards,
        IReadOnlyList<ObjectiveElement> preloadedObjectiveElements,
        CancellationToken cancellationToken = default);
    }
}

