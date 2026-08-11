using ComplianceServiceManager.Diagnostics;
using ComplianceServiceManager.Models;
using ComplianceServiceManager.Services;

namespace ComplianceServiceManager.AI;

public sealed class AIFixOrchestrator : IAIFixOrchestrator
{
    private readonly IDiagnosticsCollector diagnosticsCollector;
    private readonly ICursorAutomationService cursorAutomationService;
    private readonly IFixExecutionPipeline fixExecutionPipeline;
    private readonly IGitSafetyService gitSafetyService;
    private readonly IRepairHistoryStore repairHistoryStore;
    private readonly IRepairAnalyticsService repairAnalyticsService;
    private readonly IFixedErrorStore fixedErrorStore;

    public AIFixOrchestrator(
        IDiagnosticsCollector diagnosticsCollector,
        ICursorAutomationService cursorAutomationService,
        IFixExecutionPipeline fixExecutionPipeline,
        IGitSafetyService gitSafetyService,
        IRepairHistoryStore repairHistoryStore,
        IRepairAnalyticsService repairAnalyticsService,
        IFixedErrorStore fixedErrorStore)
    {
        this.diagnosticsCollector = diagnosticsCollector;
        this.cursorAutomationService = cursorAutomationService;
        this.fixExecutionPipeline = fixExecutionPipeline;
        this.gitSafetyService = gitSafetyService;
        this.repairHistoryStore = repairHistoryStore;
        this.repairAnalyticsService = repairAnalyticsService;
        this.fixedErrorStore = fixedErrorStore;
    }

    public async Task<FixExecutionResult> RunAsync(ServiceInfoModel service, AIFixOptions options, IProgress<string>? progress, CancellationToken cancellationToken)
    {
        var startedAt = DateTimeOffset.UtcNow;
        service.IsRepairing = true;
        service.RepairAttempts++;
        service.RepairStage = RepairStage.Diagnosing;
        service.Status = ServiceStatus.Diagnosing;
        service.LastAction = "Collecting diagnostics";
        progress?.Report($"[{service.Name}] Reading log files and collecting diagnostics...");

        var diagnostics = await diagnosticsCollector.CollectAsync(service, cancellationToken);
        service.LastFailureCategory = diagnostics.FailureCategory;
        service.LastAction = "Launching AI Fix";
        service.RepairStage = RepairStage.Fixing;
        service.Status = ServiceStatus.Fixing;
        progress?.Report(
            diagnostics.UnresolvedFailures.Count > 0
                ? $"[{service.Name}] {diagnostics.UnresolvedFailures.Count} error(s) to fix ({diagnostics.FailureCategory}). Launching AI fix..."
                : $"[{service.Name}] No new structured errors; running self-healing for {diagnostics.FailureCategory}...");

        var checkpoint = await gitSafetyService.CreateCheckpointAsync(service.WorkingDirectory, cancellationToken);
        var modifiedFiles = await cursorAutomationService.ExecuteFixAsync(service, diagnostics, cancellationToken);

        progress?.Report($"[{service.Name}] Rebuilding and validating the service...");
        var result = await fixExecutionPipeline.ExecuteAsync(service, diagnostics, modifiedFiles, checkpoint, options, cancellationToken);

        service.LastRepairAt = DateTimeOffset.Now;
        service.RepairSummary = result.Summary;
        service.IsRepairing = false;

        // Remember which errors were targeted so subsequent runs know they are already handled.
        if (result.Success && diagnostics.UnresolvedFailures.Count > 0)
        {
            fixedErrorStore.MarkResolved(
                service.Name,
                diagnostics.UnresolvedFailures.Select(f => f.Fingerprint),
                DateTimeOffset.UtcNow);
        }

        progress?.Report($"[{service.Name}] {(result.Success ? "Repair validated successfully." : "Repair completed with issues.")}");

        repairHistoryStore.Add(new RepairHistoryEntry
        {
            ServiceName = service.Name,
            Stage = result.Success ? RepairStage.Resolved : RepairStage.FailedRepair,
            Success = result.Success,
            Summary = result.Summary,
            SessionPath = diagnostics.RootDirectory,
            CheckpointReference = checkpoint,
            FailureCategory = diagnostics.FailureCategory
        });

        await repairAnalyticsService.RecordAsync(
            service,
            DateTimeOffset.UtcNow - startedAt,
            result.Success,
            diagnostics.FailureCategory,
            cancellationToken);

        return result;
    }

    public async Task<IReadOnlyList<FixExecutionResult>> FixAllFailedAsync(
        IReadOnlyList<ServiceInfoModel> failedServices,
        AIFixOptions options,
        IProgress<string>? progress,
        CancellationToken cancellationToken)
    {
        var results = new List<FixExecutionResult>();
        var position = 1;
        foreach (var service in failedServices)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            progress?.Report($"Fixing service {position}/{failedServices.Count}: {service.Name}");
            results.Add(await RunAsync(service, options, progress, cancellationToken));
            position++;
        }

        return results;
    }
}
