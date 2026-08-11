using System.Net.Http;
using Aovias.Configuration.Settings;
using Aovias.Release.Contracts.RemoteDeployment;
using Aovias.Release.SDK;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ComplianceServiceManager.Services.Deployment;

public sealed record RemoteAgentClients(
    IDeploymentClient Deployment,
    IReleaseClient Release);

public interface IRemoteAgentClientFactory
{
    RemoteAgentClients Create(RemoteEnvironment environment);
}

/// <summary>
/// Creates HTTP clients bound to the operator-selected Runtime Agent. This prevents
/// deployments from silently using the global Studio API base URL.
/// </summary>
public sealed class RemoteAgentClientFactory : IRemoteAgentClientFactory
{
    private const string ClientName = "SelectedRuntimeAgent";

    private readonly IHttpClientFactory httpClientFactory;
    private readonly IOptionsMonitor<RemoteDeploymentSettings> deploymentSettings;
    private readonly IOptionsMonitor<ReleaseManagementSettings> releaseSettings;
    private readonly IOptionsMonitor<SecuritySettings> securitySettings;
    private readonly ILoggerFactory loggerFactory;
    private readonly IReleaseClientTraceHook? traceHook;

    public RemoteAgentClientFactory(
        IHttpClientFactory httpClientFactory,
        IOptionsMonitor<RemoteDeploymentSettings> deploymentSettings,
        IOptionsMonitor<ReleaseManagementSettings> releaseSettings,
        IOptionsMonitor<SecuritySettings> securitySettings,
        ILoggerFactory loggerFactory,
        IReleaseClientTraceHook? traceHook = null)
    {
        this.httpClientFactory = httpClientFactory;
        this.deploymentSettings = deploymentSettings;
        this.releaseSettings = releaseSettings;
        this.securitySettings = securitySettings;
        this.loggerFactory = loggerFactory;
        this.traceHook = traceHook;
    }

    public RemoteAgentClients Create(RemoteEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(environment);
        if (!Uri.TryCreate(environment.BaseUrl, UriKind.Absolute, out var baseAddress))
        {
            throw new InvalidOperationException(
                $"Runtime '{environment.Name}' has invalid BaseUrl '{environment.BaseUrl}'.");
        }

        if (baseAddress.Host.EndsWith(".aovias.local", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Runtime '{environment.Name}' uses placeholder endpoint '{environment.BaseUrl}'. " +
                "Register the Runtime Agent or update the environment BaseUrl before deployment.");
        }

        var deploymentHttp = CreateHttpClient(
            baseAddress,
            Math.Max(
                deploymentSettings.CurrentValue.TimeoutSeconds,
                deploymentSettings.CurrentValue.LargeUploadTimeoutSeconds),
            environment);
        var releaseHttp = CreateHttpClient(
            baseAddress,
            Math.Max(
                releaseSettings.CurrentValue.TimeoutSeconds,
                deploymentSettings.CurrentValue.LargeUploadTimeoutSeconds),
            environment);

        return new RemoteAgentClients(
            new DeploymentClient(
                deploymentHttp,
                deploymentSettings,
                loggerFactory.CreateLogger<DeploymentClient>()),
            new ReleaseClient(
                releaseHttp,
                releaseSettings,
                loggerFactory.CreateLogger<ReleaseClient>(),
                traceHook));
    }

    private HttpClient CreateHttpClient(
        Uri baseAddress,
        int timeoutSeconds,
        RemoteEnvironment environment)
    {
        var client = httpClientFactory.CreateClient(ClientName);
        client.BaseAddress = baseAddress;
        client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);

        var apiKey = string.IsNullOrWhiteSpace(environment.ApiKey)
            ? securitySettings.CurrentValue.ApiKey
            : environment.ApiKey;
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            client.DefaultRequestHeaders.Remove("X-Api-Key");
            client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
        }

        return client;
    }
}
