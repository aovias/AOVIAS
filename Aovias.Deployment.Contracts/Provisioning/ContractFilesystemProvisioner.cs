namespace Aovias.Deployment.Contracts.Provisioning;

/// <summary>
/// Prepares host-side directories declared by a filesystem contract.
/// </summary>
public static class ContractFilesystemProvisioner
{
    public static IReadOnlyList<string> EnsureHostDirectories(
        ServiceDeploymentBundle bundle,
        HostPathContext hostPaths)
    {
        var created = new List<string>();
        foreach (var directory in bundle.Filesystem.Directories.Concat(bundle.Filesystem.ReadOnlyDirectories))
        {
            var hostPath = ResolveHostPath(directory.Path, bundle.Volume, hostPaths);
            if (string.IsNullOrWhiteSpace(hostPath))
            {
                continue;
            }

            Directory.CreateDirectory(hostPath);
            created.Add(hostPath);
        }

        return created;
    }

    public static string? ResolveHostPath(
        string containerPath,
        VolumeContract volumes,
        HostPathContext hostPaths)
    {
        foreach (var mount in volumes.Mounts.OrderByDescending(item => item.ContainerPath.Length))
        {
            if (!containerPath.StartsWith(mount.ContainerPath, StringComparison.Ordinal))
            {
                continue;
            }

            var relative = containerPath[mount.ContainerPath.Length..].TrimStart('/');
            var hostRoot = hostPaths.ResolveHostToken(mount.HostToken);
            return string.IsNullOrEmpty(relative)
                ? hostRoot
                : Path.Combine(hostRoot, relative.Replace('/', Path.DirectorySeparatorChar));
        }

        return null;
    }
}
