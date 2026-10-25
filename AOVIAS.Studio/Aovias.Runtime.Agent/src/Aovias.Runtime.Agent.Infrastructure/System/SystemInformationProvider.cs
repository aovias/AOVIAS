using Aovias.Runtime.Agent.Application.Interfaces;

namespace Aovias.Runtime.Agent.Infrastructure.System;

/// <summary>
/// Reads live system information from the host operating system.
/// </summary>
public sealed class SystemInformationProvider : ISystemInformationProvider
{
    /// <inheritdoc />
    public Task<string> GetOperatingSystemAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(HostSystemMetricsReader.Read().OperatingSystem);
    }

    /// <inheritdoc />
    public Task<string> GetKernelAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(HostSystemMetricsReader.Read().Kernel);
    }

    /// <inheritdoc />
    public Task<SystemMetricsSnapshot> GetSystemMetricsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(HostSystemMetricsReader.Read());
    }
}
