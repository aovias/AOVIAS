using Microsoft.Extensions.DependencyInjection;
using Platform.Dashboard.Contracts;
using Platform.Dashboard.Registry;
using Platform.Dashboard.Services;

namespace Platform.Dashboard.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers platform dashboard coordination services. Host applications register
    /// <see cref="IDashboardProvider"/> implementations separately.
    /// </summary>
    public static IServiceCollection AddPlatformDashboard(this IServiceCollection services)
    {
        services.AddScoped<IDashboardProviderRegistry, DashboardProviderRegistry>();
        services.AddScoped<IDashboardSummaryService, DashboardSummaryService>();
        return services;
    }
}
