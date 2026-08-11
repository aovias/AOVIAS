using Aovias.Bootstrap.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Aovias.Bootstrap.Core;

/// <summary>
/// Registers the bootstrap orchestrator. Deliberately does not register
/// <see cref="IBootstrapTargetExecutor"/> or <see cref="IBootstrapRegistrationClient"/> — those are
/// real-I/O providers (SSH, HTTP) that only make sense in a host that has a concrete target and
/// concrete network access, so <c>Aovias.Bootstrap.Host</c> registers those itself.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddBootstrapCore(this IServiceCollection services, string workingDirectory)
    {
        services.AddSingleton<IBootstrapStateStore>(new BootstrapStateStore(workingDirectory));
        services.AddSingleton<IBootstrapOrchestrator, BootstrapOrchestrator>();
        return services;
    }
}
