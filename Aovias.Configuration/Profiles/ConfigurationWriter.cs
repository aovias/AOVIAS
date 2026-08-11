using System.Text.Json;
using System.Text.Json.Serialization;
using Aovias.Configuration.Settings;
using Microsoft.Extensions.Options;

namespace Aovias.Configuration.Profiles;

/// <summary>
/// Persists strongly typed settings back to profile JSON files.
/// </summary>
public interface IConfigurationWriter
{
    /// <summary>
    /// Saves runtime settings for the active profile.
    /// </summary>
    Task SaveRuntimeSettingsAsync(RuntimeSettings settings, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves infrastructure settings for the active profile.
    /// </summary>
    Task SaveInfrastructureSettingsAsync(InfrastructureSettings settings, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves docker settings for the active profile.
    /// </summary>
    Task SaveDockerSettingsAsync(DockerSettings settings, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves API settings for the active profile.
    /// </summary>
    Task SaveApiSettingsAsync(ApiSettings settings, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves security settings for the active profile.
    /// </summary>
    Task SaveSecuritySettingsAsync(SecuritySettings settings, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves logging settings for the active profile.
    /// </summary>
    Task SaveLoggingSettingsAsync(LoggingSettings settings, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves certificate settings for the active profile.
    /// </summary>
    Task SaveCertificateSettingsAsync(CertificateSettings settings, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves deployment settings for the active profile.
    /// </summary>
    Task SaveDeploymentSettingsAsync(DeploymentSettings settings, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves monitoring settings for the active profile.
    /// </summary>
    Task SaveMonitoringSettingsAsync(MonitoringSettings settings, CancellationToken cancellationToken = default);

    Task SaveOrchestrationSettingsAsync(OrchestrationSettings settings, CancellationToken cancellationToken = default);

    Task SaveSignalRSettingsAsync(SignalRSettings settings, CancellationToken cancellationToken = default);

    Task SaveProviderSettingsAsync(ProviderSettings settings, CancellationToken cancellationToken = default);
}

/// <summary>
/// Writes profile JSON files using camelCase serialization.
/// </summary>
public sealed class ConfigurationWriter : IConfigurationWriter
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly IOptionsMonitor<AoviasPlatformOptions> platformOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationWriter"/> class.
    /// </summary>
    /// <param name="platformOptions">Platform options monitor.</param>
    public ConfigurationWriter(IOptionsMonitor<AoviasPlatformOptions> platformOptions)
    {
        this.platformOptions = platformOptions;
    }

    /// <inheritdoc />
    public Task SaveRuntimeSettingsAsync(RuntimeSettings settings, CancellationToken cancellationToken = default)
        => SaveAsync("settings.runtime.json", settings, cancellationToken);

    /// <inheritdoc />
    public Task SaveInfrastructureSettingsAsync(InfrastructureSettings settings, CancellationToken cancellationToken = default)
        => SaveAsync("settings.infrastructure.json", settings, cancellationToken);

    /// <inheritdoc />
    public Task SaveDockerSettingsAsync(DockerSettings settings, CancellationToken cancellationToken = default)
        => SaveAsync("settings.docker.json", settings, cancellationToken);

    /// <inheritdoc />
    public Task SaveApiSettingsAsync(ApiSettings settings, CancellationToken cancellationToken = default)
        => SaveAsync("settings.api.json", settings, cancellationToken);

    /// <inheritdoc />
    public Task SaveSecuritySettingsAsync(SecuritySettings settings, CancellationToken cancellationToken = default)
        => SaveAsync("settings.security.json", settings, cancellationToken);

    /// <inheritdoc />
    public Task SaveLoggingSettingsAsync(LoggingSettings settings, CancellationToken cancellationToken = default)
        => SaveAsync("settings.logging.json", settings, cancellationToken);

    /// <inheritdoc />
    public Task SaveCertificateSettingsAsync(CertificateSettings settings, CancellationToken cancellationToken = default)
        => SaveAsync("settings.certificate.json", settings, cancellationToken);

    /// <inheritdoc />
    public Task SaveDeploymentSettingsAsync(DeploymentSettings settings, CancellationToken cancellationToken = default)
        => SaveAsync("settings.deployment.json", settings, cancellationToken);

    /// <inheritdoc />
    public Task SaveMonitoringSettingsAsync(MonitoringSettings settings, CancellationToken cancellationToken = default)
        => SaveAsync("settings.monitoring.json", settings, cancellationToken);

    /// <inheritdoc />
    public Task SaveOrchestrationSettingsAsync(OrchestrationSettings settings, CancellationToken cancellationToken = default)
        => SaveAsync("settings.orchestration.json", settings, cancellationToken);

    /// <inheritdoc />
    public Task SaveSignalRSettingsAsync(SignalRSettings settings, CancellationToken cancellationToken = default)
        => SaveAsync("settings.signalr.json", settings, cancellationToken);

    /// <inheritdoc />
    public Task SaveProviderSettingsAsync(ProviderSettings settings, CancellationToken cancellationToken = default)
        => SaveAsync("settings.providers.json", settings, cancellationToken);

    private async Task SaveAsync<T>(string fileName, T payload, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var platform = platformOptions.CurrentValue;
        var profileDirectory = Path.Combine(platform.ConfigRoot, platform.ProfilesDirectory, platform.ActiveProfile);
        Directory.CreateDirectory(profileDirectory);

        var filePath = Path.Combine(profileDirectory, fileName);
        await using var stream = File.Create(filePath);
        await JsonSerializer.SerializeAsync(stream, payload, SerializerOptions, cancellationToken);
    }
}
