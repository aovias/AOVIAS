namespace Aovias.Runtime.SDK;

/// <summary>
/// Diagnostics from the most recent SignalR connection attempt.
/// </summary>
public sealed class RuntimeHubConnectionDiagnostics
{
    public bool Connected { get; init; }

    public bool DiscoverySucceeded { get; init; }

    public string ExpectedHubPath { get; init; } = string.Empty;

    public string ConfiguredHubPath { get; init; } = string.Empty;

    public string AttemptedHubUrl { get; init; } = string.Empty;

    public string? FailureReason { get; init; }

    public static RuntimeHubConnectionDiagnostics Success(
        string expectedHubPath,
        string configuredHubPath,
        string attemptedHubUrl,
        bool discoverySucceeded)
        => new()
        {
            Connected = true,
            DiscoverySucceeded = discoverySucceeded,
            ExpectedHubPath = expectedHubPath,
            ConfiguredHubPath = configuredHubPath,
            AttemptedHubUrl = attemptedHubUrl
        };

    public static RuntimeHubConnectionDiagnostics Failure(
        string expectedHubPath,
        string configuredHubPath,
        string attemptedHubUrl,
        string failureReason,
        bool discoverySucceeded)
        => new()
        {
            Connected = false,
            DiscoverySucceeded = discoverySucceeded,
            ExpectedHubPath = expectedHubPath,
            ConfiguredHubPath = configuredHubPath,
            AttemptedHubUrl = attemptedHubUrl,
            FailureReason = failureReason
        };
}
