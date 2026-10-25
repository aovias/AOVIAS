namespace ComplianceServiceManager.Models.LocalResources;

/// <summary>
/// Point-in-time local host resource observation for AOVIAS Studio.
/// Distinct from Runtime Agent observability metrics.
/// </summary>
public sealed class LocalResourceSnapshot
{
    public DateTimeOffset CapturedAt { get; init; }

    public LocalHostResourceMetrics Host { get; init; } = new();

    public IReadOnlyList<LocalServiceProcessMetrics> Services { get; init; } = [];

    public TimeSpan SampleInterval { get; init; }

    public string Source { get; init; } = "LocalHost";
}
