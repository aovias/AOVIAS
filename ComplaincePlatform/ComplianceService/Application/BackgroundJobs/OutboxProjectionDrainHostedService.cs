using ComplianceService.Application.Services;

namespace ComplianceService.Application.BackgroundJobs;

/// <summary>
/// Drains Compliance in-process outbox projection independently of HTTP command paths.
/// Hangfire TaskOverdue emission now also drains after emit; this loop covers enroll and historical backlog.
/// </summary>
public sealed class OutboxProjectionDrainHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<OutboxProjectionDrainHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<InProcessOutboxProcessor>();
                await processor.ProcessPendingAsync(stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogWarning(ex, "Outbox projection drain iteration failed");
            }

            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}
