namespace Aovias.Deployment.Contracts.Provisioning;

/// <summary>
/// Validates host paths used by deployment are writable before containers start.
/// Prevents the recurring log permission denied runtime failure.
/// </summary>
public static class HostPathWriteAccessValidator
{
    public static IReadOnlyList<string> ValidateWritable(params string?[] paths)
    {
        var errors = new List<string>();
        foreach (var path in paths.Where(item => !string.IsNullOrWhiteSpace(item)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var error = ValidateSingleWritablePath(path!);
            if (error is not null)
            {
                errors.Add(error);
            }
        }

        return errors;
    }

    public static IReadOnlyList<string> ValidateReadable(params string?[] paths)
    {
        var errors = new List<string>();
        foreach (var path in paths.Where(item => !string.IsNullOrWhiteSpace(item)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var error = ValidateSingleReadablePath(path!);
            if (error is not null)
            {
                errors.Add(error);
            }
        }

        return errors;
    }

    public static string? ValidateSingleWritablePath(string path)
    {
        try
        {
            if (File.Exists(path) && !Directory.Exists(path))
            {
                return $"Permission denied writing to host path '{path}'. Ensure logs and package directories are writable folders.";
            }

            Directory.CreateDirectory(path);
            var probe = Path.Combine(path, $".aovias-write-probe-{Guid.NewGuid():N}");
            File.WriteAllText(probe, "probe");
            File.Delete(probe);
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return $"Permission denied writing to host path '{path}'. Ensure the Runtime Agent process can write to logs and package directories.";
        }
        catch (IOException exception) when (exception.Message.Contains("denied", StringComparison.OrdinalIgnoreCase))
        {
            return $"Permission denied writing to host path '{path}': {exception.Message}";
        }
        catch (Exception exception)
        {
            return $"Host path '{path}' is not writable: {exception.Message}";
        }
    }

    public static string? ValidateSingleReadablePath(string path)
    {
        try
        {
            if (!Directory.Exists(path))
            {
                return $"Read-only host path '{path}' does not exist.";
            }

            _ = Directory.EnumerateFileSystemEntries(path).Take(1).ToList();
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return $"Permission denied reading host path '{path}'. Ensure the runtime configuration mount is accessible.";
        }
        catch (Exception exception)
        {
            return $"Host path '{path}' is not readable: {exception.Message}";
        }
    }
}
