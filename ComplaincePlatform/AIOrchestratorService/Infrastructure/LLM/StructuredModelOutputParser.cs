using System.Text.Json;
using AIOrchestratorService.Application.AI.Inference;
using AIOrchestratorService.Application.AI.Models;

namespace AIOrchestratorService.Infrastructure.LLM;

/// <summary>
/// Reads the optional Executive Compliance Advisor structured fields from a model's JSON object
/// and populates an <see cref="AIModelResponse"/>. Every field is optional and tolerant of absence
/// so the legacy <c>{hint, confidence, reasoningBullets}</c> schema keeps working unchanged.
/// </summary>
internal static class StructuredModelOutputParser
{
    public static void Populate(JsonElement root, AIModelResponse target)
    {
        if (root.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        var executiveSummary = ReadString(root, "executiveSummary", "summary");
        if (!string.IsNullOrWhiteSpace(executiveSummary))
        {
            target.ExecutiveSummary = executiveSummary;
        }

        var expectedOutcome = ReadString(root, "expectedOutcome", "outcome");
        if (!string.IsNullOrWhiteSpace(expectedOutcome))
        {
            target.ExpectedOutcome = expectedOutcome;
        }

        var insightType = ReadString(root, "insightType", "responseType");
        if (TryParseInsightType(insightType, out var parsedType))
        {
            target.InsightType = parsedType;
        }

        target.Findings = ReadFindings(root);
        target.Risks = ReadRisks(root);
        target.Recommendations = ReadRecommendations(root);
    }

    private static List<ComplianceFinding> ReadFindings(JsonElement root)
    {
        var results = new List<ComplianceFinding>();
        if (!TryGetArray(root, out var array, "findings", "keyFindings"))
        {
            return results;
        }

        foreach (var item in array.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var title = ReadString(item, "title", "name", "gap");
            if (string.IsNullOrWhiteSpace(title))
            {
                continue;
            }

            results.Add(new ComplianceFinding
            {
                Title = title,
                Severity = ParseSeverity(ReadString(item, "severity", "impact", "impactLevel")),
                Impact = ReadString(item, "impact", "impactDescription", "businessImpact"),
                Detail = ReadString(item, "detail", "description", "recommendation")
            });
        }

        return results;
    }

    private static List<ComplianceRisk> ReadRisks(JsonElement root)
    {
        var results = new List<ComplianceRisk>();
        if (!TryGetArray(root, out var array, "risks", "criticalRisks"))
        {
            return results;
        }

        foreach (var item in array.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var title = ReadString(item, "title", "name", "risk");
            if (string.IsNullOrWhiteSpace(title))
            {
                continue;
            }

            results.Add(new ComplianceRisk
            {
                Title = title,
                Severity = ParseSeverity(ReadString(item, "severity", "level", "riskLevel")),
                AffectedControls = ReadStringList(item, "affectedControls", "controls", "affected")
            });
        }

        return results;
    }

    private static List<RecommendedAction> ReadRecommendations(JsonElement root)
    {
        var results = new List<RecommendedAction>();
        if (!TryGetArray(root, out var array, "recommendations", "recommendedActions", "actions"))
        {
            return results;
        }

        var fallbackPriority = 1;
        foreach (var item in array.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var action = ReadString(item, "action", "title", "recommendation");
            if (string.IsNullOrWhiteSpace(action))
            {
                continue;
            }

            results.Add(new RecommendedAction
            {
                Priority = ReadInt(item, "priority") ?? fallbackPriority,
                Action = action,
                ExpectedOutcome = ReadString(item, "expectedOutcome", "outcome", "impact")
            });
            fallbackPriority++;
        }

        return results;
    }

    private static bool TryParseInsightType(string? value, out ComplianceInsightType insightType)
    {
        insightType = ComplianceInsightType.General;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var normalized = value.Replace(" ", string.Empty).Replace("_", string.Empty).Replace("-", string.Empty);
        return Enum.TryParse(normalized, ignoreCase: true, out insightType);
    }

    private static ComplianceSeverity ParseSeverity(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return ComplianceSeverity.Medium;
        }

        var normalized = value.Trim().ToLowerInvariant();
        return normalized switch
        {
            "critical" or "severe" or "blocker" => ComplianceSeverity.Critical,
            "high" or "major" => ComplianceSeverity.High,
            "medium" or "moderate" or "med" => ComplianceSeverity.Medium,
            "low" or "minor" => ComplianceSeverity.Low,
            _ => ComplianceSeverity.Medium
        };
    }

    private static bool TryGetArray(JsonElement root, out JsonElement array, params string[] names)
    {
        foreach (var name in names)
        {
            if (root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Array)
            {
                array = value;
                return true;
            }
        }

        array = default;
        return false;
    }

    private static string ReadString(JsonElement root, params string[] names)
    {
        foreach (var name in names)
        {
            if (root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String)
            {
                var content = value.GetString();
                if (!string.IsNullOrWhiteSpace(content))
                {
                    return content.Trim();
                }
            }
        }

        return string.Empty;
    }

    private static int? ReadInt(JsonElement root, params string[] names)
    {
        foreach (var name in names)
        {
            if (!root.TryGetProperty(name, out var value))
            {
                continue;
            }

            if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var direct))
            {
                return direct;
            }

            if (value.ValueKind == JsonValueKind.String && int.TryParse(value.GetString(), out var fromString))
            {
                return fromString;
            }
        }

        return null;
    }

    private static List<string> ReadStringList(JsonElement root, params string[] names)
    {
        foreach (var name in names)
        {
            if (!root.TryGetProperty(name, out var value))
            {
                continue;
            }

            if (value.ValueKind == JsonValueKind.Array)
            {
                var items = new List<string>();
                foreach (var item in value.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.String)
                    {
                        var text = item.GetString();
                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            items.Add(text.Trim());
                        }
                    }
                }

                return items;
            }

            if (value.ValueKind == JsonValueKind.String)
            {
                var single = value.GetString();
                if (!string.IsNullOrWhiteSpace(single))
                {
                    return new List<string> { single.Trim() };
                }
            }
        }

        return new List<string>();
    }
}
