namespace Aovias.Deployment.Contracts.Infrastructure;

/// <summary>
/// Applies idempotent recovery actions for common infrastructure gaps.
/// </summary>
public static class InfrastructureRecoveryCoordinator
{
    public static IReadOnlyList<string> DescribeRecoveryActions(InfrastructureReadinessReport report)
    {
        var actions = new List<string>();
        foreach (var stage in report.Stages.Where(item => !item.Passed))
        {
            actions.AddRange(stage.RecoveryActions);
            actions.AddRange(stage.Errors.Select(error => MapRecovery(stage.Stage, error)));
        }

        return actions.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static string MapRecovery(string stage, string error)
    {
        if (error.Contains("directory", StringComparison.OrdinalIgnoreCase)
            || error.Contains("folder", StringComparison.OrdinalIgnoreCase))
        {
            return "Recovery: create missing filesystem directory and rerun provisioning.";
        }

        if (error.Contains("permission", StringComparison.OrdinalIgnoreCase)
            || error.Contains("denied", StringComparison.OrdinalIgnoreCase))
        {
            return "Recovery: grant write permission to host logs/packages directories and rerun provisioning.";
        }

        if (error.Contains("RabbitMQ", StringComparison.OrdinalIgnoreCase))
        {
            return "Recovery: verify RabbitMQ host, port, and credentials.";
        }

        if (error.Contains("network", StringComparison.OrdinalIgnoreCase))
        {
            return "Recovery: create Docker network and rerun provisioning.";
        }

        if (error.Contains("secret", StringComparison.OrdinalIgnoreCase)
            || error.Contains("password", StringComparison.OrdinalIgnoreCase))
        {
            return "Recovery: populate settings.secrets.json and rerun provisioning.";
        }

        if (error.Contains("certificate", StringComparison.OrdinalIgnoreCase))
        {
            return "Recovery: regenerate or replace certificate using contract password secret.";
        }

        if (error.Contains("database", StringComparison.OrdinalIgnoreCase)
            || error.Contains("connection", StringComparison.OrdinalIgnoreCase))
        {
            return "Recovery: verify database connection string and SQL Server reachability.";
        }

        if (error.Contains("image", StringComparison.OrdinalIgnoreCase))
        {
            return "Recovery: load service image tar before deployment.";
        }

        return $"Recovery ({stage}): resolve '{error}' and rerun provisioning.";
    }
}
