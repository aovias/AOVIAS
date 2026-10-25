namespace Aovias.Release.Contracts.Deployment;

/// <summary>
/// Structured deployment request validation error (Sprint 11.3).
/// </summary>
public sealed class DeploymentRequestValidationErrorDto
{
    public string Category { get; init; } = "RequestValidation";

    public required string Reason { get; init; }

    public string? Field { get; init; }

    public required string Recovery { get; init; }

    public string? Endpoint { get; init; }

    public string? ExpectedRequestType { get; init; }
}
