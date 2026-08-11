using Aovias.Deployment.Contracts.Reconciliation;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aovias.Runtime.Agent.Infrastructure.Reconciliation;

/// <summary>
/// The periodic reconciliation trigger (Sprint M2.6 D8), replacing <c>PlatformReconciliationWorker</c>'s
/// old fixed-loop-that-only-checked-Docker-overview (Sprint M2.0-era). Every tick calls
/// <see cref="IReconciliationScheduler.RequestReconciliationAsync"/> with
/// <see cref="ReconciliationTrigger.Periodic"/> — the scheduler itself (not this worker) is what
/// guarantees overlapping cycles never run concurrently (D8), so this class contains no
/// concurrency logic of its own, only the timer.
/// </summary>
public sealed class ReconciliationSchedulerHostedService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(5);

    private readonly IReconciliationScheduler scheduler;
    private readonly ILogger<ReconciliationSchedulerHostedService> logger;

    public ReconciliationSchedulerHostedService(IReconciliationScheduler scheduler, ILogger<ReconciliationSchedulerHostedService> logger)
    {
        this.scheduler = scheduler;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var results = await scheduler.RequestReconciliationAsync(ReconciliationTrigger.Periodic, stoppingToken);
                foreach (var result in results.Where(r => r.DriftReport.HasCriticalOrSecurityDrift))
                {
                    logger.LogWarning(
                        "Reconciliation for service {ServiceId} in {EnvironmentId} found Critical/Security drift: {Count} finding(s).",
                        result.DriftReport.ServiceId, result.DriftReport.EnvironmentId, result.DriftReport.Findings.Count);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Periodic reconciliation cycle failed.");
            }

            try
            {
                await Task.Delay(Interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
