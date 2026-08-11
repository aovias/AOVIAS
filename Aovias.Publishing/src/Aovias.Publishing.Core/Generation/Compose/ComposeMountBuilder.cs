using Aovias.Deployment.Contracts;

namespace Aovias.Publishing.Core.Generation.Compose;

/// <summary>
/// Builds docker-compose volume entries exclusively from a <see cref="VolumeContract"/>.
/// </summary>
public static class ComposeMountBuilder
{
    public static IReadOnlyList<string> BuildVolumeEntries(VolumeContract volume)
    {
        ArgumentNullException.ThrowIfNull(volume);

        if (volume.Mounts.Count == 0)
        {
            throw new InvalidOperationException("Volume contract does not declare any bind mounts.");
        }

        var entries = new List<string>();
        var seenContainerPaths = new HashSet<string>(StringComparer.Ordinal);

        foreach (var mount in volume.Mounts.OrderBy(item => item.ContainerPath, StringComparer.Ordinal))
        {
            if (string.IsNullOrWhiteSpace(mount.HostToken) || string.IsNullOrWhiteSpace(mount.ContainerPath))
            {
                throw new InvalidOperationException("Volume mounts require hostToken and containerPath.");
            }

            if (!seenContainerPaths.Add(mount.ContainerPath))
            {
                throw new InvalidOperationException($"Duplicate volume mount declared for container path '{mount.ContainerPath}'.");
            }

            entries.Add(FormatMount(mount));
        }

        return entries;
    }

    public static string FormatMount(VolumeMountContract mount)
    {
        var entry = $"{mount.HostToken}:{mount.ContainerPath}";
        if (mount.ReadOnly)
        {
            entry += ":ro";
        }

        return entry;
    }
}
