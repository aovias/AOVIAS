using Aovias.Deployment.Contracts;

using Aovias.Deployment.Contracts.Provisioning;



namespace Aovias.Deployment.Contracts.Infrastructure.Provisioners;



public sealed class FilesystemInfrastructureProvisioner : IInfrastructureProvisioner

{

    public string StageName => "Filesystem";



    public Task<InfrastructureStageResult> ProvisionAsync(

        InfrastructureProvisioningContext context,

        CancellationToken cancellationToken = default)

    {

        _ = cancellationToken;

        var contract = context.Contract.Filesystem;

        if (contract.Directories.Count == 0 && contract.ReadOnlyDirectories.Count == 0)

        {

            return Task.FromResult(InfrastructureStageResult.Skipped(StageName, "No filesystem directories declared."));

        }



        var created = ContractFilesystemProvisioner.EnsureHostDirectories(context.Bundle, context.HostPaths);

        var writableHostPaths = CollectWritableHostPaths(context.Bundle, context.HostPaths);

        var readOnlyHostPaths = CollectReadOnlyHostPaths(context.Bundle, context.HostPaths);



        var errors = new List<string>();

        errors.AddRange(HostPathWriteAccessValidator.ValidateWritable(writableHostPaths.ToArray()));

        errors.AddRange(HostPathWriteAccessValidator.ValidateReadable(readOnlyHostPaths.ToArray()));

        if (errors.Count > 0)

        {

            return Task.FromResult(InfrastructureStageResult.Failed(StageName, errors));

        }



        var messages = created.Count == 0

            ? ["Filesystem directories verified."]

            : created.Select(path => $"Created or verified directory '{path}'.").ToArray();



        return Task.FromResult(InfrastructureStageResult.Ready(StageName, messages));

    }



    private static IReadOnlyList<string> CollectWritableHostPaths(

        ServiceDeploymentBundle bundle,

        HostPathContext hostPaths)

    {

        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var directory in bundle.Filesystem.Directories)

        {

            var hostPath = ContractFilesystemProvisioner.ResolveHostPath(directory.Path, bundle.Volume, hostPaths);

            if (!string.IsNullOrWhiteSpace(hostPath))

            {

                paths.Add(hostPath);

            }

        }



        return paths.ToList();

    }



    private static IReadOnlyList<string> CollectReadOnlyHostPaths(

        ServiceDeploymentBundle bundle,

        HostPathContext hostPaths)

    {

        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var directory in bundle.Filesystem.ReadOnlyDirectories)

        {

            var hostPath = ContractFilesystemProvisioner.ResolveHostPath(directory.Path, bundle.Volume, hostPaths);

            if (!string.IsNullOrWhiteSpace(hostPath))

            {

                paths.Add(hostPath);

            }

        }



        return paths.ToList();

    }

}


