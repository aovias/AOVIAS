using System.Collections.ObjectModel;
using System.IO;
using Aovias.Release.Contracts.RemoteDeployment;
using Aovias.Runtime.SDK;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ComplianceServiceManager.Contracts.Deployment;
using ComplianceServiceManager.Models;
using ComplianceServiceManager.Services;

namespace ComplianceServiceManager.ViewModels.Platform;

/// <summary>
/// Platform → Runtime Agents workspace. The Runtime Agent is a platform component, not a local
/// dev service: this workspace shows the connected agent's health, every registered agent in the
/// fleet, and start/stop control for the local development agent instance.
/// </summary>
public sealed partial class RuntimeAgentsViewModel : ObservableObject
{
    private readonly IRuntimeAgentClient runtimeAgentClient;
    private readonly IEnvironmentManager environmentManager;
    private readonly ServiceManagerService serviceManager;

    [ObservableProperty]
    private string connectedAgentStatus = "Checking…";

    [ObservableProperty]
    private string connectedAgentDetail = string.Empty;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private ServiceInfoModel? localAgent;

    public ObservableCollection<RemoteEnvironment> RegisteredAgents { get; } = [];

    public RuntimeAgentsViewModel(
        IRuntimeAgentClient runtimeAgentClient,
        IEnvironmentManager environmentManager,
        ServiceManagerService serviceManager)
    {
        this.runtimeAgentClient = runtimeAgentClient;
        this.environmentManager = environmentManager;
        this.serviceManager = serviceManager;
        LocalAgent = TryCreateLocalAgentModel();
    }

    [RelayCommand]
    private async Task InitializeAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        try
        {
            await RefreshConnectedAgentAsync();

            RegisteredAgents.Clear();
            foreach (var environment in await environmentManager.GetEnvironmentsAsync())
            {
                RegisteredAgents.Add(environment);
            }

            StatusMessage = RegisteredAgents.Count == 0
                ? "No runtime agents are registered with the fleet yet."
                : $"{RegisteredAgents.Count} runtime agent(s) registered.";
        }
        catch (Exception exception)
        {
            StatusMessage = $"Unable to load runtime agents: {exception.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task RefreshConnectedAgentAsync()
    {
        try
        {
            var health = await runtimeAgentClient.GetRuntimeHealthAsync();
            ConnectedAgentStatus = health.Status;
            ConnectedAgentDetail = "Connected runtime agent responded to the health probe.";
        }
        catch (Exception exception)
        {
            ConnectedAgentStatus = "Offline";
            ConnectedAgentDetail = exception.Message;
        }
    }

    [RelayCommand]
    private async Task StartLocalAgentAsync()
    {
        if (LocalAgent is null)
        {
            return;
        }

        await serviceManager.StartAsync(LocalAgent, CancellationToken.None);
        StatusMessage = "Local runtime agent starting…";
    }

    [RelayCommand]
    private async Task StopLocalAgentAsync()
    {
        if (LocalAgent is null)
        {
            return;
        }

        await serviceManager.StopAsync(LocalAgent, CancellationToken.None);
        StatusMessage = "Local runtime agent stopped.";
    }

    /// <summary>
    /// The local development Runtime Agent (run from source via dotnet run). Previously this
    /// lived inside local-service discovery; it now belongs to this platform workspace.
    /// </summary>
    private static ServiceInfoModel? TryCreateLocalAgentModel()
    {
        try
        {
            var projectPath = RootPathResolver.ResolveRuntimeAgentProjectPath();
            if (!File.Exists(projectPath))
            {
                return null;
            }

            return new ServiceInfoModel
            {
                Name = "Runtime Agent (local)",
                Kind = ServiceKind.WebApi,
                ProjectPath = projectPath,
                WorkingDirectory = Path.GetDirectoryName(projectPath) ?? RootPathResolver.ResolvePlatformRoot(),
                Port = 7080,
                HttpPort = 7080,
                HttpUrl = "http://localhost:7080",
                LaunchProfileName = "Runtime Agent",
                EnvironmentName = "Production",
                Environment = "Production",
                LastAction = "Available"
            };
        }
        catch
        {
            return null;
        }
    }
}
