namespace Aovias.Deployment.Contracts.Infrastructure;

/// <summary>
/// Generic infrastructure provisioning stage.
/// </summary>
public interface IInfrastructureProvisioner
{
    string StageName { get; }

    Task<InfrastructureStageResult> ProvisionAsync(
        InfrastructureProvisioningContext context,
        CancellationToken cancellationToken = default);
}
