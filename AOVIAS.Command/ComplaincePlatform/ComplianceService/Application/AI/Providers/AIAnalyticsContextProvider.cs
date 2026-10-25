using System.Globalization;
using ComplianceService.Application.AI.Contracts;
using ComplianceService.Application.Analytics;
using ComplianceService.Application.DTOs;

namespace ComplianceService.Application.AI.Providers;

public sealed class AIAnalyticsContextProvider : IAIAnalyticsContextProvider
{
    private readonly IAnalyticsQueryService _analytics;

    public AIAnalyticsContextProvider(IAnalyticsQueryService analytics)
    {
        _analytics = analytics;
    }

    public async Task<AIContextContribution> BuildAsync(AIContextBuildContext context, CancellationToken cancellationToken = default)
    {
        var scope = context.Scope;
        var facts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var sources = new List<AIContextSourceDto>();

        var overview = await _analytics.GetOverviewAsync(scope, cancellationToken);
        var executive = await _analytics.GetExecutiveAsync(scope, cancellationToken);
        var operational = await _analytics.GetOperationalAsync(scope, cancellationToken);

        facts["analyticsReadinessScore"] = overview.ReadinessScore.ToString(CultureInfo.InvariantCulture);
        facts["analyticsOpenDeficiencies"] = overview.OpenDeficiencies.ToString(CultureInfo.InvariantCulture);
        facts["analyticsCriticalDeficiencies"] = overview.CriticalDeficiencies.ToString(CultureInfo.InvariantCulture);
        facts["analyticsCapaEffectivenessPercent"] = overview.CapaEffectivenessRate.ToString(CultureInfo.InvariantCulture);
        facts["analyticsSlaHealthScore"] = overview.SlaHealthScore.ToString(CultureInfo.InvariantCulture);
        facts["analyticsRecurringIssues"] = overview.RecurringIssues.ToString(CultureInfo.InvariantCulture);
        facts["analyticsPendingReviews"] = overview.PendingReviews.ToString(CultureInfo.InvariantCulture);
        facts["analyticsCurrentStage"] = overview.CurrentStage ?? string.Empty;

        var risk = executive.RiskExposure;
        facts["analyticsCriticalRiskCount"] = risk.CriticalRiskCount.ToString(CultureInfo.InvariantCulture);
        facts["analyticsHighRiskDeficiencies"] = risk.HighRiskDeficiencies.ToString(CultureInfo.InvariantCulture);

        if (executive.DepartmentPerformance.Count > 0)
        {
            facts["analyticsDepartmentSummary"] = string.Join("; ",
                executive.DepartmentPerformance.Take(5).Select(d => $"{d.Label}: {d.Count} items"));
        }

        if (operational.RootCauseTrend.Count > 0)
        {
            facts["analyticsTopIssues"] = string.Join("; ",
                operational.RootCauseTrend.Take(5).Select(i => $"{i.Label}: {i.Count}"));
        }

        var trends = await _analytics.GetTrendsAsync(scope, new AnalyticsTrendQueryDto(), cancellationToken);
        if (trends.Series.Count > 0)
        {
            facts["analyticsTopTrends"] = string.Join("; ",
                trends.Series.Take(3).Select(s => $"{s.Label} ({s.Points.Count} data points)"));
        }

        foreach (var kpi in overview.KpiCards.Take(4))
        {
            var valueText = kpi.Value.HasValue
                ? $"{kpi.Value.Value}{(string.IsNullOrWhiteSpace(kpi.Unit) ? "" : kpi.Unit)}"
                : "—";
            sources.Add(new AIContextSourceDto
            {
                SourceType = "AnalyticsKpi",
                SourceId = kpi.Key,
                Summary = $"{kpi.Label}: {valueText}",
                RelevanceScore = 0.85m
            });
        }

        return new AIContextContribution { Facts = facts, Sources = sources };
    }
}
