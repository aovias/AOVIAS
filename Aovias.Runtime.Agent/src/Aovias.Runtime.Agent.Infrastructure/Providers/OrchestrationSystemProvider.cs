using Aovias.Orchestration.Contracts.Providers;
using Aovias.Runtime.Agent.Application.Interfaces;

namespace Aovias.Runtime.Agent.Infrastructure.Providers;

/// <summary>
/// System provider adapter for orchestration workflows.
/// </summary>
public sealed class OrchestrationSystemProvider : ISystemProvider
{
    private readonly IRuntimeProvider runtimeProvider;

    public OrchestrationSystemProvider(IRuntimeProvider runtimeProvider)
    {
        this.runtimeProvider = runtimeProvider;
    }

    /// <inheritdoc />
    public string ProviderName => "System";

    /// <inheritdoc />
    public bool IsAvailable => true;

    /// <inheritdoc />
    public async Task<SystemInfoSnapshot> GetSystemInfoAsync(CancellationToken cancellationToken = default)
    {
        var info = await runtimeProvider.GetRuntimeInfoAsync(cancellationToken);
        return new SystemInfoSnapshot
        {
            Hostname = info.Hostname,
            OperatingSystem = info.OperatingSystem,
            Kernel = info.Kernel
        };
    }
}
