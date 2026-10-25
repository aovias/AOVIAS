using System.IO;
using System.Text.Json;
using Aovias.Release.Contracts.RemoteDeployment;
using Aovias.Release.SDK;
using Aovias.Configuration.Settings;
using ComplianceServiceManager.Contracts.Deployment;
using Microsoft.Extensions.Options;

namespace ComplianceServiceManager.Services.Deployment;

/// <summary>
/// Persists and manages deployment environments for AOVIAS Command.
/// </summary>
public sealed class EnvironmentManagerService : IEnvironmentManager
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly IDeploymentClient deploymentClient;
    private readonly IOptionsMonitor<ApiSettings> apiSettings;
    private readonly SemaphoreSlim gate = new(1, 1);

    public EnvironmentManagerService(
        IDeploymentClient deploymentClient,
        IOptionsMonitor<ApiSettings> apiSettings)
    {
        this.deploymentClient = deploymentClient;
        this.apiSettings = apiSettings;
    }

    public async Task<IReadOnlyList<RemoteEnvironment>> GetEnvironmentsAsync(CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            var store = await LoadStoreAsync(cancellationToken);
            if (store.Count > 0)
            {
                if (MigratePlaceholderEndpoints(store))
                {
                    await SaveStoreAsync(store, cancellationToken);
                }

                return store;
            }

            store = CreateDefaultEnvironments();
            await SaveStoreAsync(store, cancellationToken);
            return store;
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task<RemoteEnvironment?> GetEnvironmentAsync(string environmentId, CancellationToken cancellationToken = default)
    {
        var environments = await GetEnvironmentsAsync(cancellationToken);
        return environments.FirstOrDefault(environment =>
            string.Equals(environment.Id, environmentId, StringComparison.OrdinalIgnoreCase));
    }

    public async Task SaveEnvironmentAsync(RemoteEnvironment environment, CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            var store = (await LoadStoreAsync(cancellationToken)).ToList();
            var index = store.FindIndex(item => string.Equals(item.Id, environment.Id, StringComparison.OrdinalIgnoreCase));
            if (index >= 0)
            {
                store[index] = environment;
            }
            else
            {
                store.Add(environment);
            }

            await SaveStoreAsync(store, cancellationToken);
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task<RemoteEnvironment> RegisterEnvironmentAsync(
        AgentRegistrationRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await deploymentClient.RegisterEnvironmentAsync(request, cancellationToken);
        var environment = new RemoteEnvironment
        {
            Id = response.EnvironmentId,
            Name = request.EnvironmentName,
            Description = $"Registered from {request.Hostname}",
            BaseUrl = request.PublicUrl,
            RuntimeAgentEndpoint = response.RuntimeAgentEndpoint,
            AuthenticationMode = EnvironmentAuthenticationMode.ApiKey,
            ApiKey = response.ApiKey,
            EnvironmentType = request.EnvironmentType,
            RuntimeVersion = request.RuntimeVersion,
            HealthStatus = "Healthy",
            IsRegistered = true,
            RegisteredUtc = response.RegisteredUtc
        };

        await SaveEnvironmentAsync(environment, cancellationToken);
        return environment;
    }

    public Task<EnvironmentHealthSnapshot?> RefreshHealthAsync(
        string environmentId,
        CancellationToken cancellationToken = default)
        => deploymentClient.GetEnvironmentHealthAsync(environmentId, cancellationToken);

    public Task<EnvironmentDriftReport> DetectDriftAsync(
        string environmentId,
        CancellationToken cancellationToken = default)
        => deploymentClient.DetectDriftAsync(environmentId, cancellationToken);

    private List<RemoteEnvironment> CreateDefaultEnvironments()
    {
        return
        [
            CreatePreset("local-docker", "Local Docker", EnvironmentType.LocalDocker, "http://localhost:5100"),
            CreatePreset("demo-vps", "Demo VPS", EnvironmentType.Demo, ResolveConfiguredAgentUrl()),
            CreatePreset("qa", "QA", EnvironmentType.Qa, "https://qa.aovias.local"),
            CreatePreset("uat", "UAT", EnvironmentType.Uat, "https://uat.aovias.local"),
            CreatePreset("production", "Production", EnvironmentType.Production, "https://production.aovias.local"),
            CreatePreset("customer-a", "Customer A", EnvironmentType.Customer, "https://customer-a.aovias.local"),
            CreatePreset("customer-b", "Customer B", EnvironmentType.Customer, "https://customer-b.aovias.local")
        ];
    }

    private bool MigratePlaceholderEndpoints(List<RemoteEnvironment> environments)
    {
        var changed = false;
        var configuredAgentUrl = ResolveConfiguredAgentUrl();
        for (var index = 0; index < environments.Count; index++)
        {
            var environment = environments[index];
            if (!string.Equals(environment.Id, "demo-vps", StringComparison.OrdinalIgnoreCase)
                || (!IsPlaceholderEndpoint(environment.BaseUrl)
                    && !IsPlaceholderEndpoint(environment.RuntimeAgentEndpoint)))
            {
                continue;
            }

            environments[index] = environment with
            {
                BaseUrl = configuredAgentUrl,
                RuntimeAgentEndpoint = configuredAgentUrl,
                Description = "Demo VPS deployment target (configured Runtime Agent endpoint)"
            };
            changed = true;
        }

        return changed;
    }

    private string ResolveConfiguredAgentUrl()
    {
        var configured = apiSettings.CurrentValue.BaseUrl;
        if (!Uri.TryCreate(configured, UriKind.Absolute, out _))
        {
            throw new InvalidOperationException(
                $"Api:BaseUrl '{configured}' is not a valid Runtime Agent endpoint.");
        }

        return configured.TrimEnd('/');
    }

    private static bool IsPlaceholderEndpoint(string endpoint)
        => Uri.TryCreate(endpoint, UriKind.Absolute, out var uri)
            && uri.Host.EndsWith(".aovias.local", StringComparison.OrdinalIgnoreCase);

    private static RemoteEnvironment CreatePreset(string id, string name, EnvironmentType type, string endpoint)
        => new()
        {
            Id = id,
            Name = name,
            Description = $"{name} deployment target",
            BaseUrl = endpoint,
            RuntimeAgentEndpoint = endpoint,
            EnvironmentType = type,
            AuthenticationMode = EnvironmentAuthenticationMode.ApiKey,
            HealthStatus = "Unknown"
        };

    private string GetStorePath()
    {
        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "AOVIAS Command");
        Directory.CreateDirectory(root);
        return Path.Combine(root, "environments.json");
    }

    private async Task<List<RemoteEnvironment>> LoadStoreAsync(CancellationToken cancellationToken)
    {
        var path = GetStorePath();
        if (!File.Exists(path))
        {
            return [];
        }

        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<List<RemoteEnvironment>>(stream, JsonOptions, cancellationToken) ?? [];
    }

    private async Task SaveStoreAsync(IReadOnlyList<RemoteEnvironment> environments, CancellationToken cancellationToken)
    {
        var path = GetStorePath();
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, environments, JsonOptions, cancellationToken);
    }
}
