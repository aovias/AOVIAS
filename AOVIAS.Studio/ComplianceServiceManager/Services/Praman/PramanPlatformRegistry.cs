using System.IO;
using System.Text.Json;
using Aovias.Configuration.Settings;
using ComplianceServiceManager.Contracts.Praman;
using Microsoft.Extensions.Options;

namespace ComplianceServiceManager.Services.Praman;

/// <summary>
/// Reads PRAMAN platform service registration from the active AOVIAS profile.
/// </summary>
public sealed class PramanPlatformRegistry : IPramanPlatformRegistry
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IOptionsMonitor<AoviasPlatformOptions> platformOptions;

    public PramanPlatformRegistry(IOptionsMonitor<AoviasPlatformOptions> platformOptions)
    {
        this.platformOptions = platformOptions;
    }

    /// <inheritdoc />
    public Task<PramanPlatformRegistryDocument> LoadAsync(CancellationToken cancellationToken = default)
    {
        var configRoot = platformOptions.CurrentValue.ConfigRoot;
        var profile = platformOptions.CurrentValue.ActiveProfile;
        var path = Path.Combine(configRoot, "profiles", profile, "praman-platform.json");

        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"PRAMAN platform registry not found at {path}.");
        }

        var json = File.ReadAllText(path);
        var document = JsonSerializer.Deserialize<PramanPlatformRegistryDocument>(json, JsonOptions)
            ?? throw new InvalidOperationException("PRAMAN platform registry is invalid.");

        return Task.FromResult(document);
    }
}
