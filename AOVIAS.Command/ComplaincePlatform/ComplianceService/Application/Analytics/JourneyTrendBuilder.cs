using System.Text.Json;
using ComplianceService.Application.Aggregation.Models;
using ComplianceService.Application.DTOs;
using ComplianceService.Domain;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ComplianceService.Application.Analytics;

/// <summary>
/// Builds distinct journey trend series from <see cref="ComplianceSnapshot"/> history (Sprint 12B-0).
/// </summary>
public static class JourneyTrendBuilder
{
    public sealed class JourneyTrendSeries
    {
        public List<TrendPointDto> OverallScoreTrend { get; init; } = new();
        public List<TrendPointDto> ReadinessTrend { get; init; } = new();
        public List<TrendPointDto> ChapterImprovementTrend { get; init; } = new();
        public List<TrendPointDto> RiskEvolutionTrend { get; init; } = new();
    }

    public static async Task<JourneyTrendSeries> BuildAsync(
        ComplianceDbContext db,
        Guid hospitalId,
        Guid frameworkId,
        int take = 12,
        CancellationToken ct = default)
    {
        var snaps = await db.ComplianceSnapshots.AsNoTracking()
            .Where(s => s.HospitalId == hospitalId && s.FrameworkId == frameworkId)
            .OrderByDescending(s => s.CalculatedAt)
            .Take(take)
            .ToListAsync(ct);

        snaps.Reverse();

        var overall = new List<TrendPointDto>();
        var readiness = new List<TrendPointDto>();
        var chapter = new List<TrendPointDto>();
        var risk = new List<TrendPointDto>();

        foreach (var s in snaps)
        {
            var label = s.CalculatedAt.ToString("yyyy-MM-dd");
            overall.Add(new TrendPointDto { Label = label, Value = s.OverallScore });
            readiness.Add(new TrendPointDto { Label = label, Value = s.AuditConfidenceScore });

            var chapterAvg = TryAverageChapterCompliance(s.ChapterBreakdownJson);
            if (chapterAvg.HasValue)
            {
                chapter.Add(new TrendPointDto { Label = label, Value = chapterAvg.Value });
            }

            if (s.CriticalRiskCount > 0)
            {
                risk.Add(new TrendPointDto { Label = label, Value = s.CriticalRiskCount });
            }
        }

        return new JourneyTrendSeries
        {
            OverallScoreTrend = overall,
            ReadinessTrend = readiness,
            ChapterImprovementTrend = chapter,
            RiskEvolutionTrend = risk,
        };
    }

    private static decimal? TryAverageChapterCompliance(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            JsonElement chapters;
            if (root.ValueKind == JsonValueKind.Array)
            {
                chapters = root;
            }
            else if (root.ValueKind == JsonValueKind.Object
                     && (root.TryGetProperty("Chapters", out chapters)
                         || root.TryGetProperty("chapters", out chapters)))
            {
                // Both historic PascalCase snapshots and current camelCase payloads are supported.
            }
            else
            {
                return null;
            }

            if (chapters.ValueKind != JsonValueKind.Array || chapters.GetArrayLength() == 0)
            {
                return null;
            }

            decimal sum = 0;
            var count = 0;
            foreach (var ch in chapters.EnumerateArray())
            {
                if (ch.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                if ((ch.TryGetProperty("CompliancePercent", out var pct)
                     || ch.TryGetProperty("compliancePercent", out pct)
                     || ch.TryGetProperty("completionPercentage", out pct))
                    && pct.ValueKind == JsonValueKind.Number
                    && pct.TryGetDecimal(out var value))
                {
                    sum += value;
                    count++;
                }
            }

            return count > 0 ? Math.Round(sum / count, 2) : null;
        }
        catch (JsonException)
        {
            return null;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }
}
