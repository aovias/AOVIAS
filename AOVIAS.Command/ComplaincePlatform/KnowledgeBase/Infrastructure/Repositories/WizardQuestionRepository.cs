using KnowledgeBase.Application.Contracts;
using KnowledgeBase.Domain.Entities;
using KnowledgeBase.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeBase.Infrastructure.Repositories;

/// <summary>
/// Retrieves WizardQuestion rows from the pre-materialised master bank.
/// Selection is deterministic (not random): mandatory-first, then by weight desc,
/// then by display priority, with at least one question per category before filling remaining slots.
/// </summary>
public sealed class WizardQuestionRepository : IWizardQuestionRepository
{
    private readonly KnowledgeBaseContext _db;

    public WizardQuestionRepository(KnowledgeBaseContext db) => _db = db;

    public async Task<IReadOnlyList<WizardQuestion>> GetCoreAssessmentQuestionsAsync(
        Guid frameworkId,
        int maxCount,
        CancellationToken ct = default)
    {
        var allCore = await _db.WizardQuestions
            .Where(q => q.FrameworkId == frameworkId
                     && q.IsActive
                     && q.AssessmentMaturityLevel == "Core")
            .OrderBy(q => q.IsMandatory ? 0 : 1)
            .ThenByDescending(q => q.Weight)
            .ThenBy(q => q.DisplayPriority)
            .AsNoTracking()
            .ToListAsync(ct);

        if (allCore.Count == 0)
            return allCore;

        // Phase 1: ensure at least one representative per category
        var selectedIds = new HashSet<Guid>();
        var byCategory = allCore.GroupBy(q => q.Category);

        foreach (var group in byCategory)
        {
            var first = group.First(); // already ordered by priority within category
            selectedIds.Add(first.Id);
        }

        // Phase 2: fill remaining slots with next-best questions not yet selected
        foreach (var q in allCore)
        {
            if (selectedIds.Count >= maxCount) break;
            selectedIds.Add(q.Id);
        }

        // Return in original deterministic order
        return allCore
            .Where(q => selectedIds.Contains(q.Id))
            .ToList();
    }
}
