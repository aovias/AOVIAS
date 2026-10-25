using Aovias.Deployment.Contracts.Provisioning;

namespace Aovias.Deployment.Contracts.Infrastructure.Provisioners;

public sealed class DockerInfrastructureProvisioner : IInfrastructureProvisioner
{
    public string StageName => "Docker";

    public async Task<InfrastructureStageResult> ProvisionAsync(
        InfrastructureProvisioningContext context,
        CancellationToken cancellationToken = default)
    {
        var docker = context.Contract.Docker;
        if (!docker.RequireEngine)
        {
            return InfrastructureStageResult.Skipped(StageName, "Docker not required by contract.");
        }

        if (context.Execution is null)
        {
            return InfrastructureStageResult.Skipped(
                StageName,
                "Docker execution context unavailable; engine and network checks deferred to Runtime Agent.");
        }

        var engine = await context.Execution.InspectDockerEngineAsync(cancellationToken);
        if (!engine.Available)
        {
            return InfrastructureStageResult.Failed(
                StageName,
                [$"Docker engine is unavailable: {engine.Error ?? "unknown error"}"]);
        }

        if (docker.RequireCompose && !engine.ComposeAvailable)
        {
            return InfrastructureStageResult.Failed(
                StageName,
                ["Docker Compose is unavailable."]);
        }

        var messages = new List<string> { $"Docker engine available ({engine.Version ?? "version unknown"})." };

        if (docker.CreateNetworkIfMissing)
        {
            var networkName = ResolveNetworkName(context);
            if (!string.IsNullOrWhiteSpace(networkName))
            {
                await context.Execution.EnsureDockerNetworkAsync(networkName, cancellationToken);
                messages.Add($"Docker network '{networkName}' verified.");
            }
        }

        foreach (var image in docker.RequiredImages.Where(item => !string.IsNullOrWhiteSpace(item)))
        {
            if (!await context.Execution.ImageExistsLocallyAsync(image, cancellationToken))
            {
                return InfrastructureStageResult.Failed(
                    StageName,
                    [$"Required image '{image}' is not loaded locally."]);
            }

            messages.Add($"Required image '{image}' is loaded.");
        }

        return InfrastructureStageResult.Ready(StageName, messages.ToArray());
    }

    private static string? ResolveNetworkName(InfrastructureProvisioningContext context)
    {
        if (!string.IsNullOrWhiteSpace(context.DockerNetworkName))
        {
            return context.DockerNetworkName;
        }

        var token = context.Contract.Network.NetworkNameToken;
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        return token.StartsWith("${", StringComparison.Ordinal) && token.EndsWith('}')
            ? Environment.GetEnvironmentVariable(token[2..^1])
            : token;
    }
}
