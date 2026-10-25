using Microsoft.Extensions.Hosting;

namespace Shared.Infrastructure.Development;

/// <summary>
/// Guards development-only bootstrap and seeding features from running in production.
/// </summary>
public static class NonProductionEnvironment
{
    public static bool AllowDevelopmentSeeding(IHostEnvironment environment)
    {
        if (environment.IsProduction())
            return false;

        if (environment.IsDevelopment())
            return true;

        var name = environment.EnvironmentName;
        return name.Contains("QA", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Staging", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Test", StringComparison.OrdinalIgnoreCase);
    }
}
