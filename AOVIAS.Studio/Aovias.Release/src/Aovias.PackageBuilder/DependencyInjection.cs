using Aovias.Publishing.Contracts;
using Aovias.Publishing.Core.Packaging;
using Aovias.PackageBuilder.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Aovias.PackageBuilder;

/// <summary>
/// Package builder dependency injection extensions.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers package builder services.
    /// </summary>
    public static IServiceCollection AddPackageBuilder(this IServiceCollection services)
    {
        services.AddSingleton<IDeployPackageReader, DeployPackageReader>();
        services.AddSingleton<IDeployPackageValidator, DeployPackageValidator>();
        services.AddSingleton<Abstractions.IPackageIdentityGenerator, PackageIdentityGenerator>();
        services.AddSingleton<Abstractions.IChecksumProvider, Sha256ChecksumProvider>();
        services.AddSingleton<Abstractions.IManifestGenerator, ManifestGenerator>();
        services.AddSingleton<Abstractions.IPackageReader, UnifiedPackageReader>();
        services.AddSingleton<Abstractions.IPackageSigner, DigitalSignatureProvider>();
        services.AddSingleton<Abstractions.IPackageValidator, PackageValidator>();
        services.AddSingleton<Abstractions.IPackageBuilder, PackageBuilderService>();
        return services;
    }
}
