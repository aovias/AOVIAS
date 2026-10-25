using System.Globalization;
using System.Text.RegularExpressions;
using AIOrchestratorService.Application.AI.Models;
using AIOrchestratorService.Application.AI.Retrieval;

namespace AIOrchestratorService.Application.AI.Risk;

public sealed class RiskIntelligenceAnalyzeRequest
{
    public Guid HospitalId { get; set; }
    public Guid? FrameworkId { get; set; }
    public string? Department { get; set; }
    public int MaxClusters { get; set; } = 8;
}

public sealed class RiskClusterDto
{
    public string Theme { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Severity { get; set; } = "Medium";
    public int SignalCount { get; set; }
    public List<string> Mitigations { get; set; } = new();
    public List<AISourceReference> Citations { get; set; } = new();
    public decimal Confidence { get; set; }
}

public sealed class EmergingRiskDto
{
    public string Title { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string Severity { get; set; } = "Medium";
    public decimal Confidence { get; set; }
    public List<AISourceReference> Citations { get; set; } = new();
}

public sealed class RiskIntelligenceResult
{
    public Guid HospitalId { get; set; }
    public Guid? FrameworkId { get; set; }
    public string ContextVersionId { get; set; } = string.Empty;
    public List<RiskClusterDto> Clusters { get; set; } = new();
    public List<EmergingRiskDto> EmergingRisks { get; set; } = new();
    public List<string> RecommendedMitigations { get; set; } = new();
    public GroundedAnswer Grounded { get; set; } = new();
    public bool UsedFallbackContext { get; set; }
}

public interface IRiskIntelligenceService
{
    Task<RiskIntelligenceResult> AnalyzeAsync(RiskIntelligenceAnalyzeRequest request, CancellationToken cancellationToken);
}

/// <summary>
/// Deterministic theme clustering over Compliance context signals (incidents/CAPAs/findings).
/// Advisory only — does not mutate regulatory state.
/// </summary>
public sealed class RiskIntelligenceService : IRiskIntelligenceService
{
    private static readonly string[] ThemeLexicon =
    [
        "infection", "medication", "documentation", "training", "equipment", "staffing",
        "consent", "privacy", "sterilization", "handover", "lab", "emergency", "capa",
        "incident", "finding", "audit", "expiry", "overdue", "evidence", "patient safety"
    ];

    private readonly IComplianceContextClient _contextClient;
    private readonly ILogger<RiskIntelligenceService> _logger;

    public RiskIntelligenceService(IComplianceContextClient contextClient, ILogger<RiskIntelligenceService> logger)
    {
        _contextClient = contextClient;
        _logger = logger;
    }

    public async Task<RiskIntelligenceResult> AnalyzeAsync(RiskIntelligenceAnalyzeRequest request, CancellationToken cancellationToken)
    {
        var maxClusters = Math.Clamp(request.MaxClusters <= 0 ? 8 : request.MaxClusters, 1, 20);
        var bundle = await _contextClient.GetContextBundleAsync(new ComplianceContextBundleRequest
        {
            HospitalId = request.HospitalId,
            FrameworkId = request.FrameworkId,
            PageContext = "RiskIntelligence",
            EntityType = "Hospital",
            EntityId = request.HospitalId.ToString("N")
        }, cancellationToken);

        var usedFallback = bundle == null;
        if (usedFallback)
        {
            _logger.LogWarning("Risk intelligence falling back to heuristic signals for hospital {HospitalId}", request.HospitalId);
            bundle = BuildFallbackBundle(request.HospitalId);
        }

        var signals = ExtractSignals(bundle!);
        var clusters = ClusterThemes(signals, maxClusters);
        var emerging = DetectEmerging(signals, clusters);
        var mitigations = clusters
            .SelectMany(c => c.Mitigations)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(10)
            .ToList();

        var citations = clusters.SelectMany(c => c.Citations)
            .Concat(emerging.SelectMany(e => e.Citations))
            .GroupBy(c => $"{c.SourceType}:{c.SourceId}", StringComparer.OrdinalIgnoreCase)
            .Select(g => g.OrderByDescending(x => x.RelevanceScore).First())
            .Take(12)
            .ToList();

        var answer = clusters.Count == 0
            ? "No recurring risk themes detected from current compliance context."
            : $"Identified {clusters.Count} recurring risk theme(s) and {emerging.Count} emerging risk signal(s). Top theme: {clusters[0].Theme}.";

        return new RiskIntelligenceResult
        {
            HospitalId = request.HospitalId,
            FrameworkId = request.FrameworkId,
            ContextVersionId = bundle!.ContextVersionId,
            Clusters = clusters,
            EmergingRisks = emerging,
            RecommendedMitigations = mitigations,
            UsedFallbackContext = usedFallback,
            Grounded = new GroundedAnswer
            {
                Answer = answer,
                Citations = citations,
                Reasoning =
                [
                    new AIReasoningStep { Code = "THEME_CLUSTER", Message = "Clustered recurring phrases from incidents, CAPAs, and findings." },
                    new AIReasoningStep { Code = "ADVISORY_ONLY", Message = "Output is advisory; no regulatory state was changed." }
                ],
                ConfidenceScore = clusters.Count == 0 ? 0.45m : clusters.Average(c => c.Confidence),
                ModelVersion = "risk-intelligence-heuristic-v1",
                TraceId = Guid.NewGuid().ToString("N"),
                HumanApprovalRequired = false
            }
        };
    }

    private static ComplianceContextBundleDto BuildFallbackBundle(Guid hospitalId)
    {
        return new ComplianceContextBundleDto
        {
            ContextVersionId = $"{hospitalId:N}:fallback-risk",
            Facts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["openIncidents"] = "3",
                ["openCapas"] = "5",
                ["overdueCapas"] = "2",
                ["openFindings"] = "4",
                ["themeHints"] = "documentation overdue; infection control finding; training expiry; medication incident"
            },
            Sources =
            [
                new ComplianceContextSourceDto
                {
                    SourceType = "Heuristic",
                    SourceId = "fallback-bundle",
                    Summary = "Fallback risk signals when Compliance context bundle is unavailable.",
                    RelevanceScore = 0.4m
                }
            ]
        };
    }

    private static List<RiskSignal> ExtractSignals(ComplianceContextBundleDto bundle)
    {
        var signals = new List<RiskSignal>();

        foreach (var source in bundle.Sources)
        {
            var text = $"{source.SourceType} {source.SourceId} {source.Summary}".Trim();
            if (string.IsNullOrWhiteSpace(text)) continue;
            signals.Add(new RiskSignal(text, source.SourceType, source.SourceId, source.Summary, source.RelevanceScore));
        }

        foreach (var (key, value) in bundle.Facts)
        {
            var text = $"{key} {value}";
            signals.Add(new RiskSignal(
                text,
                "ComplianceFact",
                key,
                value,
                ParseNumericHint(value) >= 3 ? 0.75m : 0.55m));
        }

        return signals;
    }

    private static List<RiskClusterDto> ClusterThemes(IReadOnlyList<RiskSignal> signals, int maxClusters)
    {
        var buckets = new Dictionary<string, List<RiskSignal>>(StringComparer.OrdinalIgnoreCase);

        foreach (var signal in signals)
        {
            var theme = MatchTheme(signal.Text) ?? InferThemeToken(signal.Text);
            if (!buckets.TryGetValue(theme, out var list))
            {
                list = new List<RiskSignal>();
                buckets[theme] = list;
            }

            list.Add(signal);
        }

        return buckets
            .OrderByDescending(kv => kv.Value.Count)
            .ThenByDescending(kv => kv.Value.Sum(s => (double)s.Relevance))
            .Take(maxClusters)
            .Select(kv =>
            {
                var citations = kv.Value
                    .OrderByDescending(s => s.Relevance)
                    .Take(4)
                    .Select(s => new AISourceReference
                    {
                        SourceType = s.SourceType,
                        SourceId = s.SourceId,
                        Summary = s.Summary,
                        RelevanceScore = s.Relevance,
                        Excerpt = Truncate(s.Text, 160)
                    })
                    .ToList();

                var severity = ResolveSeverity(kv.Value.Count, kv.Key);
                var confidence = Math.Clamp(0.45m + (kv.Value.Count * 0.08m), 0.45m, 0.92m);

                return new RiskClusterDto
                {
                    Theme = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(kv.Key),
                    Summary = $"Recurring signals around '{kv.Key}' across {kv.Value.Count} compliance context item(s).",
                    Severity = severity,
                    SignalCount = kv.Value.Count,
                    Mitigations = BuildMitigations(kv.Key, severity),
                    Citations = citations,
                    Confidence = confidence
                };
            })
            .ToList();
    }

    private static List<EmergingRiskDto> DetectEmerging(IReadOnlyList<RiskSignal> signals, IReadOnlyList<RiskClusterDto> clusters)
    {
        var emerging = new List<EmergingRiskDto>();

        foreach (var cluster in clusters.Where(c => c.SignalCount >= 2).Take(5))
        {
            emerging.Add(new EmergingRiskDto
            {
                Title = $"Emerging cluster: {cluster.Theme}",
                Reason = $"Theme appears in {cluster.SignalCount} recent signals with {cluster.Severity.ToLowerInvariant()} severity.",
                Severity = cluster.Severity,
                Confidence = cluster.Confidence,
                Citations = cluster.Citations.Take(2).ToList()
            });
        }

        var overdueSignals = signals.Where(s =>
            s.Text.Contains("overdue", StringComparison.OrdinalIgnoreCase) ||
            s.Text.Contains("expiry", StringComparison.OrdinalIgnoreCase) ||
            s.Text.Contains("expired", StringComparison.OrdinalIgnoreCase)).ToList();

        if (overdueSignals.Count > 0)
        {
            emerging.Add(new EmergingRiskDto
            {
                Title = "Time-bound compliance slippage",
                Reason = "Overdue/expiry language detected in context facts or sources.",
                Severity = overdueSignals.Count >= 3 ? "High" : "Medium",
                Confidence = 0.7m,
                Citations = overdueSignals.Take(3).Select(s => new AISourceReference
                {
                    SourceType = s.SourceType,
                    SourceId = s.SourceId,
                    Summary = s.Summary,
                    RelevanceScore = s.Relevance,
                    Excerpt = Truncate(s.Text, 140)
                }).ToList()
            });
        }

        return emerging
            .GroupBy(e => e.Title, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .Take(8)
            .ToList();
    }

    private static string? MatchTheme(string text)
    {
        foreach (var theme in ThemeLexicon)
        {
            if (text.Contains(theme, StringComparison.OrdinalIgnoreCase))
            {
                return theme;
            }
        }

        return null;
    }

    private static string InferThemeToken(string text)
    {
        var tokens = Regex.Matches(text.ToLowerInvariant(), "[a-z]{4,}")
            .Select(m => m.Value)
            .Where(t => t is not ("with" or "from" or "that" or "this" or "have" or "been" or "hospital" or "framework"))
            .GroupBy(t => t)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .FirstOrDefault();

        return tokens ?? "general";
    }

    private static string ResolveSeverity(int signalCount, string theme)
    {
        if (theme.Contains("infection", StringComparison.OrdinalIgnoreCase) ||
            theme.Contains("patient safety", StringComparison.OrdinalIgnoreCase) ||
            signalCount >= 5)
        {
            return "High";
        }

        if (signalCount >= 3) return "Medium";
        return "Low";
    }

    private static List<string> BuildMitigations(string theme, string severity)
    {
        var list = new List<string>
        {
            $"Review open items tagged to '{theme}' and assign owners.",
            "Confirm evidence freshness for related objective elements."
        };

        if (severity is "High" or "Critical")
        {
            list.Add("Escalate to quality governance for prioritized CAPA planning.");
        }

        if (theme.Contains("training", StringComparison.OrdinalIgnoreCase))
        {
            list.Add("Schedule refresher training for affected departments.");
        }

        if (theme.Contains("documentation", StringComparison.OrdinalIgnoreCase))
        {
            list.Add("Close documentation gaps before next audit window.");
        }

        return list;
    }

    private static int ParseNumericHint(string value)
    {
        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n))
        {
            return n;
        }

        var match = Regex.Match(value, @"\d+");
        return match.Success && int.TryParse(match.Value, out n) ? n : 0;
    }

    private static string Truncate(string value, int max)
        => value.Length <= max ? value : value[..max] + "…";

    private sealed record RiskSignal(string Text, string SourceType, string SourceId, string? Summary, decimal Relevance);
}
