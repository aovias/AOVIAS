namespace Aovias.Deployment.Contracts.Validation;

/// <summary>
/// Validates generated docker-compose bind mounts against deployment volume and filesystem contracts.
/// </summary>
public static class ComposeMountValidator
{
    public static ComposeMountValidationReport Validate(
        string composeContent,
        VolumeContract volume,
        FilesystemContract? filesystem = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(composeContent);
        ArgumentNullException.ThrowIfNull(volume);

        return ValidateParsedMounts(ParseComposeVolumes(composeContent), volume, filesystem);
    }

    public static ComposeMountValidationReport ValidateVolumeEntries(
        IReadOnlyList<string> volumeEntries,
        VolumeContract volume,
        FilesystemContract? filesystem = null)
    {
        ArgumentNullException.ThrowIfNull(volumeEntries);
        ArgumentNullException.ThrowIfNull(volume);

        var parsed = volumeEntries
            .Select(ParseVolumeEntry)
            .Where(item => item is not null)
            .Cast<ParsedComposeMount>()
            .ToList();

        return ValidateParsedMounts(parsed, volume, filesystem);
    }

    private static ComposeMountValidationReport ValidateParsedMounts(
        IReadOnlyList<ParsedComposeMount> parsed,
        VolumeContract volume,
        FilesystemContract? filesystem)
    {
        var checks = new List<ComposeMountValidationCheck>();
        var errors = new List<string>();

        errors.AddRange(DetectStructuralIssues(parsed));

        foreach (var mount in volume.Mounts)
        {
            var matching = parsed
                .Where(item => string.Equals(item.ContainerPath, mount.ContainerPath, StringComparison.Ordinal))
                .ToList();

            if (matching.Count == 0)
            {
                checks.Add(ComposeMountValidationCheck.Fail(
                    ResolveLabel(mount.ContainerPath),
                    mount.ContainerPath,
                    $"Missing bind mount for container path '{mount.ContainerPath}'."));
                errors.Add($"Missing bind mount for container path '{mount.ContainerPath}'.");
                continue;
            }

            var selected = matching[0];
            if (!string.Equals(selected.HostToken, mount.HostToken, StringComparison.Ordinal))
            {
                checks.Add(ComposeMountValidationCheck.Fail(
                    ResolveLabel(mount.ContainerPath),
                    mount.ContainerPath,
                    $"Host token mismatch for '{mount.ContainerPath}'. Expected '{mount.HostToken}', found '{selected.HostToken}'."));
                errors.Add($"Host token mismatch for '{mount.ContainerPath}'.");
                continue;
            }

            if (mount.ReadOnly && !selected.ReadOnly)
            {
                checks.Add(ComposeMountValidationCheck.Fail(
                    ResolveLabel(mount.ContainerPath),
                    mount.ContainerPath,
                    $"Mount '{mount.ContainerPath}' must be read-only."));
                errors.Add($"Mount '{mount.ContainerPath}' must be read-only.");
                continue;
            }

            if (!mount.ReadOnly && selected.ReadOnly)
            {
                checks.Add(ComposeMountValidationCheck.Fail(
                    ResolveLabel(mount.ContainerPath),
                    mount.ContainerPath,
                    $"Mount '{mount.ContainerPath}' must be writable."));
                errors.Add($"Mount '{mount.ContainerPath}' must be writable.");
                continue;
            }

            checks.Add(ComposeMountValidationCheck.Pass(
                ResolveLabel(mount.ContainerPath),
                mount.ContainerPath,
                mount.ReadOnly ? "ReadOnly" : "ReadWrite"));
        }

        if (filesystem is not null)
        {
            foreach (var directory in filesystem.Directories)
            {
                if (IsCoveredByWritableMount(directory.Path, parsed))
                {
                    checks.Add(ComposeMountValidationCheck.Pass(
                        ResolveLabel(directory.Path),
                        directory.Path,
                        "Writable"));
                }
                else
                {
                    checks.Add(ComposeMountValidationCheck.Fail(
                        ResolveLabel(directory.Path),
                        directory.Path,
                        $"Filesystem directory '{directory.Path}' is not covered by a writable bind mount."));
                    errors.Add($"Filesystem directory '{directory.Path}' is not covered by a writable bind mount.");
                }
            }

            foreach (var directory in filesystem.ReadOnlyDirectories)
            {
                if (IsCoveredByReadOnlyMount(directory.Path, parsed))
                {
                    checks.Add(ComposeMountValidationCheck.Pass(
                        ResolveLabel(directory.Path),
                        directory.Path,
                        "ReadOnly"));
                }
                else
                {
                    checks.Add(ComposeMountValidationCheck.Fail(
                        ResolveLabel(directory.Path),
                        directory.Path,
                        $"Read-only filesystem directory '{directory.Path}' is not covered by a read-only bind mount."));
                    errors.Add($"Read-only filesystem directory '{directory.Path}' is not covered by a read-only bind mount.");
                }
            }
        }

        return new ComposeMountValidationReport
        {
            Passed = errors.Count == 0,
            Checks = checks,
            Errors = errors,
            ParsedMounts = parsed
        };
    }

    public static IReadOnlyList<ParsedComposeMount> ParseComposeVolumes(string composeContent)
    {
        var mounts = new List<ParsedComposeMount>();
        var inVolumes = false;
        var volumesIndent = -1;

        foreach (var rawLine in composeContent.Split('\n'))
        {
            var line = rawLine.TrimEnd('\r');
            if (line.TrimStart().StartsWith("volumes:", StringComparison.OrdinalIgnoreCase))
            {
                inVolumes = true;
                volumesIndent = line.TakeWhile(char.IsWhiteSpace).Count();
                continue;
            }

            if (!inVolumes)
            {
                continue;
            }

            var indent = line.TakeWhile(char.IsWhiteSpace).Count();
            var trimmed = line.Trim();
            if (line.Length > 0 && indent <= volumesIndent && !trimmed.StartsWith('-'))
            {
                break;
            }

            if (!trimmed.StartsWith('-'))
            {
                continue;
            }

            var entry = trimmed[1..].Trim();
            var parsed = ParseVolumeEntry(entry);
            if (parsed is not null)
            {
                mounts.Add(parsed);
            }
        }

        return mounts;
    }

    public static ParsedComposeMount? ParseVolumeEntry(string rawEntry)
    {
        if (string.IsNullOrWhiteSpace(rawEntry))
        {
            return null;
        }

        var entry = rawEntry.Trim().Trim('"', '\'');
        var readOnly = false;
        if (entry.EndsWith(":ro", StringComparison.OrdinalIgnoreCase))
        {
            readOnly = true;
            entry = entry[..^3];
        }
        else if (entry.EndsWith(":rw", StringComparison.OrdinalIgnoreCase))
        {
            entry = entry[..^3];
        }

        var separator = entry.IndexOf(':');
        if (separator <= 0)
        {
            return null;
        }

        var hostToken = entry[..separator].Trim();
        var containerPath = entry[(separator + 1)..].Trim();
        if (!containerPath.StartsWith("/", StringComparison.Ordinal))
        {
            return null;
        }

        return new ParsedComposeMount
        {
            HostToken = hostToken,
            ContainerPath = containerPath,
            ReadOnly = readOnly
        };
    }

    private static IEnumerable<string> DetectStructuralIssues(IReadOnlyList<ParsedComposeMount> parsed)
    {
        var errors = new List<string>();
        var byContainer = parsed.GroupBy(item => item.ContainerPath, StringComparer.Ordinal);
        foreach (var group in byContainer.Where(item => item.Count() > 1))
        {
            errors.Add($"Duplicate bind mount for container path '{group.Key}'.");
        }

        foreach (var group in byContainer)
        {
            var hostTokens = group.Select(item => item.HostToken).Distinct(StringComparer.Ordinal).ToList();
            if (hostTokens.Count > 1)
            {
                errors.Add($"Conflicting host tokens for container path '{group.Key}': {string.Join(", ", hostTokens)}.");
            }

            var modes = group.Select(item => item.ReadOnly).Distinct().ToList();
            if (modes.Count > 1)
            {
                errors.Add($"Conflicting read/write modes for container path '{group.Key}'.");
            }
        }

        return errors;
    }

    private static bool IsCoveredByWritableMount(string containerPath, IReadOnlyList<ParsedComposeMount> parsed)
        => parsed.Any(mount => !mount.ReadOnly && IsSameOrChildPath(containerPath, mount.ContainerPath));

    private static bool IsCoveredByReadOnlyMount(string containerPath, IReadOnlyList<ParsedComposeMount> parsed)
        => parsed.Any(mount => mount.ReadOnly && IsSameOrChildPath(containerPath, mount.ContainerPath));

    private static bool IsSameOrChildPath(string path, string mountRoot)
    {
        if (string.Equals(path, mountRoot, StringComparison.Ordinal))
        {
            return true;
        }

        return path.StartsWith(mountRoot.TrimEnd('/') + "/", StringComparison.Ordinal);
    }

    private static string ResolveLabel(string containerPath)
    {
        foreach (var (label, path) in PlatformDeploymentDefaults.RequiredWritableResources)
        {
            if (string.Equals(path, containerPath, StringComparison.Ordinal))
            {
                return label;
            }
        }

        foreach (var (label, path, _) in PlatformDeploymentDefaults.RequiredRuntimeResources)
        {
            if (string.Equals(path, containerPath, StringComparison.Ordinal))
            {
                return label;
            }
        }

        if (string.Equals(containerPath, PlatformRuntimeDefaults.CertificatesRoot, StringComparison.Ordinal))
        {
            return "Certificates";
        }

        return containerPath;
    }
}

public sealed class ParsedComposeMount
{
    public required string HostToken { get; init; }

    public required string ContainerPath { get; init; }

    public bool ReadOnly { get; init; }
}

public sealed class ComposeMountValidationCheck
{
    public required string Label { get; init; }

    public required string ContainerPath { get; init; }

    public required string Status { get; init; }

    public string? Detail { get; init; }

    public static ComposeMountValidationCheck Pass(string label, string containerPath, string detail)
        => new() { Label = label, ContainerPath = containerPath, Status = "PASS", Detail = detail };

    public static ComposeMountValidationCheck Fail(string label, string containerPath, string detail)
        => new() { Label = label, ContainerPath = containerPath, Status = "FAIL", Detail = detail };
}

public sealed class ComposeMountValidationReport
{
    public bool Passed { get; init; }

    public IReadOnlyList<ComposeMountValidationCheck> Checks { get; init; } = [];

    public IReadOnlyList<string> Errors { get; init; } = [];

    public IReadOnlyList<ParsedComposeMount> ParsedMounts { get; init; } = [];
}
