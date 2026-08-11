using Aovias.Deployment.Contracts.Provisioning;

namespace Aovias.Deployment.Contracts.Infrastructure.Provisioners;

public sealed class DatabaseInfrastructureProvisioner : IInfrastructureProvisioner
{
    public string StageName => "Database";

    public async Task<InfrastructureStageResult> ProvisionAsync(
        InfrastructureProvisioningContext context,
        CancellationToken cancellationToken = default)
    {
        var database = context.Contract.Database;
        if (!database.Required || string.Equals(database.Provider, "None", StringComparison.OrdinalIgnoreCase))
        {
            return InfrastructureStageResult.Skipped(StageName, "No database required.");
        }

        if (!context.RuntimeCapabilities.SqlServer)
        {
            return InfrastructureStageResult.Failed(
                StageName,
                ["Runtime does not provide SQL Server capability required by contract."]);
        }

        var connectionSecret = ResolveConnectionSecret(context);
        if (string.IsNullOrWhiteSpace(connectionSecret))
        {
            return InfrastructureStageResult.Failed(
                StageName,
                [
                    $"Database connection secret is missing for '{database.ConnectionSetting}'. " +
                    "Add it to settings.secrets.json."
                ]);
        }

        if (context.Execution is not null)
        {
            var reachability = await context.Execution.ValidateDatabaseAsync(connectionSecret, cancellationToken);
            if (!reachability.Reachable)
            {
                return InfrastructureStageResult.Failed(
                    StageName,
                    [$"Database server is unreachable: {reachability.Error ?? "unknown error"}"]);
            }
        }

        var messages = new List<string> { $"Database provider '{database.Provider}' connection validated." };
        if (!string.Equals(database.Migration, "None", StringComparison.OrdinalIgnoreCase))
        {
            messages.Add($"Migration policy: {database.Migration}.");
        }

        return InfrastructureStageResult.Ready(StageName, messages.ToArray());
    }

    private static string? ResolveConnectionSecret(InfrastructureProvisioningContext context)
    {
        var candidates = new[]
        {
            "databaseConnectionString",
            "ConnectionStrings:DefaultConnection",
            "ConnectionStrings:ComplianceConnection",
            context.Contract.Database.ConnectionSetting
        };

        foreach (var candidate in candidates.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (ContractSecretValidator.TryResolveSecretValue(context.HostPaths.HostConfigRoot, candidate, out var value))
            {
                return value;
            }
        }

        return null;
    }
}
