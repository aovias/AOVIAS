using ComplianceService.Application.Contracts;
using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ComplianceService.Infrastructure.Services;

/// <summary>
/// Fire-and-forget background snapshot recalculation using a fresh DI scope per run.
/// </summary>
public sealed class ComplianceSnapshotRecalculationQueue : IComplianceSnapshotRecalculationQueue
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IBackgroundJobClient _backgroundJobs;
    private readonly ILogger<ComplianceSnapshotRecalculationQueue> _logger;

    public ComplianceSnapshotRecalculationQueue(
        IServiceScopeFactory scopeFactory,
        IBackgroundJobClient backgroundJobs,
        ILogger<ComplianceSnapshotRecalculationQueue> logger)
    {
        _scopeFactory = scopeFactory;
        _backgroundJobs = backgroundJobs;
        _logger = logger;
    }

    /// <inheritdoc />
    public void EnqueueMaturityRecalc(Guid hospitalId, Guid organizationId)
    {
        if (hospitalId == Guid.Empty || organizationId == Guid.Empty) return;

        try
        {
            // Enqueue as a Hangfire background job so it survives process restarts.
            _backgroundJobs.Enqueue<IComplianceMaturityAggregationService>(
                svc => svc.GetDashboardAsync(hospitalId, organizationId, CancellationToken.None));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to enqueue CMI recalculation for HospitalId={HospitalId}; falling back to fire-and-forget",
                hospitalId);

            // Fallback: inline Task.Run if Hangfire is unavailable
            _ = Task.Run(async () =>
            {
                try
                {
                    await using var scope = _scopeFactory.CreateAsyncScope();
                    var svc = scope.ServiceProvider.GetRequiredService<IComplianceMaturityAggregationService>();
                    await svc.GetDashboardAsync(hospitalId, organizationId, CancellationToken.None);
                }
                catch (Exception inner)
                {
                    _logger.LogError(inner,
                        "Fallback CMI recalculation failed for HospitalId={HospitalId}", hospitalId);
                }
            });
        }
    }

    /// <inheritdoc />
    public void EnqueueHospitalFramework(Guid hospitalId, Guid frameworkId)
    {
        if (hospitalId == Guid.Empty || frameworkId == Guid.Empty)
        {
            return;
        }

        _ = Task.Run(async () =>
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var svc = scope.ServiceProvider.GetRequiredService<IComplianceFrameworkSnapshotRecalculationService>();
                await svc.RecalculateAsync(hospitalId, frameworkId, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Background snapshot recalculation failed for HospitalId={HospitalId} FrameworkId={FrameworkId}",
                    hospitalId,
                    frameworkId);
            }
        });
    }
}
