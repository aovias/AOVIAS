namespace Aovias.Configuration.Deployment;

/// <summary>
/// Result of validating deployment storage layout and permissions.
/// </summary>
public sealed class DeploymentStorageValidationResult
{
    public required bool IsValid { get; init; }

    public required IReadOnlyList<DeploymentStoragePathCheck> Checks { get; init; }

    public string? FailureMessage { get; init; }

    public DeploymentFailure? StructuredFailure { get; init; }

    public static DeploymentStorageValidationResult Success(IReadOnlyList<DeploymentStoragePathCheck> checks)
        => new() { IsValid = true, Checks = checks };

    public static DeploymentStorageValidationResult Failure(
        IReadOnlyList<DeploymentStoragePathCheck> checks,
        string message,
        DeploymentFailure? structuredFailure = null)
        => new()
        {
            IsValid = false,
            Checks = checks,
            FailureMessage = message,
            StructuredFailure = structuredFailure
        };
}

/// <summary>
/// Individual path validation entry for startup and upload diagnostics.
/// </summary>
public sealed class DeploymentStoragePathCheck
{
    public required string Label { get; init; }

    public required string ResolvedPath { get; init; }

    public required string Status { get; init; }

    public string? Detail { get; init; }
}

/// <summary>
/// Structured deployment path failure propagated to operators.
/// </summary>
public sealed class DeploymentFailure
{
    public required string Category { get; init; }

    public string Phase { get; init; } = string.Empty;

    public string Component { get; init; } = string.Empty;

    public string Operation { get; init; } = string.Empty;

    public string ExceptionType { get; init; } = string.Empty;

    public required string Message { get; init; }

    public string? InnerExceptionMessage { get; init; }

    public required string Recovery { get; init; }

    public string? ExpectedPath { get; init; }

    public string? ResolvedPath { get; init; }

    public string? DeploymentRoot { get; init; }
}
