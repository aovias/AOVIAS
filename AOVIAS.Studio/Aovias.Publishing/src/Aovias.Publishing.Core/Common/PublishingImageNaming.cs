using Aovias.Publishing.Contracts;

namespace Aovias.Publishing.Core.Common;

/// <summary>
/// Canonical Docker image and package naming for AOVIAS deployment artifacts.
/// </summary>
public static class PublishingImageNaming
{
    public const string RegistryPrefix = "aovias";

    public static string GetImageName(string serviceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceId);
        return $"{RegistryPrefix}/{serviceId.ToLowerInvariant()}";
    }

    public static string GetPackageFileName(string serviceId)
        => $"{serviceId.ToLowerInvariant()}{DeployPackageLayout.Extension}";

    public static string GetPackageFileName(string serviceId, string version)
        => $"{serviceId.ToLowerInvariant()}-{version}{DeployPackageLayout.Extension}";
}
