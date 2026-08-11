using Aovias.Configuration.Settings;
using Aovias.Runtime.Agent.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aovias.Runtime.Agent.Infrastructure.Persistence;

/// <summary>
/// Reads the runtime manifest from the configured host path when available.
/// </summary>
public sealed class RuntimeManifestStore
{
    private static readonly global::System.Text.Json.JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = global::System.Text.Json.JsonNamingPolicy.CamelCase
    };

    private readonly IOptionsMonitor<RuntimeManifestSettings> manifestSettings;
    private readonly ILogger<RuntimeManifestStore> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RuntimeManifestStore"/> class.
    /// </summary>
    /// <param name="manifestSettings">Manifest path settings.</param>
    /// <param name="logger">The logger instance.</param>
    public RuntimeManifestStore(IOptionsMonitor<RuntimeManifestSettings> manifestSettings, ILogger<RuntimeManifestStore> logger)
    {
        this.manifestSettings = manifestSettings;
        this.logger = logger;
    }

    /// <summary>
    /// Attempts to load the runtime manifest from disk.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>The manifest entity when found; otherwise <see langword="null"/>.</returns>
    public async Task<RuntimeManifestEntity?> TryLoadAsync(CancellationToken cancellationToken = default)
    {
        var manifestPath = manifestSettings.CurrentValue.ManifestPath;
        if (string.IsNullOrWhiteSpace(manifestPath) || !File.Exists(manifestPath))
        {
            logger.LogDebug("Runtime manifest not found at {ManifestPath}", manifestPath);
            return null;
        }

        await using var stream = File.OpenRead(manifestPath);
        var manifest = await global::System.Text.Json.JsonSerializer.DeserializeAsync<RuntimeManifestEntity>(stream, SerializerOptions, cancellationToken);
        return manifest;
    }

    /// <summary>
    /// Persists the runtime manifest using an atomic write.
    /// </summary>
    /// <param name="manifest">The manifest entity to save.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    public async Task SaveAsync(RuntimeManifestEntity manifest, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(manifest);

        var manifestPath = manifestSettings.CurrentValue.ManifestPath;
        if (string.IsNullOrWhiteSpace(manifestPath))
        {
            throw new InvalidOperationException("Runtime manifest path is not configured.");
        }

        manifest.LastUpdated = DateTimeOffset.UtcNow;
        Directory.CreateDirectory(Path.GetDirectoryName(manifestPath)!);

        var tempPath = $"{manifestPath}.{Guid.NewGuid():N}.tmp";
        await File.WriteAllTextAsync(tempPath, global::System.Text.Json.JsonSerializer.Serialize(manifest, SerializerOptions), cancellationToken);
        File.Move(tempPath, manifestPath, overwrite: true);
        logger.LogInformation("Runtime manifest saved to {ManifestPath}", manifestPath);
    }
}
