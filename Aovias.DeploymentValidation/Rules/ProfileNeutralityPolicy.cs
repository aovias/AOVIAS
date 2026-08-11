using Aovias.Configuration.Deployment;
using Aovias.Publishing.Contracts;

namespace Aovias.DeploymentValidation.Rules;

/// <summary>
/// Determines profile-aware environment-neutrality allowances. A package is
/// permitted to carry the host-root convention of the profile it was built for
/// (for example Windows drive paths for the local profile), while developer and
/// machine identifiers are forbidden for every profile.
/// </summary>
public static class ProfileNeutralityPolicy
{
    /// <summary>
    /// Decides whether the package may legitimately contain Windows host paths,
    /// based on the authoritative definition of the profile it declares.
    /// </summary>
    public static bool AllowsWindowsHostPaths(DeployPackageContents? contents, string? configRoot)
    {
        if (contents is null || string.IsNullOrWhiteSpace(configRoot))
        {
            return false;
        }

        var activeProfile = ReadActiveProfile(contents.EnvTemplateContent);
        if (string.IsNullOrWhiteSpace(activeProfile))
        {
            return false;
        }

        try
        {
            return DeploymentPathResolver.Resolve(configRoot, activeProfile).UsesWindowsHostConvention;
        }
        catch
        {
            // If the profile cannot be resolved we fail closed (no allowance).
            return false;
        }
    }

    /// <summary>Reads the declared <c>AOVIAS_ACTIVE_PROFILE</c> from .env content.</summary>
    public static string? ReadActiveProfile(string? envContent)
    {
        if (string.IsNullOrWhiteSpace(envContent))
        {
            return null;
        }

        foreach (var line in envContent.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (line.StartsWith("AOVIAS_ACTIVE_PROFILE=", StringComparison.OrdinalIgnoreCase))
            {
                return line["AOVIAS_ACTIVE_PROFILE=".Length..].Trim();
            }
        }

        return null;
    }

    /// <summary>Reads the declared <c>AOVIAS_DOCKER_NETWORK</c> from .env content.</summary>
    public static string? ReadDockerNetwork(string? envContent)
    {
        if (string.IsNullOrWhiteSpace(envContent))
        {
            return null;
        }

        foreach (var line in envContent.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (line.StartsWith("AOVIAS_DOCKER_NETWORK=", StringComparison.OrdinalIgnoreCase))
            {
                return line["AOVIAS_DOCKER_NETWORK=".Length..].Trim();
            }
        }

        return null;
    }
}
