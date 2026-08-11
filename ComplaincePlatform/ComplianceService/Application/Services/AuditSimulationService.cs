using ComplianceService.Application.Utilities;
using ComplianceService.Domain;
using ComplianceService.Domain.Enums;
using Shared.Infrastructure.Domain.Enums;

namespace ComplianceService.Application.Services;

/// <summary>
/// Service for simulating audit results based on compliance data.
/// Implements rules for detecting non-conformances and high-risk chapters.
/// Logic is isolated and does not integrate with scoring engine.
/// </summary>
public class AuditSimulationService
{
    private readonly ILogger<AuditSimulationService> _logger;

    public AuditSimulationService(ILogger<AuditSimulationService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Simulates an audit based on the provided compliance context.
    /// Applies rules to detect non-conformances and identify high-risk chapters.
    /// </summary>
    public AuditSimulationResult SimulateAudit(AuditSimulationContext context)
    {
        if (context == null)
        {
            _logger.LogWarning("AuditSimulationContext is null, returning empty result");
            return new AuditSimulationResult();
        }

        var result = new AuditSimulationResult();

        CheckMandatoryObjectiveElements(context, result);
        CheckOpenRisks(context, result);
        CheckChapterCompletion(context, result);

        _logger.LogInformation(
            "Audit simulation completed. MajorNC: {MajorNC}, MinorNC: {MinorNC}, Observations: {Obs}, OFI: {OFI}, HighRiskChapters: {Chapters}",
            result.MajorNCCount,
            result.MinorNCCount,
            result.ObservationCount,
            result.OFICount,
            result.HighRiskChapters.Count);

        return result;
    }

    private void CheckMandatoryObjectiveElements(AuditSimulationContext context, AuditSimulationResult result)
    {
        if (context.ObjectiveElements == null || context.ObjectiveElements.Count == 0)
        {
            return;
        }

        foreach (var oe in context.ObjectiveElements)
        {
            if (!oe.IsMandatory)
            {
                continue;
            }

            var oeTasks = context.Tasks?.Where(t => t.ObjectiveElementId == oe.Id).ToList()
                ?? new List<ComplianceTask>();

            if (oeTasks.Count == 0)
            {
                result.MajorNCCount++;
                _logger.LogDebug(
                    "Mandatory objective element {Code} has no tasks → MajorNC",
                    oe.Code);
                continue;
            }

            var hasIncompleteTasks = oeTasks.Any(t => !IsTaskComplete(t));
            if (hasIncompleteTasks)
            {
                result.MajorNCCount++;
                _logger.LogDebug(
                    "Mandatory objective element {Code} has incomplete tasks → MajorNC",
                    oe.Code);
            }
        }
    }

    private void CheckOpenRisks(AuditSimulationContext context, AuditSimulationResult result)
    {
        if (context.Risks == null || context.Risks.Count == 0)
        {
            return;
        }

        foreach (var risk in context.Risks)
        {
            if (!risk.Status.IsActive())
            {
                continue;
            }

            var nonConformanceType = NonConformanceMapper.MapFromRisk(risk);

            if (nonConformanceType.HasValue)
            {
                switch (nonConformanceType.Value)
                {
                    case NonConformanceType.MajorNC:
                        result.MajorNCCount++;
                        _logger.LogDebug(
                            "Open risk {RiskId} with Critical severity → MajorNC",
                            risk.Id);
                        break;

                    case NonConformanceType.MinorNC:
                        result.MinorNCCount++;
                        _logger.LogDebug(
                            "Open risk {RiskId} with Major severity → MinorNC",
                            risk.Id);
                        break;

                    case NonConformanceType.Observation:
                        result.ObservationCount++;
                        break;

                    case NonConformanceType.OFI:
                        result.OFICount++;
                        break;
                }
            }
        }
    }

    private void CheckChapterCompletion(AuditSimulationContext context, AuditSimulationResult result)
    {
        if (context.Chapters == null || context.Chapters.Count == 0)
        {
            return;
        }

        foreach (var chapter in context.Chapters)
        {
            var chapterOes = context.ObjectiveElements?
                .Where(oe => oe.ComplianceStandard != null && oe.ComplianceStandard.ChapterId == chapter.Id)
                .ToList() ?? new List<ObjectiveElement>();

            if (chapterOes.Count == 0)
            {
                continue;
            }

            int completed = 0;
            foreach (var oe in chapterOes)
            {
                var oeTasks = context.Tasks?.Where(t => t.ObjectiveElementId == oe.Id).ToList()
                    ?? new List<ComplianceTask>();

                if (oeTasks.Count > 0 && oeTasks.All(t => IsTaskComplete(t)))
                {
                    completed++;
                }
            }

            var completionPercentage = chapterOes.Count > 0
                ? (decimal)completed / chapterOes.Count * 100
                : 0;

            if (completionPercentage < 50)
            {
                result.HighRiskChapters.Add(new HighRiskChapter
                {
                    ChapterId = chapter.Id,
                    ChapterCode = chapter.Code,
                    ChapterName = chapter.Name,
                    CompletionPercentage = completionPercentage,
                    TotalObjectiveElements = chapterOes.Count,
                    CompletedObjectiveElements = completed
                });

                _logger.LogDebug(
                    "Chapter {ChapterCode} has {Completion}% completion (< 50%) → High Risk",
                    chapter.Code,
                    completionPercentage);
            }
        }
    }

    private bool IsTaskComplete(ComplianceTask task) =>
        task != null && ComplianceTaskStatusExtensions.IsCompletedForScoring(task.Status.ToScoringStatusString());
}

/// <summary>
/// Context for audit simulation containing all necessary compliance data.
/// </summary>
public class AuditSimulationContext
{
    public List<ComplianceChapter>? Chapters { get; set; }

    public List<ObjectiveElement>? ObjectiveElements { get; set; }

    public List<ComplianceTask>? Tasks { get; set; }

    public List<Risk>? Risks { get; set; }
}
