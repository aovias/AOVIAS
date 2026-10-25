using Aovias.PackageBuilder;
using Aovias.ReleaseManager.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Aovias.ReleaseManager;

/// <summary>
/// Release manager dependency injection extensions.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers release management services.
    /// </summary>
    public static IServiceCollection AddReleaseManager(this IServiceCollection services)
    {
        services.AddPackageBuilder();
        services.AddSingleton<Abstractions.IReleaseRepository, FileSystemReleaseRepository>();
        services.AddSingleton<FileSystemReleaseAuditStore>();
        services.AddSingleton<Abstractions.IReleaseAuditStore>(provider => provider.GetRequiredService<FileSystemReleaseAuditStore>());
        services.AddSingleton<Abstractions.IReleaseStateMachine, ReleaseStateMachine>();
        services.AddSingleton<Abstractions.IReleaseIdentityGenerator, ReleaseIdentityGenerator>();
        services.AddSingleton<Abstractions.IReleaseValidator, ReleaseValidator>();
        services.AddSingleton<Abstractions.IReleasePlanner, ReleasePlanner>();
        services.AddSingleton<Abstractions.IReleaseRollbackService, ReleaseRollbackService>();
        services.AddSingleton<Abstractions.IReleaseHistoryProvider, ReleaseHistoryProvider>();
        services.AddSingleton<ReleaseManagerService>();
        services.AddSingleton<Abstractions.IReleaseManager>(provider => provider.GetRequiredService<ReleaseManagerService>());
        return services;
    }
}
