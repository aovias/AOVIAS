namespace Aovias.Release.Contracts.RemoteDeployment;

/// <summary>
/// Operator-facing deployment error with recovery guidance (Sprint 12 D18).
/// </summary>
public sealed class DeploymentOperatorError
{
    public required DeploymentFailureCategory Category { get; init; }

    public required string Title { get; init; }

    public required string Reason { get; init; }

    public required string RecoveryAction { get; init; }

    public string DocumentationHint { get; init; } = string.Empty;

    public string? ServiceId { get; init; }

    public string? RequiredSecret { get; init; }

    public int? HttpStatusCode { get; init; }
}

/// <summary>
/// Maps failures to actionable operator guidance.
/// </summary>
public static class DeploymentOperatorErrorMapper
{
    public static DeploymentOperatorError Map(
        DeploymentFailureCategory category,
        string message,
        string? serviceId = null,
        int? httpStatusCode = null)
    {
        var normalized = message.Trim();
        var requiredSecret = ExtractSecretName(normalized);

        return category switch
        {
            DeploymentFailureCategory.Secrets or DeploymentFailureCategory.Configuration
                when requiredSecret is not null => new DeploymentOperatorError
                {
                    Category = DeploymentFailureCategory.Secrets,
                    Title = "Secrets",
                    Reason = normalized,
                    RecoveryAction = "Open Runtime Secrets",
                    DocumentationHint = "Configure required secrets in the Runtime Agent profile before redeploying.",
                    ServiceId = serviceId,
                    RequiredSecret = requiredSecret,
                    HttpStatusCode = httpStatusCode
                },
            DeploymentFailureCategory.Certificates => new DeploymentOperatorError
            {
                Category = category,
                Title = "Certificate",
                Reason = normalized,
                RecoveryAction = "Open Runtime Certificates",
                DocumentationHint = "Verify certificate paths and passwords in the runtime profile.",
                ServiceId = serviceId,
                HttpStatusCode = httpStatusCode
            },
            DeploymentFailureCategory.Network or DeploymentFailureCategory.Docker
                when normalized.Contains("network", StringComparison.OrdinalIgnoreCase) => new DeploymentOperatorError
                {
                    Category = DeploymentFailureCategory.Network,
                    Title = "Infrastructure",
                    Reason = "Docker network missing or unreachable.",
                    RecoveryAction = "Create Network",
                    DocumentationHint = "Ensure the compose network exists on the target runtime host.",
                    ServiceId = serviceId,
                    HttpStatusCode = httpStatusCode
                },
            DeploymentFailureCategory.Docker => new DeploymentOperatorError
            {
                Category = category,
                Title = "Docker",
                Reason = normalized,
                RecoveryAction = "Inspect Docker Runtime",
                DocumentationHint = "Review container logs and compose configuration on the runtime host.",
                ServiceId = serviceId,
                HttpStatusCode = httpStatusCode
            },
            DeploymentFailureCategory.Health or DeploymentFailureCategory.Smoke => new DeploymentOperatorError
            {
                Category = category,
                Title = "Health",
                Reason = normalized,
                RecoveryAction = "Review Health Checks",
                DocumentationHint = "Confirm service health endpoints and startup timeouts.",
                ServiceId = serviceId,
                HttpStatusCode = httpStatusCode
            },
            DeploymentFailureCategory.Database => new DeploymentOperatorError
            {
                Category = category,
                Title = "Database",
                Reason = normalized,
                RecoveryAction = "Verify Database Connectivity",
                DocumentationHint = "Check connection strings, migrations, and SQL availability.",
                ServiceId = serviceId,
                HttpStatusCode = httpStatusCode
            },
            DeploymentFailureCategory.Contracts or DeploymentFailureCategory.Package => new DeploymentOperatorError
            {
                Category = category,
                Title = category == DeploymentFailureCategory.Package ? "Package" : "Contracts",
                Reason = normalized,
                RecoveryAction = "Republish Package",
                DocumentationHint = "Run publish preflight and regenerate deployment contracts.",
                ServiceId = serviceId,
                HttpStatusCode = httpStatusCode
            },
            DeploymentFailureCategory.Authentication => new DeploymentOperatorError
            {
                Category = category,
                Title = "Authentication",
                Reason = normalized,
                RecoveryAction = "Verify Runtime API Key",
                DocumentationHint = "Confirm the environment registration and API key in AOVIAS Command.",
                ServiceId = serviceId,
                HttpStatusCode = httpStatusCode
            },
            DeploymentFailureCategory.Infrastructure => new DeploymentOperatorError
            {
                Category = category,
                Title = "Infrastructure",
                Reason = normalized,
                RecoveryAction = "Run Infrastructure Preflight",
                DocumentationHint = "Validate filesystem, networks, and runtime folders on the target host.",
                ServiceId = serviceId,
                HttpStatusCode = httpStatusCode
            },
            DeploymentFailureCategory.Rollback => new DeploymentOperatorError
            {
                Category = category,
                Title = "Rollback",
                Reason = normalized,
                RecoveryAction = "Review Rollback Plan",
                DocumentationHint = "Inspect rollback snapshots and redeploy the previous certified release.",
                ServiceId = serviceId,
                HttpStatusCode = httpStatusCode
            },
            _ => new DeploymentOperatorError
            {
                Category = category,
                Title = category.ToString(),
                Reason = normalized,
                RecoveryAction = "Review Deployment Console",
                DocumentationHint = "Inspect runtime logs and certification report for this deployment.",
                ServiceId = serviceId,
                HttpStatusCode = httpStatusCode
            }
        };
    }

    private static string? ExtractSecretName(string message)
    {
        if (message.Contains("certificatePassword", StringComparison.OrdinalIgnoreCase))
        {
            return "certificatePassword";
        }

        if (message.Contains("secret", StringComparison.OrdinalIgnoreCase))
        {
            const string marker = "secret ";
            var index = message.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (index >= 0)
            {
                var remainder = message[(index + marker.Length)..].Trim();
                var token = remainder.Split([' ', '.', ',', ';'], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
                return string.IsNullOrWhiteSpace(token) ? null : token;
            }
        }

        return null;
    }
}
