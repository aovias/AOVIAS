using System.Text;
using AIOrchestratorService.Application.AI.Models;
using AIOrchestratorService.Application.AI.Predictive;
using AIOrchestratorService.Application.AI.Retrieval;
using AIOrchestratorService.Application.AI.Risk;

namespace AIOrchestratorService.Application.AI.Reporting;

public enum AiReportType
{
    ManagementReview = 1,
    AuditSummary = 2,
    ExecutiveBriefing = 3,
    CapaReport = 4,
    IncidentReport = 5,
    RiskReport = 6,
    TrainingReport = 7,
    ComplianceReport = 8
}

public sealed class AiReportGenerateRequest
{
    public Guid HospitalId { get; set; }
    public Guid? FrameworkId { get; set; }
    public string ReportType { get; set; } = nameof(AiReportType.ExecutiveBriefing);
    public string Horizon { get; set; } = "monthly";
}

public sealed class AiReportGenerateResult
{
    public string ReportType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Markdown { get; set; } = string.Empty;
    public List<AISourceReference> Citations { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
    public List<string> ActionItems { get; set; } = new();
    public GroundedAnswer Grounded { get; set; } = new();
    public string ExportFormat { get; set; } = "markdown";
    public string Notes { get; set; } = "PDF/Word export deferred — markdown string returned.";
}

public interface IAiReportingEngine
{
    Task<AiReportGenerateResult> GenerateAsync(AiReportGenerateRequest request, CancellationToken cancellationToken);
}

public sealed class AiReportingEngine : IAiReportingEngine
{
    private readonly IComplianceContextClient _contextClient;
    private readonly IRiskIntelligenceService _riskIntelligence;
    private readonly IPredictiveAnalyticsService _predictive;
    private readonly ILogger<AiReportingEngine> _logger;

    public AiReportingEngine(
        IComplianceContextClient contextClient,
        IRiskIntelligenceService riskIntelligence,
        IPredictiveAnalyticsService predictive,
        ILogger<AiReportingEngine> logger)
    {
        _contextClient = contextClient;
        _riskIntelligence = riskIntelligence;
        _predictive = predictive;
        _logger = logger;
    }

    public async Task<AiReportGenerateResult> GenerateAsync(AiReportGenerateRequest request, CancellationToken cancellationToken)
    {
        var reportType = ParseReportType(request.ReportType);
        var bundle = await _contextClient.GetContextBundleAsync(new ComplianceContextBundleRequest
        {
            HospitalId = request.HospitalId,
            FrameworkId = request.FrameworkId,
            PageContext = "AiReporting",
            EntityType = "Hospital",
            EntityId = request.HospitalId.ToString("N")
        }, cancellationToken);

        var risk = await _riskIntelligence.AnalyzeAsync(new RiskIntelligenceAnalyzeRequest
        {
            HospitalId = request.HospitalId,
            FrameworkId = request.FrameworkId,
            MaxClusters = 5
        }, cancellationToken);

        var predictive = await _predictive.GetOverviewAsync(new PredictiveRequest
        {
            HospitalId = request.HospitalId,
            FrameworkId = request.FrameworkId,
            Horizon = request.Horizon
        }, cancellationToken);

        var citations = risk.Grounded.Citations
            .Concat(predictive.Grounded.Citations)
            .Concat((bundle?.Sources ?? []).Select(s => new AISourceReference
            {
                SourceType = s.SourceType,
                SourceId = s.SourceId,
                Summary = s.Summary,
                RelevanceScore = s.RelevanceScore
            }))
            .GroupBy(c => $"{c.SourceType}:{c.SourceId}", StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .Take(15)
            .ToList();

        var recommendations = BuildRecommendations(reportType, risk, predictive);
        var actionItems = BuildActionItems(reportType, risk, predictive);
        var title = ResolveTitle(reportType);
        var markdown = BuildMarkdown(reportType, title, request, bundle, risk, predictive, recommendations, actionItems, citations);

        _logger.LogInformation("Generated AI report {ReportType} for hospital {HospitalId}", reportType, request.HospitalId);

        return new AiReportGenerateResult
        {
            ReportType = reportType.ToString(),
            Title = title,
            Markdown = markdown,
            Citations = citations,
            Recommendations = recommendations,
            ActionItems = actionItems,
            ExportFormat = "markdown",
            Notes = "PDF/Word export deferred — consumers should render or convert the markdown string.",
            Grounded = new GroundedAnswer
            {
                Answer = ExtractExecutiveSummary(markdown),
                Citations = citations,
                Reasoning =
                [
                    new AIReasoningStep { Code = "REPORT_COMPOSE", Message = $"Composed {reportType} from risk + predictive + context signals." },
                    new AIReasoningStep { Code = "ADVISORY_ONLY", Message = "Report is advisory; does not change compliance state." }
                ],
                ConfidenceScore = Decimal.Round((risk.Grounded.ConfidenceScore + predictive.Grounded.ConfidenceScore) / 2m, 2),
                ModelVersion = "ai-reporting-engine-v1",
                TraceId = Guid.NewGuid().ToString("N")
            }
        };
    }

    private static string BuildMarkdown(
        AiReportType reportType,
        string title,
        AiReportGenerateRequest request,
        ComplianceContextBundleDto? bundle,
        RiskIntelligenceResult risk,
        PredictiveOverviewDto predictive,
        IReadOnlyList<string> recommendations,
        IReadOnlyList<string> actionItems,
        IReadOnlyList<AISourceReference> citations)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# {title}");
        sb.AppendLine();
        sb.AppendLine($"- Hospital: `{request.HospitalId}`");
        if (request.FrameworkId.HasValue)
        {
            sb.AppendLine($"- Framework: `{request.FrameworkId}`");
        }

        sb.AppendLine($"- Horizon: **{predictive.Horizon}**");
        sb.AppendLine($"- Generated (UTC): {DateTime.UtcNow:yyyy-MM-dd HH:mm}");
        sb.AppendLine($"- Context version: `{bundle?.ContextVersionId ?? risk.ContextVersionId}`");
        sb.AppendLine();
        sb.AppendLine("## Executive summary");
        sb.AppendLine();
        sb.AppendLine(BuildExecutiveSummary(reportType, risk, predictive));
        sb.AppendLine();
        sb.AppendLine("## Key signals");
        sb.AppendLine();
        sb.AppendLine($"- Readiness now → forecast: **{predictive.Readiness.CurrentReadinessPercent}% → {predictive.Readiness.ForecastReadinessPercent}%** ({predictive.Readiness.Trend})");
        sb.AppendLine($"- CAPA delay probability: **{predictive.Delays.CapaDelayProbability}%**");
        sb.AppendLine($"- Document expiry risk: **{predictive.Delays.DocumentExpiryRisk}%**");
        sb.AppendLine($"- Training expiry risk: **{predictive.Delays.TrainingExpiryRisk}%**");
        sb.AppendLine($"- Risk clusters detected: **{risk.Clusters.Count}**");
        sb.AppendLine();

        AppendSectionForType(sb, reportType, risk, predictive);

        sb.AppendLine("## Recommendations");
        sb.AppendLine();
        foreach (var (item, idx) in recommendations.Select((r, i) => (r, i + 1)))
        {
            sb.AppendLine($"{idx}. {item}");
        }

        sb.AppendLine();
        sb.AppendLine("## Action items");
        sb.AppendLine();
        foreach (var (item, idx) in actionItems.Select((r, i) => (r, i + 1)))
        {
            sb.AppendLine($"{idx}. [ ] {item}");
        }

        sb.AppendLine();
        sb.AppendLine("## Citations");
        sb.AppendLine();
        if (citations.Count == 0)
        {
            sb.AppendLine("_No citations available._");
        }
        else
        {
            foreach (var c in citations)
            {
                sb.AppendLine($"- `{c.SourceType}` / `{c.SourceId}` — {c.Summary ?? c.Excerpt ?? "n/a"} (rel {c.RelevanceScore:0.00})");
            }
        }

        sb.AppendLine();
        sb.AppendLine("> Advisory AI report. Human review required before governance decisions. PDF/Word export not included in this release.");
        return sb.ToString();
    }

    private static void AppendSectionForType(
        StringBuilder sb,
        AiReportType reportType,
        RiskIntelligenceResult risk,
        PredictiveOverviewDto predictive)
    {
        switch (reportType)
        {
            case AiReportType.RiskReport:
                sb.AppendLine("## Risk clusters");
                sb.AppendLine();
                foreach (var cluster in risk.Clusters.Take(6))
                {
                    sb.AppendLine($"### {cluster.Theme} ({cluster.Severity})");
                    sb.AppendLine(cluster.Summary);
                    sb.AppendLine($"Confidence: {cluster.Confidence:0.00}");
                    sb.AppendLine();
                }
                break;

            case AiReportType.CapaReport:
                sb.AppendLine("## CAPA outlook");
                sb.AppendLine();
                sb.AppendLine(predictive.Delays.Narrative);
                sb.AppendLine();
                break;

            case AiReportType.TrainingReport:
                sb.AppendLine("## Training expiry outlook");
                sb.AppendLine();
                sb.AppendLine($"Training expiry risk is projected at **{predictive.Delays.TrainingExpiryRisk}%** over the {predictive.Horizon} horizon.");
                sb.AppendLine();
                break;

            case AiReportType.IncidentReport:
                sb.AppendLine("## Incident / safety themes");
                sb.AppendLine();
                foreach (var emerging in risk.EmergingRisks.Take(5))
                {
                    sb.AppendLine($"- **{emerging.Title}** — {emerging.Reason}");
                }
                sb.AppendLine();
                break;

            case AiReportType.AuditSummary:
                sb.AppendLine("## Audit readiness notes");
                sb.AppendLine();
                sb.AppendLine(predictive.Readiness.Narrative);
                sb.AppendLine();
                break;

            default:
                sb.AppendLine("## Department performance");
                sb.AppendLine();
                foreach (var dept in predictive.Departments)
                {
                    sb.AppendLine($"- **{dept.Department}**: {dept.PerformanceScore:0.#} ({dept.Outlook})");
                }
                sb.AppendLine();
                break;
        }
    }

    private static string BuildExecutiveSummary(
        AiReportType reportType,
        RiskIntelligenceResult risk,
        PredictiveOverviewDto predictive)
    {
        var topTheme = risk.Clusters.FirstOrDefault()?.Theme ?? "general compliance";
        return reportType switch
        {
            AiReportType.ExecutiveBriefing =>
                $"Compliance readiness is {predictive.Readiness.CurrentReadinessPercent}% with a {predictive.Readiness.Trend} {predictive.Horizon} outlook to {predictive.Readiness.ForecastReadinessPercent}%. Primary risk theme: {topTheme}.",
            AiReportType.ManagementReview =>
                $"Management review snapshot: readiness {predictive.Readiness.CurrentReadinessPercent}%, CAPA delay probability {predictive.Delays.CapaDelayProbability}%, {risk.Clusters.Count} recurring risk themes.",
            AiReportType.ComplianceReport =>
                $"Compliance posture summary for {predictive.Horizon}: forecast readiness {predictive.Readiness.ForecastReadinessPercent}% with {risk.EmergingRisks.Count} emerging risk signals.",
            _ =>
                $"{reportType} highlights: readiness {predictive.Readiness.CurrentReadinessPercent}% → {predictive.Readiness.ForecastReadinessPercent}%; focus theme {topTheme}."
        };
    }

    private static List<string> BuildRecommendations(
        AiReportType reportType,
        RiskIntelligenceResult risk,
        PredictiveOverviewDto predictive)
    {
        var list = new List<string>();
        if (predictive.Readiness.Trend == "declining")
        {
            list.Add("Prioritize overdue CAPAs and evidence gaps before the next governance cycle.");
        }

        list.AddRange(risk.RecommendedMitigations.Take(4));

        if (predictive.Delays.TrainingExpiryRisk >= 40)
        {
            list.Add("Accelerate training renewals for departments with elevated expiry risk.");
        }

        if (reportType == AiReportType.AuditSummary)
        {
            list.Add("Validate audit trail completeness for high-severity findings.");
        }

        if (list.Count == 0)
        {
            list.Add("Maintain current control cadence and re-run predictive overview next cycle.");
        }

        return list.Distinct(StringComparer.OrdinalIgnoreCase).Take(8).ToList();
    }

    private static List<string> BuildActionItems(
        AiReportType reportType,
        RiskIntelligenceResult risk,
        PredictiveOverviewDto predictive)
    {
        var items = new List<string>
        {
            "Acknowledge top ACE insights related to this report.",
            $"Review readiness forecast ({predictive.Horizon}) with Quality leadership."
        };

        foreach (var cluster in risk.Clusters.Take(3))
        {
            items.Add($"Assign owner for risk theme '{cluster.Theme}'.");
        }

        if (reportType is AiReportType.CapaReport or AiReportType.IncidentReport)
        {
            items.Add("Confirm CAPA effectiveness checks for recently closed items.");
        }

        return items.Take(8).ToList();
    }

    private static string ResolveTitle(AiReportType reportType) => reportType switch
    {
        AiReportType.ManagementReview => "Management Review Report",
        AiReportType.AuditSummary => "Audit Summary Report",
        AiReportType.ExecutiveBriefing => "Executive Compliance Briefing",
        AiReportType.CapaReport => "CAPA Report",
        AiReportType.IncidentReport => "Incident Report",
        AiReportType.RiskReport => "Risk Intelligence Report",
        AiReportType.TrainingReport => "Training Compliance Report",
        AiReportType.ComplianceReport => "Compliance Status Report",
        _ => "AI Compliance Report"
    };

    private static AiReportType ParseReportType(string? value)
    {
        if (Enum.TryParse<AiReportType>(value, ignoreCase: true, out var parsed))
        {
            return parsed;
        }

        return value?.Trim().ToLowerInvariant() switch
        {
            "management-review" or "managementreview" => AiReportType.ManagementReview,
            "audit-summary" or "auditsummary" => AiReportType.AuditSummary,
            "executive" or "executive-briefing" => AiReportType.ExecutiveBriefing,
            "capa" => AiReportType.CapaReport,
            "incident" => AiReportType.IncidentReport,
            "risk" => AiReportType.RiskReport,
            "training" => AiReportType.TrainingReport,
            "compliance" => AiReportType.ComplianceReport,
            _ => AiReportType.ExecutiveBriefing
        };
    }

    private static string ExtractExecutiveSummary(string markdown)
    {
        const string marker = "## Executive summary";
        var idx = markdown.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return markdown.Length > 280 ? markdown[..280] : markdown;
        var start = idx + marker.Length;
        var end = markdown.IndexOf("## ", start, StringComparison.Ordinal);
        var section = end > start ? markdown[start..end] : markdown[start..];
        return section.Trim();
    }
}
