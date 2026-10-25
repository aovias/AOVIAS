using Aovias.Deployment.Contracts.Provisioning;

namespace Aovias.Deployment.Contracts.Infrastructure.Provisioners;

public sealed class CertificateInfrastructureProvisioner : IInfrastructureProvisioner
{
    public string StageName => "Certificates";

    public Task<InfrastructureStageResult> ProvisionAsync(
        InfrastructureProvisioningContext context,
        CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        if (!context.Contract.Certificates.Certificates.Any(item => item.Required))
        {
            return Task.FromResult(InfrastructureStageResult.Skipped(StageName, "No certificates declared."));
        }

        if (!context.RuntimeCapabilities.Certificates)
        {
            return Task.FromResult(InfrastructureStageResult.Failed(
                StageName,
                ["Runtime does not provide certificate provisioning capability."]));
        }

        ContractCertificateProvisioner.EnsureProvisioned(context.HostPaths.HostConfigRoot, context.Contract.Certificates);

        var report = ContractCertificateValidator.Validate(context.HostPaths.HostConfigRoot, context.Contract.Certificates);
        if (!report.Passed)
        {
            var errors = report.Results
                .Where(item => !item.Passed)
                .SelectMany(item => item.Errors)
                .ToList();
            return Task.FromResult(InfrastructureStageResult.Failed(StageName, errors));
        }

        var messages = report.Results
            .Where(item => item.Passed)
            .Select(item => $"Certificate '{item.CertificateId}' validated (thumbprint={item.Thumbprint}).")
            .ToArray();

        return Task.FromResult(InfrastructureStageResult.Ready(StageName, messages));
    }
}
