namespace Aovias.Release.Contracts.Packages;

/// <summary>
/// health.json content describing post-deployment health checks.
/// </summary>
public sealed class HealthDescription
{
    public required string HealthEndpoint { get; init; }

    public int ExpectedStatusCode { get; init; } = 200;

    public TimeSpan Timeout { get; init; }

    public int RetryCount { get; init; }

    public int FailureThreshold { get; init; }
}
