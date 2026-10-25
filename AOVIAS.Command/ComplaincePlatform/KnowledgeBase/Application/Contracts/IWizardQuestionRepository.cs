using KnowledgeBase.Domain.Entities;

namespace KnowledgeBase.Application.Contracts;

public interface IWizardQuestionRepository
{
    /// <summary>
    /// Returns a deterministic, risk-weighted, category-balanced selection of Core-level
    /// questions for the given framework, capped at <paramref name="maxCount"/>.
    /// </summary>
    Task<IReadOnlyList<WizardQuestion>> GetCoreAssessmentQuestionsAsync(
        Guid frameworkId,
        int maxCount,
        CancellationToken ct = default);
}
