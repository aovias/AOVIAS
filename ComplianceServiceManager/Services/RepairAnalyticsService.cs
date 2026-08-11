using System.Collections.Concurrent;
using System.Text.Json;
using ComplianceServiceManager.Models;
using System.IO;

namespace ComplianceServiceManager.Services;

public sealed class RepairAnalyticsService : IRepairAnalyticsService
{
    private readonly ConcurrentBag<AnalyticsEntry> entries = new();
    private readonly string analyticsFilePath;

    public RepairAnalyticsService()
    {
        var analyticsDirectory = Path.Combine(AppContext.BaseDirectory, "Diagnostics", "Analytics");
        Directory.CreateDirectory(analyticsDirectory);
        analyticsFilePath = Path.Combine(analyticsDirectory, "repair-analytics.json");
    }

    public async Task RecordAsync(ServiceInfoModel service, TimeSpan duration, bool success, FailureCategory category, CancellationToken cancellationToken)
    {
        var entry = new AnalyticsEntry
        {
            ServiceName = service.Name,
            DurationMs = (long)duration.TotalMilliseconds,
            Success = success,
            FailureCategory = category,
            Timestamp = DateTimeOffset.UtcNow
        };

        entries.Add(entry);
        var snapshot = entries.ToArray().OrderByDescending(item => item.Timestamp).Take(500).ToArray();
        await using var stream = File.Create(analyticsFilePath);
        await JsonSerializer.SerializeAsync(stream, snapshot, cancellationToken: cancellationToken);
    }

    public RepairAnalyticsSnapshot GetSnapshot()
    {
        var items = entries.ToArray();
        if (items.Length == 0)
        {
            return new RepairAnalyticsSnapshot
            {
                TotalAttempts = 0,
                SuccessfulAttempts = 0,
                FailedAttempts = 0,
                SuccessRate = 0,
                AverageRepairDuration = TimeSpan.Zero,
                Recommendations = new[] { "No repairs executed yet. Run AI Fix on a failed service to populate analytics." }
            };
        }

        var total = items.Length;
        var success = items.Count(item => item.Success);
        var failed = total - success;
        var avgDuration = TimeSpan.FromMilliseconds(items.Average(item => item.DurationMs));
        var mostFrequentCategory = items
            .Where(item => item.FailureCategory != FailureCategory.Unknown)
            .GroupBy(item => item.FailureCategory)
            .OrderByDescending(group => group.Count())
            .FirstOrDefault()?.Key ?? FailureCategory.Unknown;

        var recommendations = new List<string>();
        if (mostFrequentCategory == FailureCategory.PortConflict)
        {
            recommendations.Add("Port conflicts are frequent. Enable auto-port reassignment and stop orphaned dotnet processes before bulk start.");
        }
        else if (mostFrequentCategory == FailureCategory.NuGetRestoreIssue)
        {
            recommendations.Add("NuGet restore failures are frequent. Run cache clear and pre-restore all projects before startup.");
        }
        else if (mostFrequentCategory == FailureCategory.EfMigrationIssue)
        {
            recommendations.Add("Migration errors are frequent. Validate DB history table and run targeted `dotnet ef database update` per service.");
        }
        else
        {
            recommendations.Add("Review failed repair sessions and promote recurrent manual fixes into automated preflight steps.");
        }

        return new RepairAnalyticsSnapshot
        {
            TotalAttempts = total,
            SuccessfulAttempts = success,
            FailedAttempts = failed,
            SuccessRate = Math.Round(success * 100d / total, 2),
            AverageRepairDuration = avgDuration,
            Recommendations = recommendations
        };
    }

    private sealed class AnalyticsEntry
    {
        public required string ServiceName { get; init; }

        public required long DurationMs { get; init; }

        public required bool Success { get; init; }

        public required FailureCategory FailureCategory { get; init; }

        public required DateTimeOffset Timestamp { get; init; }
    }
}
