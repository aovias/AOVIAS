using Aovias.Configuration.Settings;
using Aovias.Orchestration.Contracts.Providers;
using Microsoft.Extensions.Options;

namespace Aovias.Runtime.Agent.Infrastructure.Providers;

/// <summary>
/// File system provider for orchestration workflows.
/// </summary>
public sealed class OrchestrationFileSystemProvider : IFileSystemProvider
{
    private readonly IOptionsMonitor<ProviderSettings> providerSettings;

    public OrchestrationFileSystemProvider(IOptionsMonitor<ProviderSettings> providerSettings)
    {
        this.providerSettings = providerSettings;
    }

    /// <inheritdoc />
    public string ProviderName => "FileSystem";

    /// <inheritdoc />
    public bool IsAvailable => providerSettings.CurrentValue.FileSystemEnabled;

    /// <inheritdoc />
    public Task<bool> DirectoryExistsAsync(string path, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(Directory.Exists(path));
    }

    /// <inheritdoc />
    public Task CreateDirectoryAsync(string path, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Directory.CreateDirectory(path);
        return Task.CompletedTask;
    }
}
