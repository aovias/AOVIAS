using System;
using System.IO;
using System.Windows;
using ComplianceServiceManager.AI;
using ComplianceServiceManager.Diagnostics;
using ComplianceServiceManager.Logging;
using ComplianceServiceManager.Operations;
using ComplianceServiceManager.Services;
using ComplianceServiceManager.Services.Bootstrap;
using ComplianceServiceManager.Services.Configuration;
using ComplianceServiceManager.Navigation;
using ComplianceServiceManager.Models.LocalResources;
using ComplianceServiceManager.Models.ResourceIntelligence;
using ComplianceServiceManager.Services.AgentSessions;
using ComplianceServiceManager.Services.LocalDev;
using ComplianceServiceManager.Services.LocalPullRequests;
using ComplianceServiceManager.Services.LocalResources;
using ComplianceServiceManager.Services.ResourceIntelligence;
using ComplianceServiceManager.Services.Runtime;
using ComplianceServiceManager.Services.RuntimeProfiles;
using ComplianceServiceManager.Services.Workspaces;
using ComplianceServiceManager.ViewModels;
using ComplianceServiceManager.ViewModels.AiCopilot;
using ComplianceServiceManager.ViewModels.ReleaseCenter;
using ComplianceServiceManager.ViewModels.Infrastructure;
using ComplianceServiceManager.ViewModels.Observability;
using ComplianceServiceManager.ViewModels.Overview;
using ComplianceServiceManager.ViewModels.Publishing;
using ComplianceServiceManager.ViewModels.Runtime;
using ComplianceServiceManager.ViewModels.Applications;
using ComplianceServiceManager.ViewModels.Development;
using ComplianceServiceManager.ViewModels.Operations;
using ComplianceServiceManager.ViewModels.Platform;
using ComplianceServiceManager.ViewModels.Releases;
using ComplianceServiceManager.Contracts.Publishing;
using ComplianceServiceManager.Services.Publishing;
using ComplianceServiceManager.ViewModels.Deployment;
using ComplianceServiceManager.ViewModels.Settings;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace ComplianceServiceManager;

public partial class App : Application
{
    private ServiceProvider? serviceProvider;

    protected override void OnStartup(StartupEventArgs e)
    {
        DispatcherUnhandledException += (_, args) =>
        {
            LogCrash(args.Exception);
            args.Handled = true;
            MessageBox.Show(
                $"AOVIAS Studio encountered an error:{Environment.NewLine}{Environment.NewLine}{args.Exception.Message}",
                "AOVIAS Studio",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        };

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            if (args.ExceptionObject is Exception exception)
            {
                LogCrash(exception);
            }
        };

        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            LogCrash(args.Exception);
            args.SetObserved();
        };

        try
        {
            base.OnStartup(e);
            ConfigureSerilog();
            serviceProvider = ConfigureServices();

            var window = serviceProvider.GetRequiredService<MainWindow>();
            MainWindow = window;
            window.Show();
        }
        catch (Exception exception)
        {
            LogCrash(exception);
            MessageBox.Show(
                $"AOVIAS Studio failed to start:{Environment.NewLine}{Environment.NewLine}{exception.Message}",
                "AOVIAS Studio",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(-1);
        }
    }

    private static void LogCrash(Exception exception)
    {
        try
        {
            var logPath = Path.Combine(AppContext.BaseDirectory, "startup-crash.log");
            File.AppendAllText(logPath, $"{DateTimeOffset.Now:O}{Environment.NewLine}{exception}{Environment.NewLine}{Environment.NewLine}");
        }
        catch
        {
            // best-effort
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        try
        {
            if (serviceProvider is IAsyncDisposable asyncDisposable)
            {
                asyncDisposable.DisposeAsync().AsTask().GetAwaiter().GetResult();
            }
            else
            {
                serviceProvider?.Dispose();
            }
        }
        catch (Exception exception)
        {
            LogCrash(exception);
        }

        if (MainWindow?.DataContext is ShellViewModel shellViewModel &&
            shellViewModel.ComplianceViewModel is IDisposable disposable)
        {
            disposable.Dispose();
        }

        Log.CloseAndFlush();
        base.OnExit(e);
    }

    private static ServiceProvider ConfigureServices()
    {
        var configuration = AoviasConfigurationBootstrap.Build();
        var services = new ServiceCollection();

        services.AddAoviasPlatform(configuration);

        services.AddSingleton<ILoggingService>(provider => new LoggingService(Current.Dispatcher));
        services.AddSingleton<SolutionFolderRegistry>();
        services.AddSingleton<DevelopmentWorkspaceRegistry>();
        services.AddSingleton(provider => new GitCliRunner(
            () => provider.GetRequiredService<DevelopmentWorkspaceRegistry>().GetAllowedRoots()));
        services.AddSingleton<IGitWorkspaceService, GitWorkspaceService>();
        services.AddSingleton<IDevelopmentWorkspaceService, DevelopmentWorkspaceService>();
        services.AddSingleton<RuntimeProfileRegistry>();
        services.AddSingleton<RuntimeProfileValidator>();
        services.AddSingleton<LocalPullRequestRegistry>();
        services.AddSingleton<IGitPullRequestService, GitPullRequestService>();
        services.AddSingleton<ILocalPullRequestService, LocalPullRequestService>();
        services.AddSingleton<AgentSessionRegistry>();
        services.AddSingleton<IAgentSessionService, AgentSessionService>();
        services.AddSingleton(ResourceThresholdPolicy.Default);
        services.AddSingleton(new LocalResourceMonitoringOptions
        {
            PollInterval = TimeSpan.FromSeconds(3),
            DiskProbePath = @"V:\DevelopmentRepo",
            Thresholds = LocalResourceThresholds.Default
        });
        services.AddSingleton<ILocalResourceCollector, WindowsLocalResourceCollector>();
        services.AddSingleton<ILocalResourceMonitoringService>(provider =>
            new LocalResourceMonitoringService(
                provider.GetRequiredService<ILocalResourceCollector>(),
                provider.GetRequiredService<LocalResourceMonitoringOptions>(),
                provider.GetRequiredService<IServiceRuntimeHost>()));
        services.AddSingleton<IResourceIntelligenceService>(provider =>
            new ResourceIntelligenceService(
                provider.GetRequiredService<ILocalResourceMonitoringService>(),
                provider.GetRequiredService<ResourceThresholdPolicy>(),
                provider.GetRequiredService<IRuntimeProfileOrchestrator>(),
                provider.GetRequiredService<IAgentSessionService>()));
        services.AddSingleton(provider => new ServiceDiscoveryService(
            provider.GetRequiredService<SolutionFolderRegistry>(),
            provider.GetRequiredService<ILoggingService>()));
        services.AddSingleton<ILogFileService>(_ =>
            new LogFileService(RootPathResolver.ResolveCompliancePlatformRoot()));
        services.AddSingleton<ProcessService>();
        services.AddSingleton<BuildService>();
        services.AddSingleton<HealthCheckService>();
        services.AddSingleton<IHealthProbe>(provider => provider.GetRequiredService<HealthCheckService>());
        services.AddSingleton<BuildRepairService>();
        services.AddSingleton<PortRecoveryService>();
        services.AddSingleton<IRepairHistoryStore, RepairHistoryStore>();
        services.AddSingleton<IRepairAnalyticsService, RepairAnalyticsService>();
        services.AddSingleton<IFixedErrorStore, FixedErrorStore>();
        services.AddSingleton<IDiagnosticsCollector, DiagnosticsCollector>();
        services.AddSingleton<ICursorPromptBuilder, CursorPromptBuilder>();
        services.AddSingleton<ICursorAutomationService, CursorAutomationService>();
        services.AddSingleton<IGitSafetyService, GitSafetyService>();
        services.AddSingleton<IFixExecutionPipeline, FixExecutionPipeline>();
        services.AddSingleton<IAIFixOrchestrator, AIFixOrchestrator>();
        services.AddSingleton<ServiceManagerService>();
        services.AddSingleton<IServiceRuntimeHost>(provider => provider.GetRequiredService<ServiceManagerService>());
        services.AddSingleton<IRuntimeProfileOrchestrator, RuntimeProfileOrchestrator>();
        services.AddSingleton<WorkspaceNavigator>();
        services.AddSingleton<IWorkspaceNavigator>(provider => provider.GetRequiredService<WorkspaceNavigator>());
        services.AddSingleton<IRuntimeCapabilitiesService, RuntimeCapabilitiesService>();
        services.AddSingleton<OverviewViewModel>();
        services.AddSingleton<InfrastructureViewModel>();
        services.AddSingleton<AiCopilotViewModel>();
        services.AddSingleton<RuntimeDashboardViewModel>();
        services.AddSingleton<SettingsConfigurationViewModel>();
        services.AddSingleton<ReleaseCenterViewModel>();
        services.AddSingleton<DeploymentCenterViewModel>();
        services.AddSingleton<PlatformDeploymentCenterViewModel>();
        services.AddSingleton<IPublishingWorkspaceService, PublishingWorkspaceService>();
        services.AddSingleton<PublishingViewModel>();
        services.AddSingleton<ObservabilityViewModel>();
        services.AddSingleton<MainViewModel>();

        // AOVIAS Studio workspaces
        services.AddSingleton<EnvironmentsViewModel>();
        services.AddSingleton<TargetHostProbeService>();
        services.AddSingleton<OperationHistoryStore>();
        services.AddSingleton<OperationEngine>();
        services.AddSingleton<BootstrapViewModel>();
        services.AddSingleton<RuntimeAgentsViewModel>();
        services.AddSingleton<CertificatesViewModel>();
        services.AddSingleton<ReleasesViewModel>();
        services.AddSingleton<DeploymentsWorkspaceViewModel>();
        services.AddSingleton<RollbackHistoryViewModel>();
        services.AddSingleton<LiveOperationsViewModel>();
        services.AddSingleton<ReconciliationViewModel>();
        services.AddSingleton<AlertsViewModel>();
        services.AddSingleton<AuditViewModel>();
        services.AddSingleton<LocalDevServicesViewModel>();

        services.AddSingleton<ShellViewModel>();
        services.AddSingleton<MainWindow>();

        return services.BuildServiceProvider();
    }

    private static void ConfigureSerilog()
    {
        var logRoot = Path.Combine(AppContext.BaseDirectory, "Diagnostics", "ManagerLogs");
        Directory.CreateDirectory(logRoot);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(
                Path.Combine(logRoot, "manager-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14,
                shared: true)
            .CreateLogger();
    }
}
