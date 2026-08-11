using Aovias.Deployment.Contracts.Provisioning;

namespace Aovias.Deployment.Contracts.Infrastructure.Provisioners;

public sealed class SecretInfrastructureProvisioner : IInfrastructureProvisioner
{
    public string StageName => "Secrets";

    public Task<InfrastructureStageResult> ProvisionAsync(
        InfrastructureProvisioningContext context,
        CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        if (!context.Contract.Secrets.Secrets.Any(item => item.Required))
        {
            return Task.FromResult(InfrastructureStageResult.Skipped(StageName, "No required secrets declared."));
        }

        var report = ContractSecretValidator.Validate(context.HostPaths.HostConfigRoot, context.Contract.Secrets);
        if (!report.Passed)
        {
            return Task.FromResult(InfrastructureStageResult.Failed(StageName, report.Errors));
        }

        var count = context.Contract.Secrets.Secrets.Count(item => item.Required);
        return Task.FromResult(InfrastructureStageResult.Ready(
            StageName,
            [$"{count} required secret(s) validated from settings.secrets.json."]));
    }
}
