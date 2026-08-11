using System.Globalization;
using System.Text.RegularExpressions;
using AIOrchestratorService.Application.AI.Models;
using AIOrchestratorService.Application.AI.Retrieval;

namespace AIOrchestratorService.Application.AI.Predictive;

public enum ForecastHorizon
{
    Weekly = 1,
    Monthly = 2,
    Quarterly = 3
}

public sealed class PredictiveRequest
{
    public Guid HospitalId { get; set; }
    public Guid? FrameworkId { get; set; }
    public string Horizon { get; set; } = "monthly";
}

public sealed class ReadinessForecastDto
{
    public decimal CurrentReadinessPercent { get; set; }
    public decimal ForecastReadinessPercent { get; set; }
    public string Horizon { get; set; } = "monthly";
    public string Trend { get; set; } = "stable";
    public string Narrative { get; set; } = string.Empty;
    public List<AISourceReference> Citations { get; set; } = new();
    public decimal Confidence { get; set; }
}

public sealed class DelayForecastDto
{
    public decimal CapaDelayProbability { get; set; }
    public decimal DocumentExpiryRisk { get; set; }
    public decimal TrainingExpiryRisk { get; set; }
    public string Horizon { get; set; } = "monthly";
    public string Narrative { get; set; } = string.Empty;
    public List<AISourceReference> Citations { get; set; } = new();
    public decimal Confidence { get; set; }
}

public sealed class DepartmentPerformanceForecastDto
{
    public string Department { get; set; } = string.Empty;
    public decimal PerformanceScore { get; set; }
    public string Outlook { get; set; } = "stable";
}

public sealed class PredictiveOverviewDto
{
    public Guid HospitalId { get; set; }
    public Guid? FrameworkId { get; set; }
    public string Horizon { get; set; } = "monthly";
    public string ContextVersionId { get; set; } = string.Empty;
    public ReadinessForecastDto Readiness { get; set; } = new();
    public DelayForecastDto Delays { get; set; } = new();
    public List<DepartmentPerformanceForecastDto> Departments { get; set; } = new();
    public GroundedAnswer Grounded { get; set; } = new();
    public bool UsedFallbackContext { get; set; }
}

public interface IPredictiveAnalyticsService
{
    Task<ReadinessForecastDto> ForecastReadinessAsync(PredictiveRequest request, CancellationToken cancellationToken);
    Task<DelayForecastDto> ForecastDelaysAsync(PredictiveRequest request, CancellationToken cancellationToken);
    Task<PredictiveOverviewDto> GetOverviewAsync(PredictiveRequest request, CancellationToken cancellationToken);
}

/// <summary>
/// Deterministic-first predictive analytics. LLM narrative is optional; template narrative is default.
/// </summary>
public sealed class PredictiveAnalyticsService : IPredictiveAnalyticsService
{
    private readonly IComplianceContextClient _contextClient;
    private readonly ILogger<PredictiveAnalyticsService> _logger;

    public PredictiveAnalyticsService(IComplianceContextClient contextClient, ILogger<PredictiveAnalyticsService> logger)
    {
        _contextClient = contextClient;
        _logger = logger;
    }

    public async Task<ReadinessForecastDto> ForecastReadinessAsync(PredictiveRequest request, CancellationToken cancellationToken)
    {
        var overview = await GetOverviewAsync(request, cancellationToken);
        return overview.Readiness;
    }

    public async Task<DelayForecastDto> ForecastDelaysAsync(PredictiveRequest request, CancellationToken cancellationToken)
    {
        var overview = await GetOverviewAsync(request, cancellationToken);
        return overview.Delays;
    }

    public async Task<PredictiveOverviewDto> GetOverviewAsync(PredictiveRequest request, CancellationToken cancellationToken)
    {
        var horizon = ParseHorizon(request.Horizon);
        var horizonLabel = horizon.ToString().ToLowerInvariant();
        var horizonFactor = HorizonFactor(horizon);

        var bundle = await _contextClient.GetContextBundleAsync(new ComplianceContextBundleRequest
        {
            HospitalId = request.HospitalId,
            FrameworkId = request.FrameworkId,
            PageContext = "PredictiveAnalytics",
            EntityType = "Hospital",
            EntityId = request.HospitalId.ToString("N")
        }, cancellationToken);

        var usedFallback = bundle == null;
        if (usedFallback)
        {
            _logger.LogWarning("Predictive analytics using fallback facts for hospital {HospitalId}", request.HospitalId);
            bundle = BuildFallbackBundle(request.HospitalId);
        }

        var facts = bundle!.Facts;
        var currentReadiness = ClampPercent(
            FirstDecimal(facts, "readinessPercent", "complianceScore", "maturityScore") ??
            DeriveReadinessFromCounts(facts));

        var overdueCapas = FirstInt(facts, "overdueCapas", "openCapas", "criticalCapas");
        var openFindings = FirstInt(facts, "openFindings", "findingsOpen");
        var docExpiring = FirstInt(facts, "documentsExpiring", "docExpiryCount", "expiredDocuments");
        var trainingExpiring = FirstInt(facts, "trainingExpiring", "trainingOverdue", "expiredTrainings");
        var overdueTasks = FirstInt(facts, "overdueTasks", "openTasks");

        var drift = Math.Min(18m, (overdueCapas * 2.2m + openFindings * 1.4m + overdueTasks * 0.8m) * horizonFactor);
        var forecastReadiness = ClampPercent(currentReadiness - drift + (horizon == ForecastHorizon.Weekly ? 1.5m : 0m));
        var trend = forecastReadiness > currentReadiness + 1 ? "improving"
            : forecastReadiness < currentReadiness - 1 ? "declining"
            : "stable";

        var capaDelayProb = ClampPercent(25m + overdueCapas * 8m * horizonFactor + openFindings * 2m);
        var docExpiryRisk = ClampPercent(15m + docExpiring * 10m * horizonFactor);
        var trainingExpiryRisk = ClampPercent(18m + trainingExpiring * 9m * horizonFactor);

        var citations = bundle.Sources.Take(6).Select(s => new AISourceReference
        {
            SourceType = s.SourceType,
            SourceId = s.SourceId,
            Summary = s.Summary,
            RelevanceScore = s.RelevanceScore,
            Excerpt = s.Summary
        }).ToList();

        if (citations.Count == 0)
        {
            citations.Add(new AISourceReference
            {
                SourceType = "ComplianceFact",
                SourceId = "predictive-inputs",
                Summary = "Derived from compliance context facts.",
                RelevanceScore = 0.6m
            });
        }

        var readiness = new ReadinessForecastDto
        {
            CurrentReadinessPercent = Decimal.Round(currentReadiness, 1),
            ForecastReadinessPercent = Decimal.Round(forecastReadiness, 1),
            Horizon = horizonLabel,
            Trend = trend,
            Narrative = $"Over the {horizonLabel} horizon, readiness is projected to move from {currentReadiness:0.#}% to {forecastReadiness:0.#}% ({trend}).",
            Citations = citations,
            Confidence = usedFallback ? 0.5m : 0.78m
        };

        var delays = new DelayForecastDto
        {
            CapaDelayProbability = Decimal.Round(capaDelayProb, 1),
            DocumentExpiryRisk = Decimal.Round(docExpiryRisk, 1),
            TrainingExpiryRisk = Decimal.Round(trainingExpiryRisk, 1),
            Horizon = horizonLabel,
            Narrative =
                $"CAPA delay probability ~{capaDelayProb:0.#}%; document expiry risk ~{docExpiryRisk:0.#}%; training expiry risk ~{trainingExpiryRisk:0.#}% ({horizonLabel}).",
            Citations = citations,
            Confidence = usedFallback ? 0.48m : 0.76m
        };

        var departments = BuildDepartmentForecasts(facts, forecastReadiness, capaDelayProb);

        return new PredictiveOverviewDto
        {
            HospitalId = request.HospitalId,
            FrameworkId = request.FrameworkId,
            Horizon = horizonLabel,
            ContextVersionId = bundle.ContextVersionId,
            Readiness = readiness,
            Delays = delays,
            Departments = departments,
            UsedFallbackContext = usedFallback,
            Grounded = new GroundedAnswer
            {
                Answer = $"{readiness.Narrative} {delays.Narrative}",
                Citations = citations,
                Reasoning =
                [
                    new AIReasoningStep { Code = "DETERMINISTIC_MODEL", Message = "Forecasts computed from context counts and horizon factors." },
                    new AIReasoningStep { Code = "TEMPLATE_NARRATIVE", Message = "Narrative generated from templates (no LLM required)." }
                ],
                ConfidenceScore = Decimal.Round((readiness.Confidence + delays.Confidence) / 2m, 2),
                ModelVersion = "predictive-deterministic-v1",
                TraceId = Guid.NewGuid().ToString("N")
            }
        };
    }

    private static List<DepartmentPerformanceForecastDto> BuildDepartmentForecasts(
        Dictionary<string, string> facts,
        decimal readiness,
        decimal capaDelayProb)
    {
        var named = facts
            .Where(kv => kv.Key.StartsWith("deptScore:", StringComparison.OrdinalIgnoreCase))
            .Select(kv =>
            {
                var dept = kv.Key.Split(':', 2).LastOrDefault() ?? "Department";
                var score = ParseDecimal(kv.Value) ?? readiness;
                return new DepartmentPerformanceForecastDto
                {
                    Department = dept,
                    PerformanceScore = ClampPercent(score),
                    Outlook = score >= readiness ? "on-track" : "at-risk"
                };
            })
            .OrderBy(d => d.PerformanceScore)
            .Take(8)
            .ToList();

        if (named.Count > 0) return named;

        return
        [
            new DepartmentPerformanceForecastDto
            {
                Department = "Nursing",
                PerformanceScore = ClampPercent(readiness - 3),
                Outlook = capaDelayProb > 50 ? "at-risk" : "stable"
            },
            new DepartmentPerformanceForecastDto
            {
                Department = "Quality",
                PerformanceScore = ClampPercent(readiness + 2),
                Outlook = "on-track"
            },
            new DepartmentPerformanceForecastDto
            {
                Department = "Laboratory",
                PerformanceScore = ClampPercent(readiness - (capaDelayProb / 10m)),
                Outlook = capaDelayProb > 60 ? "at-risk" : "stable"
            }
        ];
    }

    private static ComplianceContextBundleDto BuildFallbackBundle(Guid hospitalId)
    {
        return new ComplianceContextBundleDto
        {
            ContextVersionId = $"{hospitalId:N}:fallback-predict",
            Facts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["readinessPercent"] = "72",
                ["overdueCapas"] = "3",
                ["openFindings"] = "5",
                ["documentsExpiring"] = "2",
                ["trainingOverdue"] = "4",
                ["overdueTasks"] = "7"
            },
            Sources =
            [
                new ComplianceContextSourceDto
                {
                    SourceType = "Heuristic",
                    SourceId = "fallback-predict",
                    Summary = "Fallback predictive inputs.",
                    RelevanceScore = 0.4m
                }
            ]
        };
    }

    private static ForecastHorizon ParseHorizon(string? horizon)
    {
        return (horizon ?? "monthly").Trim().ToLowerInvariant() switch
        {
            "week" or "weekly" => ForecastHorizon.Weekly,
            "quarter" or "quarterly" => ForecastHorizon.Quarterly,
            _ => ForecastHorizon.Monthly
        };
    }

    private static decimal HorizonFactor(ForecastHorizon horizon) => horizon switch
    {
        ForecastHorizon.Weekly => 0.45m,
        ForecastHorizon.Quarterly => 1.55m,
        _ => 1.0m
    };

    private static decimal DeriveReadinessFromCounts(Dictionary<string, string> facts)
    {
        var overdue = FirstInt(facts, "overdueCapas", "overdueTasks", "openFindings");
        return ClampPercent(85m - overdue * 3.5m);
    }

    private static decimal? FirstDecimal(Dictionary<string, string> facts, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (facts.TryGetValue(key, out var value))
            {
                var parsed = ParseDecimal(value);
                if (parsed.HasValue) return parsed;
            }
        }

        return null;
    }

    private static int FirstInt(Dictionary<string, string> facts, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (facts.TryGetValue(key, out var value))
            {
                var match = Regex.Match(value, @"-?\d+");
                if (match.Success && int.TryParse(match.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n))
                {
                    return Math.Max(0, n);
                }
            }
        }

        return 0;
    }

    private static decimal? ParseDecimal(string value)
    {
        if (decimal.TryParse(value.Replace("%", string.Empty).Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var d))
        {
            return d;
        }

        return null;
    }

    private static decimal ClampPercent(decimal value) => Math.Clamp(value, 0m, 100m);
}
