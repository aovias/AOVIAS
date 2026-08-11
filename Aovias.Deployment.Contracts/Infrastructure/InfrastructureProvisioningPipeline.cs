namespace Aovias.Deployment.Contracts.Infrastructure;

/// <summary>
/// Executes the contract-driven infrastructure provisioning pipeline.
/// </summary>
public static class InfrastructureProvisioningPipeline
{
    private static readonly IReadOnlyList<IInfrastructureProvisioner> DefaultProvisioners =
    [
        new Provisioners.ValidationInfrastructureProvisioner(),
        new Provisioners.FilesystemInfrastructureProvisioner(),
        new Provisioners.DockerInfrastructureProvisioner(),
        new Provisioners.CertificateInfrastructureProvisioner(),
        new Provisioners.SecretInfrastructureProvisioner(),
        new Provisioners.StorageInfrastructureProvisioner(),
        new Provisioners.DatabaseInfrastructureProvisioner(),
        new Provisioners.ProviderInfrastructureProvisioner(),
        new Provisioners.DependencyInfrastructureProvisioner()
    ];

    public static async Task<InfrastructureReadinessReport> ExecuteAsync(
        InfrastructureProvisioningContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var warnings = new List<string>();
        foreach (var provisioner in DefaultProvisioners)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = await RunStageAsync(provisioner, context, cancellationToken);
            context.StageResults.Add(result);

            if (!result.Passed)
            {
                warnings.AddRange(result.RecoveryActions);
                break;
            }
        }

        return InfrastructureReadinessReport.Create(context.Bundle, context.StageResults.ToList(), warnings);
    }

    private static async Task<InfrastructureStageResult> RunStageAsync(
        IInfrastructureProvisioner provisioner,
        InfrastructureProvisioningContext context,
        CancellationToken cancellationToken)
    {
        try
        {
            return await provisioner.ProvisionAsync(context, cancellationToken);
        }
        catch (Exception exception)
        {
            return InfrastructureStageResult.Failed(
                provisioner.StageName,
                [$"{provisioner.StageName} provisioning failed: {exception.Message}"]);
        }
    }
}
