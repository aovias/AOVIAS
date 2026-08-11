using System.Diagnostics;
using System.Text.Json;

namespace ComplianceService.Application.Dashboard.Summary;

/// <summary>
/// Timing marks for dashboard summary engine execution.
/// </summary>
public sealed class DashboardSummaryTelemetry
{
    public long ContextResolutionMs { get; set; }

    public long CacheLookupMs { get; set; }

    public bool CacheHit { get; set; }

    public long QueryExecutionMs { get; set; }

    public long CachePopulationMs { get; set; }

    public long TotalMs { get; set; }

    public string ToCompactJson() => JsonSerializer.Serialize(this);
}

public static class DashboardSummaryTelemetryKeys
{
    public const string HttpContextItemKey = "DashboardSummaryTelemetry";
}

public sealed class DashboardSummaryTelemetryScope : IDisposable
{
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

    public DashboardSummaryTelemetryScope(DashboardSummaryTelemetry telemetry)
    {
        Telemetry = telemetry;
    }

    public DashboardSummaryTelemetry Telemetry { get; }

    public void MarkContextResolution() => Telemetry.ContextResolutionMs = _stopwatch.ElapsedMilliseconds;

    public void MarkCacheLookup(bool hit)
    {
        Telemetry.CacheLookupMs = _stopwatch.ElapsedMilliseconds;
        Telemetry.CacheHit = hit;
    }

    public void MarkQueryExecution() => Telemetry.QueryExecutionMs = _stopwatch.ElapsedMilliseconds;

    public void MarkCachePopulation() => Telemetry.CachePopulationMs = _stopwatch.ElapsedMilliseconds;

    public void Complete()
    {
        Telemetry.TotalMs = _stopwatch.ElapsedMilliseconds;
        _stopwatch.Stop();
    }

    public void Dispose() => Complete();
}
