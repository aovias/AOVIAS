using Aovias.Deployment.Contracts.Provisioning;

namespace Aovias.Deployment.Contracts.Infrastructure.Provisioners;

public sealed class ProviderInfrastructureProvisioner : IInfrastructureProvisioner
{
    public string StageName => "Providers";

    public async Task<InfrastructureStageResult> ProvisionAsync(
        InfrastructureProvisioningContext context,
        CancellationToken cancellationToken = default)
    {
        var requiredProviders = context.Contract.Providers.Where(item => item.Required).ToList();
        if (requiredProviders.Count == 0)
        {
            return InfrastructureStageResult.Skipped(StageName, "No required external providers declared.");
        }

        var errors = new List<string>();
        var messages = new List<string>();

        foreach (var provider in requiredProviders)
        {
            if (string.IsNullOrWhiteSpace(provider.SecretName))
            {
                messages.Add($"Provider '{provider.Provider}' declared (no secret binding).");
                continue;
            }

            if (!ContractSecretValidator.TryResolveSecretValue(
                    context.HostPaths.HostConfigRoot,
                    provider.SecretName,
                    out var secretValue))
            {
                errors.Add(
                    $"Provider '{provider.Provider}' requires secret '{provider.SecretName}' " +
                    $"(maps to '{provider.ConfigurationKey}').");
                continue;
            }

            if (context.Execution is not null
                && provider.Provider.Contains("Rabbit", StringComparison.OrdinalIgnoreCase))
            {
                var reachability = await context.Execution.ValidateMessageBusAsync(secretValue, cancellationToken);
                if (!reachability.Reachable)
                {
                    errors.Add(
                        $"RabbitMQ is unreachable: {reachability.Error ?? "unknown error"}. " +
                        "Recovery: verify RabbitMQ host, port, and credentials.");
                    continue;
                }
            }

            messages.Add($"Provider '{provider.Provider}' secret validated.");
        }

        if (errors.Count > 0)
        {
            return InfrastructureStageResult.Failed(StageName, errors);
        }

        return InfrastructureStageResult.Ready(StageName, messages.ToArray());
    }
}
