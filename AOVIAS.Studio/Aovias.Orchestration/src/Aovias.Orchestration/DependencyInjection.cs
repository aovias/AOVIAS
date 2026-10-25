using Aovias.Orchestration.Contracts;
using Aovias.Orchestration.Contracts.Events;
using Aovias.Orchestration.Contracts.Providers;
using Aovias.Orchestration.Engine;
using Aovias.Orchestration.Providers;
using Aovias.Orchestration.Steps;
using Aovias.Orchestration.Workflows;
using Aovias.Platform.Core;
using Microsoft.Extensions.DependencyInjection;

namespace Aovias.Orchestration;

/// <summary>
/// Orchestration engine dependency injection extensions.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers orchestration engine services.
    /// </summary>
    public static IServiceCollection AddOrchestrationEngine(this IServiceCollection services)
    {
        services.AddSingleton<IPlatformClock, PlatformClock>();
        services.AddSingleton<WorkflowStateMachine>();
        services.AddSingleton<InMemoryWorkflowStore>();
        services.AddSingleton<IWorkflowProvider, DefaultWorkflowProvider>();
        services.AddSingleton<IWorkflowRegistry, WorkflowRegistry>();
        services.AddSingleton<IReleaseDeploymentProvider, NullReleaseDeploymentProvider>();

        services.AddSingleton<IWorkflowStep, ValidatePackageStep>();
        services.AddSingleton<IWorkflowStep, VerifyRuntimeStep>();
        services.AddSingleton<IWorkflowStep, VerifyDependenciesStep>();
        services.AddSingleton<IWorkflowStep, BackupStep>();
        services.AddSingleton<IWorkflowStep, ComposeStopStep>();
        services.AddSingleton<IWorkflowStep, ExtractPackageStep>();
        services.AddSingleton<IWorkflowStep, StageReleaseStep>();
        services.AddSingleton<IWorkflowStep, LoadDockerImageStep>();
        services.AddSingleton<IWorkflowStep, EnsureRuntimeFoldersStep>();
        services.AddSingleton<IWorkflowStep, ConfigureNginxStep>();
        services.AddSingleton<IWorkflowStep, RegisterDeploymentStep>();
        services.AddSingleton<IWorkflowStep, RegisterRuntimeStep>();
        services.AddSingleton<IWorkflowStep, DeploymentAcceptanceStep>();
        services.AddSingleton<IWorkflowStep, ComposeUpStep>();
        services.AddSingleton<IWorkflowStep, RestorePreviousReleaseStep>();
        services.AddSingleton<IWorkflowStep, StopContainerStep>();
        services.AddSingleton<IWorkflowStep, StartContainerStep>();
        services.AddSingleton<IWorkflowStep, HealthVerificationStep>();
        services.AddSingleton<IWorkflowStep, ActivateReleaseStep>();
        services.AddSingleton<IWorkflowStep, AuditStep>();
        services.AddSingleton<IWorkflowStep, DockerReadyStep>();
        services.AddSingleton<IWorkflowStep, RestartContainerStep>();
        services.AddSingleton<IWorkflowStep, MaintenanceStep>();
        services.AddSingleton<IWorkflowStep, RecoveryStep>();

        services.AddSingleton<IWorkflowExecutor, WorkflowExecutor>();
        services.AddSingleton<IExecutionEngine, ExecutionEngine>();

        return services;
    }
}
