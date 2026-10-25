using System.Text.Json;
using System.Net.Sockets;
using Aovias.Release.Contracts.RemoteDeployment;

namespace Aovias.Release.Contracts.Deployment;

/// <summary>
/// Unified structured deployment failure propagated across all layers (Sprint 11.4).
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

    public string Documentation { get; init; } = string.Empty;

    public string? CorrelationId { get; init; }

    public string? DeploymentId { get; init; }

    public string? ServiceId { get; init; }

    public string? Path { get; init; }

    public string? Field { get; init; }

    public int? HttpStatusCode { get; init; }

    public string? StackTrace { get; init; }

    public IReadOnlyList<DeploymentExceptionDetail> ExceptionChain { get; init; } = [];

    public bool IsTransient { get; init; }
}

public sealed class DeploymentExceptionDetail
{
    public required string Type { get; init; }

    public required string Message { get; init; }

    public int? HResult { get; init; }

    public string? SocketError { get; init; }
}

/// <summary>
/// Maps exceptions and HTTP payloads to <see cref="DeploymentFailure"/> without losing context.
/// </summary>
public static class DeploymentFailureMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public static DeploymentFailure FromException(
        Exception exception,
        string phase,
        string component,
        string operation,
        DeploymentFailureCategory category = DeploymentFailureCategory.Application,
        string? serviceId = null,
        string? deploymentId = null,
        string? correlationId = null,
        int? httpStatusCode = null,
        string? path = null)
    {
        var mapped = DeploymentOperatorErrorMapper.Map(category, exception.Message, serviceId, httpStatusCode);
        var inner = exception.InnerException;
        var message = exception.Message;
        var recovery = mapped.RecoveryAction;

        if (path is null)
        {
            path = ExtractPathFromMessage(exception);
        }

        if (IsLegacyPathMismatch(message, path))
        {
            message =
                $"Deployment Root: /opt/aovias/deployments; Resolved Upload Directory: {path ?? "/opt/aovias/releases"}; Status: Configuration Mismatch";
            recovery = "DeploymentPathResolver returned an unexpected path. Verify deployment configuration.";
        }

        return new DeploymentFailure
        {
            Category = IsLegacyPathMismatch(message, path) ? "Filesystem" : category.ToString(),
            Phase = phase,
            Component = component,
            Operation = operation,
            ExceptionType = exception.GetType().Name,
            Message = message,
            InnerExceptionMessage = inner?.Message,
            Recovery = recovery,
            Documentation = mapped.DocumentationHint,
            CorrelationId = correlationId,
            DeploymentId = deploymentId,
            ServiceId = serviceId,
            Path = path,
            HttpStatusCode = httpStatusCode,
            StackTrace = exception.StackTrace,
            ExceptionChain = BuildExceptionChain(exception),
            IsTransient = IsTransient(exception)
        };
    }

    public static DeploymentFailure FromOperatorError(
        DeploymentOperatorError error,
        string phase,
        string component,
        string operation,
        string? deploymentId = null,
        string? correlationId = null)
        => new()
        {
            Category = error.Category.ToString(),
            Phase = phase,
            Component = component,
            Operation = operation,
            ExceptionType = string.Empty,
            Message = error.Reason,
            Recovery = error.RecoveryAction,
            Documentation = error.DocumentationHint,
            ServiceId = error.ServiceId,
            DeploymentId = deploymentId,
            CorrelationId = correlationId,
            HttpStatusCode = error.HttpStatusCode
        };

    public static DeploymentFailure FromHttpResponse(
        int statusCode,
        string? responseBody,
        string phase,
        string component,
        string operation,
        string? serviceId = null,
        string? correlationId = null)
    {
        if (TryParse(responseBody, out var parsed))
        {
            return Merge(parsed, phase, component, operation, serviceId, correlationId, statusCode);
        }

        var category = statusCode switch
        {
            401 => DeploymentFailureCategory.Authentication,
            404 => DeploymentFailureCategory.Upload,
            400 => DeploymentFailureCategory.Package,
            _ => DeploymentFailureCategory.Infrastructure
        };

        var message = ExtractPlainMessage(responseBody) ?? $"HTTP {statusCode} returned by Runtime Agent.";
        var mapped = DeploymentOperatorErrorMapper.Map(category, message, serviceId, statusCode);

        return new DeploymentFailure
        {
            Category = category.ToString(),
            Phase = phase,
            Component = component,
            Operation = operation,
            Message = message,
            Recovery = mapped.RecoveryAction,
            Documentation = mapped.DocumentationHint,
            ServiceId = serviceId,
            CorrelationId = correlationId,
            HttpStatusCode = statusCode
        };
    }

    public static bool TryParse(string? json, out DeploymentFailure failure)
    {
        failure = null!;
        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        var trimmed = json.Trim();
        if (!trimmed.StartsWith('{'))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(trimmed);
            var root = document.RootElement;

            if (root.TryGetProperty("category", out _) || root.TryGetProperty("Category", out _))
            {
                var deserialized = JsonSerializer.Deserialize<DeploymentFailure>(trimmed, JsonOptions);
                if (deserialized is not null && !string.IsNullOrWhiteSpace(deserialized.Message))
                {
                    failure = deserialized;
                    return true;
                }
            }

            if (root.TryGetProperty("reason", out var reasonProp))
            {
                failure = new DeploymentFailure
                {
                    Category = ReadString(root, "category") ?? "RequestValidation",
                    Phase = ReadString(root, "phase") ?? string.Empty,
                    Component = ReadString(root, "component") ?? string.Empty,
                    Operation = ReadString(root, "operation") ?? string.Empty,
                    ExceptionType = ReadString(root, "exception") ?? ReadString(root, "exceptionType") ?? string.Empty,
                    Message = reasonProp.GetString() ?? ReadString(root, "message") ?? trimmed,
                    Recovery = ReadString(root, "recovery") ?? "Review the deployment request and retry.",
                    Documentation = ReadString(root, "documentation") ?? string.Empty,
                    Field = ReadString(root, "field"),
                    Path = ReadString(root, "path"),
                    CorrelationId = ReadString(root, "correlationId"),
                    DeploymentId = ReadString(root, "deploymentId"),
                    ServiceId = ReadString(root, "serviceId")
                };
                return true;
            }

            if (root.TryGetProperty("detail", out var detail))
            {
                failure = new DeploymentFailure
                {
                    Category = ReadString(root, "title") ?? "Application",
                    Message = detail.GetString() ?? trimmed,
                    Recovery = "Review Runtime Agent logs for the matching correlation id.",
                    CorrelationId = ReadString(root, "traceId")
                };
                return true;
            }

            if (root.TryGetProperty("message", out var messageProp))
            {
                failure = new DeploymentFailure
                {
                    Category = "Application",
                    Message = messageProp.GetString() ?? trimmed,
                    Recovery = "Review the deployment console and runtime logs."
                };
                return true;
            }
        }
        catch (JsonException)
        {
            return false;
        }

        return false;
    }

    public static DeploymentFailureCategory Classify(Exception exception)
        => exception switch
        {
            UnauthorizedAccessException => DeploymentFailureCategory.Authentication,
            FileNotFoundException => DeploymentFailureCategory.Upload,
            DirectoryNotFoundException => DeploymentFailureCategory.Infrastructure,
            IOException => DeploymentFailureCategory.Infrastructure,
            InvalidOperationException operation when operation.Message.Contains("validation", StringComparison.OrdinalIgnoreCase)
                => DeploymentFailureCategory.Contracts,
            InvalidOperationException operation when operation.Message.Contains("docker", StringComparison.OrdinalIgnoreCase)
                => DeploymentFailureCategory.Docker,
            InvalidOperationException operation when operation.Message.Contains("health", StringComparison.OrdinalIgnoreCase)
                => DeploymentFailureCategory.Health,
            _ => DeploymentFailureCategory.Application
        };

    public static bool IsTransient(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is TimeoutException)
            {
                return true;
            }

            if (current is OperationCanceledException)
            {
                return true;
            }

            if (current is SocketException socket
                && socket.SocketErrorCode is SocketError.ConnectionReset
                    or SocketError.ConnectionAborted
                    or SocketError.TimedOut
                    or SocketError.NetworkDown
                    or SocketError.NetworkUnreachable
                    or SocketError.HostDown
                    or SocketError.HostUnreachable
                    or SocketError.TryAgain)
            {
                return true;
            }

            if (current is HttpRequestException request
                && request.StatusCode is null
                    or System.Net.HttpStatusCode.RequestTimeout
                    or System.Net.HttpStatusCode.BadGateway
                    or System.Net.HttpStatusCode.ServiceUnavailable
                    or System.Net.HttpStatusCode.GatewayTimeout)
            {
                return true;
            }
        }

        return false;
    }

    private static IReadOnlyList<DeploymentExceptionDetail> BuildExceptionChain(Exception exception)
    {
        var details = new List<DeploymentExceptionDetail>();
        for (var current = exception; current is not null; current = current.InnerException)
        {
            details.Add(new DeploymentExceptionDetail
            {
                Type = current.GetType().FullName ?? current.GetType().Name,
                Message = current.Message,
                HResult = current.HResult,
                SocketError = current is SocketException socket ? socket.SocketErrorCode.ToString() : null
            });
        }

        return details;
    }

    private static DeploymentFailure Merge(
        DeploymentFailure parsed,
        string phase,
        string component,
        string operation,
        string? serviceId,
        string? correlationId,
        int statusCode)
        => new()
        {
            Category = parsed.Category,
            Phase = string.IsNullOrWhiteSpace(parsed.Phase) ? phase : parsed.Phase,
            Component = string.IsNullOrWhiteSpace(parsed.Component) ? component : parsed.Component,
            Operation = string.IsNullOrWhiteSpace(parsed.Operation) ? operation : parsed.Operation,
            ExceptionType = parsed.ExceptionType,
            Message = parsed.Message,
            InnerExceptionMessage = parsed.InnerExceptionMessage,
            Recovery = parsed.Recovery,
            Documentation = parsed.Documentation,
            CorrelationId = parsed.CorrelationId ?? correlationId,
            DeploymentId = parsed.DeploymentId,
            ServiceId = parsed.ServiceId ?? serviceId,
            Path = parsed.Path,
            Field = parsed.Field,
            HttpStatusCode = parsed.HttpStatusCode ?? statusCode,
            StackTrace = parsed.StackTrace,
            ExceptionChain = parsed.ExceptionChain,
            IsTransient = parsed.IsTransient
        };

    private static string? ExtractPlainMessage(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }

        if (TryParse(body, out var failure))
        {
            return failure.Message;
        }

        return body.Trim('"');
    }

    private static string? ReadString(JsonElement root, string propertyName)
        => root.TryGetProperty(propertyName, out var value) ? value.GetString() : null;

    private static string? ExtractPathFromMessage(Exception exception)
    {
        const string marker = "path '";
        var text = exception.Message;
        var start = text.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (start < 0)
        {
            return null;
        }

        start += marker.Length;
        var end = text.IndexOf('\'', start);
        return end > start ? text[start..end] : null;
    }

    private static bool IsLegacyPathMismatch(string message, string? path)
        => message.Contains("/opt/aovias/releases", StringComparison.OrdinalIgnoreCase)
           || (path?.Contains("/opt/aovias/releases", StringComparison.OrdinalIgnoreCase) ?? false);
}
