using Aovias.Deployment.Contracts.Provisioning;



namespace Aovias.Deployment.Contracts.Infrastructure.Provisioners;



public sealed class StorageInfrastructureProvisioner : IInfrastructureProvisioner

{

    public string StageName => "Storage";



    public Task<InfrastructureStageResult> ProvisionAsync(

        InfrastructureProvisioningContext context,

        CancellationToken cancellationToken = default)

    {

        _ = cancellationToken;

        if (context.Contract.Storage.Mounts.Count == 0)

        {

            return Task.FromResult(InfrastructureStageResult.Skipped(StageName, "No storage mounts declared."));

        }



        var created = new List<string>();

        var errors = new List<string>();

        foreach (var mount in context.Contract.Storage.Mounts)

        {

            var hostPath = context.HostPaths.ResolveHostToken(mount.HostToken);

            if (string.IsNullOrWhiteSpace(hostPath))

            {

                continue;

            }



            Directory.CreateDirectory(hostPath);

            created.Add(hostPath);



            if (mount.ReadOnly)

            {

                errors.AddRange(HostPathWriteAccessValidator.ValidateReadable(hostPath));

            }

            else

            {

                errors.AddRange(HostPathWriteAccessValidator.ValidateWritable(hostPath));

            }

        }



        if (errors.Count > 0)

        {

            return Task.FromResult(InfrastructureStageResult.Failed(StageName, errors));

        }



        var messages = created.Count == 0

            ? ["Storage mounts verified."]

            : created.Select(path => $"Storage mount verified at '{path}'.").ToArray();



        return Task.FromResult(InfrastructureStageResult.Ready(StageName, messages));

    }

}


