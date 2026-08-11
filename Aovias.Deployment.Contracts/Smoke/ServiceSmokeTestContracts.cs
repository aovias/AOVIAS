namespace Aovias.Deployment.Contracts.Smoke;

/// <summary>
/// Supported smoke test probe kinds declared in deployment contracts.
/// </summary>
public enum SmokeTestKind
{
    HttpGet,
    HttpHead,
    TcpConnect,
    JsonPathExists,
    StaticAsset
}

/// <summary>
/// One declarative smoke test for a deployed service.
/// </summary>
public sealed class ServiceSmokeTestDefinition
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public SmokeTestKind Kind { get; init; } = SmokeTestKind.HttpGet;

    public required string Path { get; init; }

    public int ExpectedStatus { get; init; } = 200;

    public int TimeoutSeconds { get; init; } = 15;

    public bool Required { get; init; } = true;

    public string? ExpectedContentContains { get; init; }
}

/// <summary>
/// Smoke test suite for one service.
/// </summary>
public sealed class ServiceSmokeTestSuite
{
    public required string ServiceId { get; init; }

    public IReadOnlyList<ServiceSmokeTestDefinition> Tests { get; init; } = [];
}

/// <summary>
/// Result of a single smoke test execution.
/// </summary>
public sealed class SmokeTestExecutionResult
{
    public required string ServiceId { get; init; }

    public required string TestId { get; init; }

    public required string TestName { get; init; }

    public bool Passed { get; init; }

    public int? StatusCode { get; init; }

    public string Message { get; init; } = string.Empty;

    public TimeSpan Duration { get; init; }
}

/// <summary>
/// Aggregated smoke test results for one or more services.
/// </summary>
public sealed class SmokeTestSuiteResult
{
    public bool Passed { get; init; }

    public IReadOnlyList<SmokeTestExecutionResult> Results { get; init; } = [];

    public int PassedCount { get; init; }

    public int FailedCount { get; init; }
}
