using Aovias.Configuration.Features;
using Aovias.Configuration.Extensions;
using Aovias.Orchestration;
using Aovias.Orchestration.Contracts;
using Aovias.Orchestration.Contracts.Providers;
using Aovias.ReleaseManager;
using Aovias.Runtime.Agent.Application.Interfaces;
using Aovias.Runtime.Agent.Infrastructure.Admission;
using Aovias.Deployment.Contracts.Infrastructure;
using Aovias.Deployment.Contracts.Reading;
using Aovias.Runtime.Agent.Application.Deployment;
using Aovias.Runtime.Agent.Infrastructure.Deployment;
using Aovias.Runtime.Agent.Infrastructure.Reliability;
using Aovias.Deployment.Contracts.Smoke;
using Aovias.Runtime.Agent.Infrastructure.Docker;
using Aovias.Runtime.Agent.Infrastructure.Infrastructure;
using Aovias.Runtime.Agent.Infrastructure.Health;
using Aovias.Runtime.Agent.Infrastructure.Monitoring;
using Aovias.Runtime.Agent.Infrastructure.Persistence;
using Aovias.Runtime.Agent.Infrastructure.Providers;
using Aovias.Runtime.Agent.Infrastructure.Reconciliation;
using Aovias.Runtime.Agent.Infrastructure.Registration;
using Aovias.Deployment.Contracts.Reconciliation;
using Aovias.Runtime.Agent.Infrastructure.SignalR;
using Aovias.Runtime.Agent.Infrastructure.Observability;
using Aovias.Runtime.Agent.Infrastructure.Observability.EventBus;
using Aovias.Runtime.Agent.Infrastructure.Runtime;
using Aovias.Runtime.Agent.Infrastructure.System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Aovias.Runtime.Agent.Infrastructure;

/// <summary>
/// Infrastructure layer dependency injection extensions.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers infrastructure services for the runtime agent.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var features = PlatformFeatureActivation.ResolveRuntimeAgent(configuration);
        services.AddRuntimeAgentConfiguration(configuration, features);
        services.AddHttpClient();

        if (features.ReleaseManagement)
        {
            services.AddReleaseManager();
            services.AddOrchestrationEngine();
            RegisterReleaseManagementProviders(services, features);
            services.AddSingleton<RuntimeAgentDeploymentPathResolver>();
            services.AddSingleton<IDeploymentPackageContractReader, DeploymentPackageContractReader>();
            services.AddSingleton<IReleaseDeploymentProvider, ReleaseDeploymentProvider>();
            services.AddSingleton<IDeploymentWorkflowCoordinator, DeploymentWorkflowCoordinator>();
            services.AddSingleton<IInfrastructureExecutionContext, DockerInfrastructureExecutionContext>();
            services.AddSingleton<DeploymentQueueStore>();
            services.AddSingleton<DeploymentSessionStore>();
            services.AddSingleton<EnvironmentRegistrationStore>();
            services.AddSingleton<DeploymentPackageStore>();
            services.AddSingleton<PlatformDeploymentService>();
            services.AddSingleton<IPlatformDeploymentService>(provider => provider.GetRequiredService<PlatformDeploymentService>());
            services.AddSingleton<IServiceSmokeTestRunner, ServiceSmokeTestRunner>();
            services.AddSingleton<PlatformCertificationOrchestrator>();
            services.AddSingleton<DeploymentReplayStore>();
            services.AddSingleton<DeploymentReliabilityMetricsStore>();
            services.AddSingleton<ChaosTestHarness>();
            services.AddSingleton<PlatformReconciliationService>();
            services.AddHostedService<PlatformDeploymentWorker>();

            // Sprint M2.6: the Autonomous Infrastructure Reconciliation & Self-Healing Engine.
            // Consolidates into, rather than duplicates, the pre-existing PlatformReconciliationService/
            // PlatformReconciliationWorker (Sprint M2.0-era) — see PlatformReliabilityServices.cs
            // and the M2.6 report's Technical Debt section for exactly what was and was not
            // unified. PlatformReconciliationWorker's naive fixed-loop is replaced outright by
            // ReconciliationSchedulerHostedService, which drives the real engine below instead of
            // only checking Docker's overview status.
            services.AddSingleton<LastDeploymentPlanStore>();
            // Sprint M2.7: in-memory maintenance-mode flag, consulted by
            // RuntimeReconciliationTargetProvider's SafetyContextFactory and served/mutated by
            // DeploymentController's new heartbeat/maintenance actions.
            services.AddSingleton<MaintenanceModeStore>();
            services.AddSingleton<IRuntimeHeartbeatService, RuntimeHeartbeatService>();
            services.AddSingleton<IDesiredStateProvider, DesiredStateProvider>();
            services.AddSingleton<IObservedStateDiscoveryEngine, RuntimeObservedStateDiscoveryEngine>();
            services.AddSingleton<IDriftDetectionEngine, DriftDetectionEngine>();
            services.AddSingleton<IReconciliationPlanner, ReconciliationPlanner>();
            services.AddSingleton<IRepairPolicyEvaluator, RepairPolicyEvaluator>();
            services.AddSingleton<IOperatorApprovalStore, InMemoryOperatorApprovalStore>();
            services.AddSingleton<IRepairExecutor, DockerNetworkRepairExecutor>();
            services.AddSingleton<IRepairExecutor, DirectoryRepairExecutor>();
            services.AddSingleton<IRepairExecutor, ContainerRestartRepairExecutor>();
            services.AddSingleton<IRepairExecutor, PermissionsRepairExecutor>();
            services.AddSingleton<IRepairExecutor, SymbolicLinkRepairExecutor>();
            services.AddSingleton<IRepairExecutor, ConfigurationReloadRepairExecutor>();
            services.AddSingleton<IRepairExecutor, ApprovalRequiredRepairExecutor>();
            services.AddSingleton(provider => new RepairExecutorRegistry(provider.GetServices<IRepairExecutor>().ToList()));
            services.AddSingleton<ISelfHealingEngine, SelfHealingEngine>();
            services.AddSingleton<IReconciliationHistoryStore, FileReconciliationHistoryStore>();
            services.AddSingleton<IReconciliationOrchestrator, ReconciliationOrchestrator>();
            services.AddSingleton<IReconciliationTargetProvider, RuntimeReconciliationTargetProvider>();
            services.AddSingleton<IReconciliationScheduler, ReconciliationScheduler>();
            services.AddHostedService<ReconciliationSchedulerHostedService>();

            // Sprint M4.3: automatic environment registration & capability declaration. Reuses
            // EnvironmentRegistrationStore (already registered above) and RuntimeCapabilitiesProvider
            // (registered below) rather than duplicating either — this hosted service only adds the
            // "call it automatically, and tell Command Center" orchestration around them.
            services.AddSingleton<EnvironmentRegistrationRecordStore>();
            services.AddHttpClient<ICommandCenterRegistrationClient, CommandCenterRegistrationClient>();
            services.AddHostedService<EnvironmentRegistrationHostedService>();

            // Sprint M4.4: Environment Admission & Deployment Readiness. Reuses LastDeploymentPlanStore
            // (already registered above) and EnvironmentRegistrationSettings' new CapabilityProfileId
            // field rather than introducing a second capability/requirement source.
            services.AddSingleton<EnvironmentAdmissionStore>();
            services.AddHostedService<EnvironmentAdmissionHostedService>();

            if (!features.SignalR)
            {
                RegisterIfMissing<IWorkflowEventBroadcaster, NoOpWorkflowEventBroadcaster>(services);
                RegisterIfMissing<IPlatformDeploymentEventPublisher, NoOpPlatformDeploymentEventPublisher>(services);
            }
        }

        if (features.Observability)
        {
            services.AddObservabilityPlatform();
        }
        else
        {
            RegisterIfMissing<IRuntimeEventBus, RuntimeEventBus>(services);
        }

        if (features.SignalR)
        {
            RegisterIfMissing<IRuntimeEventBus, RuntimeEventBus>(services);
            RegisterIfMissing<IWorkflowEventBroadcaster, SignalRWorkflowEventBroadcaster>(services);
            services.AddSingleton<IPlatformDeploymentEventPublisher, PlatformDeploymentSignalRBroadcaster>();
        }
        else
        {
            RegisterIfMissing<IPlatformDeploymentEventPublisher, NoOpPlatformDeploymentEventPublisher>(services);
        }

        if (features.DockerProvider)
        {
            services.AddSingleton<Aovias.Orchestration.Contracts.Providers.IDockerProvider, OrchestrationDockerProvider>();
        }

        services.AddSingleton<IFileSystemProvider, OrchestrationFileSystemProvider>();
        services.AddSingleton<ISystemProvider, OrchestrationSystemProvider>();
        services.AddSingleton<Aovias.Orchestration.Contracts.Providers.IHealthProvider, OrchestrationHealthProvider>();
        services.AddSingleton<INetworkProvider, OrchestrationNetworkProvider>();

        if (features.Certificates)
        {
            RegisterIfMissing<INginxProvider, OrchestrationNginxProvider>(services);
            RegisterIfMissing<ICertificateProvider, OrchestrationCertificateProvider>(services);
        }

        services.AddSingleton<RuntimeCapabilitiesProvider>();

        services.AddSingleton<RuntimeManifestStore>();
        services.AddSingleton<IRuntimeManifestProvider, RuntimeManifestProvider>();
        services.AddSingleton<IRuntimeProvider, RuntimeProvider>();
        services.AddSingleton<ISystemInformationProvider, SystemInformationProvider>();
        services.AddSingleton<Aovias.Runtime.Agent.Application.Interfaces.IDockerProvider, DockerProvider>();
        services.AddSingleton<Aovias.Runtime.Agent.Application.Interfaces.IHealthProvider, HealthProvider>();
        services.AddSingleton<IStatisticsProvider, StatisticsProvider>();

        return services;
    }

    private static void RegisterReleaseManagementProviders(
        IServiceCollection services,
        PlatformFeatureActivation features)
    {
        RegisterIfMissing<INginxProvider, OrchestrationNginxProvider>(services);
        RegisterIfMissing<ICertificateProvider, OrchestrationCertificateProvider>(services);

        if (!features.DockerProvider)
        {
            RegisterIfMissing<Aovias.Orchestration.Contracts.Providers.IDockerProvider, OrchestrationDockerProvider>(services);
        }
    }

    private static void RegisterIfMissing<TService, TImplementation>(IServiceCollection services)
        where TService : class
        where TImplementation : class, TService
    {
        if (services.All(descriptor => descriptor.ServiceType != typeof(TService)))
        {
            services.AddSingleton<TService, TImplementation>();
        }
    }
}
