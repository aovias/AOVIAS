using KnowledgeBase.Domain.Entities;

namespace KnowledgeBase.Application.Contracts;

public interface IGapAnalysisRepository
{
    Task<GapAnalysisReport?> GetByAssessmentIdAsync(Guid assessmentId, CancellationToken cancellationToken = default);

    Task<GapAnalysisReport?> GetByIdWithItemsAsync(Guid reportId, CancellationToken cancellationToken = default);

    Task UpsertReportAsync(GapAnalysisReport report, CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
