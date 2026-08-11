using KnowledgeBase.Application.AssessmentSummary;
using KnowledgeBase.Application.Contracts;
using KnowledgeBase.Application.DTOs;
using KnowledgeBase.Application.GapAnalysis;
using KnowledgeBase.Domain.Entities;

namespace KnowledgeBase.Application.Services;

public sealed class GapAnalysisAppService
{
    private readonly AssessmentContextBuilder _contextBuilder;
    private readonly IAssessmentSummaryCalculator _calculator;
    private readonly GapAnalysisOrchestrator _orchestrator;
    private readonly IGapAnalysisRepository _gapAnalysis;

    public GapAnalysisAppService(
        AssessmentContextBuilder contextBuilder,
        IAssessmentSummaryCalculator calculator,
        GapAnalysisOrchestrator orchestrator,
        IGapAnalysisRepository gapAnalysis)
    {
        _contextBuilder = contextBuilder;
        _calculator = calculator;
        _orchestrator = orchestrator;
        _gapAnalysis = gapAnalysis;
    }

    public async Task<GapAnalysisReportDto> GenerateAsync(
        GenerateGapAnalysisCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.AssessmentId == Guid.Empty)
            throw new ArgumentException("AssessmentId is required.");

        var context = await _contextBuilder.BuildAsync(command.AssessmentId, cancellationToken);
        if (context.Assessment.Items.Count == 0)
            throw new InvalidOperationException("Assessment has no items.");

        var summaryResult = _calculator.Calculate(context.ToSummaryInput());
        var summaryDto = ToSummaryDto(summaryResult);

        var reportId = Guid.NewGuid();
        var gapItems = await _orchestrator.BuildItemsAsync(context, summaryResult, reportId, cancellationToken);

        var report = new GapAnalysisReport
        {
            Id = reportId,
            AssessmentId = context.Assessment.Id,
            FrameworkId = context.Assessment.FrameworkId,
            FacilityId = context.Assessment.FacilityId,
            GeneratedAt = DateTime.UtcNow,
            OverallPercent = summaryResult.OverallPercent,
            MeetsOverallPercent = summaryResult.MeetsOverallPercent,
            CoreRulePassed = summaryResult.CoreRulePassed,
            ChapterAveragesPassed = summaryResult.ChapterAveragesPassed,
            StandardAveragesPassed = summaryResult.StandardAveragesPassed,
            CommitmentLevelPassed = summaryResult.CommitmentLevelPassed,
            CriticalCount = gapItems.Count(i => i.Category == GapCategory.Critical.ToString()),
            MajorCount = gapItems.Count(i => i.Category == GapCategory.Major.ToString()),
            MinorCount = gapItems.Count(i => i.Category == GapCategory.Minor.ToString()),
            CompliantCount = gapItems.Count(i => i.Category == GapCategory.Compliant.ToString()),
            Items = gapItems.ToList()
        };

        await _gapAnalysis.UpsertReportAsync(report, cancellationToken);
        await _gapAnalysis.SaveChangesAsync(cancellationToken);

        var persisted = await _gapAnalysis.GetByAssessmentIdAsync(context.Assessment.Id, cancellationToken)
            ?? report;

        return ToDto(persisted, summaryDto);
    }

    public async Task<GapAnalysisReportDto?> GetByAssessmentIdAsync(
        GetGapAnalysisByAssessmentQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.AssessmentId == Guid.Empty)
            throw new ArgumentException("AssessmentId is required.");

        var report = await _gapAnalysis.GetByAssessmentIdAsync(query.AssessmentId, cancellationToken);
        if (report is null)
            return null;

        var context = await _contextBuilder.BuildAsync(query.AssessmentId, cancellationToken);
        var summaryResult = _calculator.Calculate(context.ToSummaryInput());
        return ToDto(report, ToSummaryDto(summaryResult));
    }

    public async Task<GapAnalysisReportDto?> GetByIdAsync(Guid reportId, CancellationToken cancellationToken = default)
    {
        var report = await _gapAnalysis.GetByIdWithItemsAsync(reportId, cancellationToken);
        if (report is null)
            return null;

        var context = await _contextBuilder.BuildAsync(report.AssessmentId, cancellationToken);
        var summaryResult = _calculator.Calculate(context.ToSummaryInput());
        return ToDto(report, ToSummaryDto(summaryResult));
    }

    private static AssessmentSummaryResponseDto ToSummaryDto(AssessmentSummaryResult result) =>
        new(
            result.OverallPercent,
            result.MeetsOverallPercent,
            result.CoreRulePassed,
            result.CoreFailureNodeIds,
            result.ChapterAverages,
            result.StandardAverages,
            result.ChapterAveragesPassed,
            result.StandardAveragesPassed,
            result.CommitmentLevelPassed,
            result.TooManyLowScoresInStandard,
            result.StandardsWithMultipleLowScores,
            result.ActionPlanViolationNodeIds);

    private static GapAnalysisReportDto ToDto(GapAnalysisReport report, AssessmentSummaryResponseDto summary) =>
        new(
            report.Id,
            report.AssessmentId,
            report.FrameworkId,
            report.FacilityId,
            report.GeneratedAt,
            report.OverallPercent,
            report.MeetsOverallPercent,
            report.CoreRulePassed,
            report.ChapterAveragesPassed,
            report.StandardAveragesPassed,
            report.CommitmentLevelPassed,
            report.CriticalCount,
            report.MajorCount,
            report.MinorCount,
            report.CompliantCount,
            summary,
            report.Items.Select(i => new GapAnalysisItemDto(
                i.NodeId,
                i.NodeCode,
                i.NodeTitle,
                i.ChapterId,
                i.StandardId,
                i.Score,
                i.ComplianceStatus,
                i.Classification,
                Enum.Parse<GapCategory>(i.Category),
                i.Recommendation)).ToList());
}
