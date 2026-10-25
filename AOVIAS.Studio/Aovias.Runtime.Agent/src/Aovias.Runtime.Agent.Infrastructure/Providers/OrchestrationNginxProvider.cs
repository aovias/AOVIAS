using System.Diagnostics;
using System.Globalization;
using Aovias.Configuration.Settings;
using Aovias.Orchestration.Contracts.Providers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aovias.Runtime.Agent.Infrastructure.Providers;

/// <summary>
/// Renders and applies nginx site configuration for the runtime agent.
/// </summary>
public sealed class OrchestrationNginxProvider : INginxProvider
{
    private const string SiteFileName = "aovias-runtime-agent.conf";
    private const string BackupFileName = "aovias-runtime-agent.previous.conf";

    private readonly IOptionsMonitor<NginxSettings> nginxSettings;
    private readonly IOptionsMonitor<CertificateSettings> certificateSettings;
    private readonly IOptionsMonitor<ReleaseManagementSettings> releaseSettings;
    private readonly IOptionsMonitor<ProviderSettings> providerSettings;
    private readonly ILogger<OrchestrationNginxProvider> logger;

    public OrchestrationNginxProvider(
        IOptionsMonitor<NginxSettings> nginxSettings,
        IOptionsMonitor<CertificateSettings> certificateSettings,
        IOptionsMonitor<ReleaseManagementSettings> releaseSettings,
        IOptionsMonitor<ProviderSettings> providerSettings,
        ILogger<OrchestrationNginxProvider> logger)
    {
        this.nginxSettings = nginxSettings;
        this.certificateSettings = certificateSettings;
        this.releaseSettings = releaseSettings;
        this.providerSettings = providerSettings;
        this.logger = logger;
    }

    /// <inheritdoc />
    public string ProviderName => "Nginx";

    /// <inheritdoc />
    public bool IsAvailable => providerSettings.CurrentValue.NginxEnabled && nginxSettings.CurrentValue.Enabled;

    /// <inheritdoc />
    public Task<string> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!IsAvailable)
        {
            return Task.FromResult("Disabled");
        }

        var sitePath = GetSitePath();
        return Task.FromResult(File.Exists(sitePath) ? "Configured" : "NotInstalled");
    }

    /// <inheritdoc />
    public async Task<NginxOperationResult> ConfigureRuntimeSiteAsync(CancellationToken cancellationToken = default)
    {
        if (!IsAvailable)
        {
            return NginxOperationResult.Success("Nginx integration is disabled; configuration skipped.");
        }

        var settings = nginxSettings.CurrentValue;
        if (string.IsNullOrWhiteSpace(settings.SiteTemplatePath) || !File.Exists(settings.SiteTemplatePath))
        {
            return NginxOperationResult.Failure($"Nginx site template was not found: {settings.SiteTemplatePath}");
        }

        Directory.CreateDirectory(settings.ConfigRoot);
        var sitePath = GetSitePath();
        await BackupCurrentSiteAsync(sitePath, cancellationToken);

        var template = await File.ReadAllTextAsync(settings.SiteTemplatePath, cancellationToken);
        var rendered = RenderTemplate(template, settings);
        await File.WriteAllTextAsync(sitePath, rendered, cancellationToken);

        var reload = await ReloadAsync(cancellationToken);
        return reload.IsSuccess
            ? NginxOperationResult.Success($"Nginx site configured at {sitePath}.", sitePath)
            : reload;
    }

    /// <inheritdoc />
    public async Task<NginxOperationResult> RestorePreviousSiteAsync(CancellationToken cancellationToken = default)
    {
        if (!IsAvailable)
        {
            return NginxOperationResult.Success("Nginx integration is disabled; restore skipped.");
        }

        var backupPath = GetBackupPath();
        if (!File.Exists(backupPath))
        {
            return NginxOperationResult.Failure("No previous nginx configuration backup was found.");
        }

        var sitePath = GetSitePath();
        Directory.CreateDirectory(Path.GetDirectoryName(sitePath)!);
        File.Copy(backupPath, sitePath, overwrite: true);

        var reload = await ReloadAsync(cancellationToken);
        return reload.IsSuccess
            ? NginxOperationResult.Success($"Previous nginx configuration restored to {sitePath}.", sitePath)
            : reload;
    }

    /// <inheritdoc />
    public async Task<NginxOperationResult> ReloadAsync(CancellationToken cancellationToken = default)
    {
        if (!IsAvailable)
        {
            return NginxOperationResult.Success("Nginx integration is disabled; reload skipped.");
        }

        var settings = nginxSettings.CurrentValue;
        var startInfo = new ProcessStartInfo
        {
            FileName = settings.Executable,
            Arguments = settings.ReloadCommand,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo);
        if (process is null)
        {
            return NginxOperationResult.Failure($"Unable to start nginx executable '{settings.Executable}'.");
        }

        var stdout = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderr = await process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode != 0)
        {
            logger.LogError("Nginx reload failed: {StdErr}", stderr);
            return NginxOperationResult.Failure(string.IsNullOrWhiteSpace(stderr) ? stdout : stderr);
        }

        return NginxOperationResult.Success("Nginx reloaded successfully.");
    }

    private async Task BackupCurrentSiteAsync(string sitePath, CancellationToken cancellationToken)
    {
        if (!File.Exists(sitePath))
        {
            return;
        }

        var backupPath = GetBackupPath();
        Directory.CreateDirectory(Path.GetDirectoryName(backupPath)!);
        File.Copy(sitePath, backupPath, overwrite: true);
        await Task.CompletedTask;
    }

    private string RenderTemplate(string template, NginxSettings settings)
    {
        var certificate = certificateSettings.CurrentValue;
        return template
            .Replace("{{SERVER_NAME}}", settings.ServerName, StringComparison.Ordinal)
            .Replace("{{SSL_CERTIFICATE_PATH}}", certificate.CertificatePath, StringComparison.Ordinal)
            .Replace("{{SSL_CERTIFICATE_KEY_PATH}}", certificate.StoreLocation, StringComparison.Ordinal)
            .Replace("{{PUBLIC_PATH_PREFIX}}", settings.PublicPathPrefix.TrimEnd('/'), StringComparison.Ordinal)
            .Replace("{{UPSTREAM_HOST}}", settings.UpstreamHost, StringComparison.Ordinal)
            .Replace("{{UPSTREAM_PORT}}", settings.UpstreamPort.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("{{UPLOAD_BODY_LIMIT_MB}}", settings.UploadBodyLimitMegabytes.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("{{UPLOAD_TIMEOUT_SECONDS}}", settings.UploadTimeoutSeconds.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }

    private string GetSitePath()
        => Path.Combine(nginxSettings.CurrentValue.ConfigRoot, SiteFileName);

    private string GetBackupPath()
        => Path.Combine(releaseSettings.CurrentValue.RollbackFolder, "nginx", BackupFileName);
}
