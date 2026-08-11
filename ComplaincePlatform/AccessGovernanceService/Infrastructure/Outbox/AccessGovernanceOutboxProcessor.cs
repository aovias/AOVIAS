using AccessGovernanceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AccessGovernanceService.Infrastructure.Outbox;

public class AccessGovernanceOutboxProcessor(
    IServiceProvider serviceProvider,
    ILogger<AccessGovernanceOutboxProcessor> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AccessGovernanceDbContext>();
                var pending = await db.OutboxMessages
                    .Where(m => m.ProcessedOn == null)
                    .OrderBy(m => m.OccurredOn)
                    .Take(50)
                    .ToListAsync(stoppingToken);

                foreach (var message in pending)
                {
                    message.ProcessedOn = DateTime.UtcNow;
                    logger.LogInformation("AccessGovernance outbox processed {Type} for aggregate {AggregateId}", message.Type, message.AggregateId);
                }

                if (pending.Count > 0)
                    await db.SaveChangesAsync(stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogWarning(ex, "AccessGovernance outbox processor iteration failed");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
