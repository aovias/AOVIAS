using ComplianceServiceManager.Logging;
using ComplianceServiceManager.Models;

namespace ComplianceServiceManager.Services;

public sealed class FixExecutionPipeline : IFixExecutionPipeline
{
    private readonly BuildRepairService buildRepairService;
    private readonly ServiceManagerService serviceManagerService;
    private readonly HealthCheckService healthCheckService;
    private readonly PortRecoveryService portRecoveryService;
    private readonly ILoggingService logger;

    public FixExecutionPipeline(
        BuildRepairService buildRepairService,
        ServiceManagerService serviceManagerService,
        HealthCheckService healthCheckService,
        PortRecoveryService portRecoveryService,
        ILoggingService logger)
    {
        this.buildRepairService = buildRepairService;
        this.serviceManagerService = serviceManagerService;
        this.healthCheckService = healthCheckService;
        this.portRecoveryService = portRecoveryService;
        this.logger = logger;
    }

    public async Task<FixExecutionResult> ExecuteAsync(
        ServiceInfoModel service,
        DiagnosticBundle bundle,
        IReadOnlyList<string> modifiedFiles,
        string checkpointReference,
        AIFixOptions options,
        CancellationToken cancellationToken)
    {
        var actions = new List<string>();
        if (options.DryRun)
        {
            actions.Add("Dry run enabled. No fix actions executed.");
            return new FixExecutionResult
            {
                Success = false,
                Summary = "Dry run completed. Diagnostics and prompt generated.",
                CheckpointReference = checkpointReference,
                ModifiedFiles = modifiedFiles,
                Actions = actions
            };
        }

        service.RepairStage = RepairStage.Rebuilding;
        service.Status = ServiceStatus.Rebuilding;
        actions.Add("Running restore/build with self-healing.");
        var built = await buildRepairService.RestoreBuildWithSelfHealingAsync(service, cancellationToken);
        if (!built)
        {
            if (service.LastFailureCategory == FailureCategory.PortConflict || service.LastAction.Contains("Port", StringComparison.OrdinalIgnoreCase))
            {
                portRecoveryService.TryReassign(service);
                actions.Add("Port reassignment attempted.");
            }

            return new FixExecutionResult
            {
                Success = false,
                Summary = "Build repair pipeline failed.",
                CheckpointReference = checkpointReference,
                ModifiedFiles = modifiedFiles,
                Actions = actions
            };
        }

        service.RepairStage = RepairStage.Restarting;
        service.Status = ServiceStatus.Restarting;
        actions.Add("Restarting service.");
        await serviceManagerService.RestartAsync(service, cancellationToken);

        await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
        var health = await healthCheckService.CheckAsync(service, cancellationToken);
        service.Health = health;
        var success = health == HealthStatus.Healthy && service.Status == ServiceStatus.Running;
        actions.Add($"Health check result: {health}");

        if (success)
        {
            service.RepairStage = RepairStage.Resolved;
            service.Status = ServiceStatus.Resolved;
            service.LastAction = "AI Repair Successful";
        }
        else
        {
            service.RepairStage = RepairStage.FailedRepair;
            service.Status = ServiceStatus.FailedRepair;
            service.LastAction = "AI Repair Failed";
            logger.Warning(service.Name, "Post-fix validation failed; rollback available.");
        }

        return new FixExecutionResult
        {
            Success = success,
            Summary = success ? "AI repair validated successfully." : "AI repair applied but validation failed.",
            CheckpointReference = checkpointReference,
            ModifiedFiles = modifiedFiles,
            Actions = actions
        };
    }
}
