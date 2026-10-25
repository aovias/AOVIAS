using System.Net.Http;
using Aovias.Release.Contracts.Deployment;
using Aovias.Runtime.Contracts.Deployment;

namespace ComplianceServiceManager.Services.Deployment;

/// <summary>
/// Converts deployment failures into operator-facing UI messages without generic replacements (Sprint 11.4).
/// </summary>
public static class DeploymentHttpErrorPresenter
{
    public sealed record Presentation(
        string Category,
        string Phase,
        string Component,
        string Reason,
        string Details,
        string Recovery,
        string? ServiceId,
        string? CorrelationId,
        string? DeploymentId,
        int? StatusCode);

    public static Presentation Map(Exception exception, string? serviceId = null)
    {
        if (exception is Aovias.Release.SDK.DeploymentFailureException failureException)
        {
            return FromFailure(failureException.Failure, serviceId);
        }

        var message = exception.Message;
        var statusCode = ExtractStatusCode(message);
        var jsonStart = message.IndexOf('{');
        if (jsonStart >= 0 && DeploymentFailureMapper.TryParse(message[jsonStart..], out var parsed))
        {
            return FromFailure(parsed, serviceId ?? parsed.ServiceId);
        }

        if (message.Contains("copying content to a stream", StringComparison.OrdinalIgnoreCase)
            || message.Contains("AllowSynchronousIO", StringComparison.OrdinalIgnoreCase)
            || message.Contains("Synchronous operations are disallowed", StringComparison.OrdinalIgnoreCase)
            || exception is HttpRequestException)
        {
            var recovery = message.Contains("Synchronous operations are disallowed", StringComparison.OrdinalIgnoreCase)
                || message.Contains("AllowSynchronousIO", StringComparison.OrdinalIgnoreCase)
                ? "Update Runtime Agent on Demo VPS (republish/bootstrap), then retry Gateway deploy. Chunked upload requires the async chunk fix."
                : "Retry deploy. Single-service packages use chunked upload; confirm Demo VPS is reachable.";

            return new Presentation(
                Category: "Upload",
                Phase: "Upload",
                Component: "DeploymentClient",
                Reason: "Package transfer was interrupted before the Runtime Agent acknowledged the chunk.",
                Details: exception.InnerException?.Message
                    ?? "The package upload stream was interrupted before the Runtime Agent received the full file.",
                Recovery: recovery,
                ServiceId: serviceId,
                CorrelationId: null,
                DeploymentId: null,
                StatusCode: statusCode);
        }

        return new Presentation(
            Category: "Application",
            Phase: "Request",
            Component: "AOVIAS Command",
            Reason: message,
            Details: exception.InnerException?.Message ?? string.Empty,
            Recovery: "Review the deployment trace log and Runtime Agent boundary logs.",
            ServiceId: serviceId,
            CorrelationId: null,
            DeploymentId: null,
            StatusCode: statusCode);
    }

    public static Presentation FromFailure(DeploymentFailure failure, string? fallbackServiceId = null)
        => new(
            Category: failure.Category,
            Phase: string.IsNullOrWhiteSpace(failure.Phase) ? "Unknown" : failure.Phase,
            Component: string.IsNullOrWhiteSpace(failure.Component) ? "Unknown" : failure.Component,
            Reason: failure.Message,
            Details: BuildDetails(failure),
            Recovery: failure.Recovery,
            ServiceId: failure.ServiceId ?? fallbackServiceId,
            CorrelationId: failure.CorrelationId,
            DeploymentId: failure.DeploymentId,
            StatusCode: failure.HttpStatusCode);

    public static Presentation FromSignalR(DeploymentOperatorErrorDto failure, string? fallbackServiceId = null)
        => new(
            Category: failure.Category,
            Phase: string.IsNullOrWhiteSpace(failure.Phase) ? "Deploy" : failure.Phase,
            Component: string.IsNullOrWhiteSpace(failure.Component) ? "PlatformDeploymentService" : failure.Component,
            Reason: failure.Reason,
            Details: BuildDetails(failure),
            Recovery: failure.RecoveryAction,
            ServiceId: failure.ServiceId ?? fallbackServiceId,
            CorrelationId: failure.CorrelationId,
            DeploymentId: failure.DeploymentId,
            StatusCode: failure.HttpStatusCode);

    public static string FormatStatusMessage(Presentation presentation)
        => $"{presentation.Category} · {presentation.Phase}: {presentation.Reason}";

    public static string FormatTimelineMessage(Presentation presentation)
        => $"Component: {presentation.Component}\nDetails: {presentation.Details}\nRecovery: {presentation.Recovery}";

    private static string BuildDetails(DeploymentFailure failure)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(failure.ExceptionType))
        {
            parts.Add($"Exception: {failure.ExceptionType}");
        }

        if (!string.IsNullOrWhiteSpace(failure.InnerExceptionMessage))
        {
            parts.Add($"Inner: {failure.InnerExceptionMessage}");
        }

        if (!string.IsNullOrWhiteSpace(failure.Field))
        {
            parts.Add($"Field: {failure.Field}");
        }

        if (!string.IsNullOrWhiteSpace(failure.Path))
        {
            parts.Add($"Path: {failure.Path}");
        }

        if (failure.HttpStatusCode is not null)
        {
            parts.Add($"HTTP: {failure.HttpStatusCode}");
        }

        if (failure.ExceptionChain.Count > 0)
        {
            parts.Add(
                "Cause: " +
                string.Join(
                    " → ",
                    failure.ExceptionChain.Select(detail =>
                        $"{detail.Type}: {detail.Message}" +
                        (string.IsNullOrWhiteSpace(detail.SocketError) ? string.Empty : $" [Socket={detail.SocketError}]") +
                        (detail.HResult is null ? string.Empty : $" [HResult=0x{detail.HResult:X8}]"))));
        }

        return parts.Count == 0 ? failure.Message : string.Join("; ", parts);
    }

    private static string BuildDetails(DeploymentOperatorErrorDto failure)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(failure.ExceptionType))
        {
            parts.Add($"Exception: {failure.ExceptionType}");
        }

        if (!string.IsNullOrWhiteSpace(failure.InnerExceptionMessage))
        {
            parts.Add($"Inner: {failure.InnerExceptionMessage}");
        }

        if (!string.IsNullOrWhiteSpace(failure.Operation))
        {
            parts.Add($"Operation: {failure.Operation}");
        }

        return parts.Count == 0 ? failure.Reason : string.Join("; ", parts);
    }

    private static int? ExtractStatusCode(string message)
    {
        var match = System.Text.RegularExpressions.Regex.Match(message, @"\b(401|404|400|500|502|503)\b");
        return match.Success ? int.Parse(match.Value) : null;
    }
}
