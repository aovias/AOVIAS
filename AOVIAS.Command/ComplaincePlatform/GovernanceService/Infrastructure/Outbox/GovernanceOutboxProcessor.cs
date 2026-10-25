using GovernanceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GovernanceService.Infrastructure.Outbox;

public class GovernanceOutboxProcessor(
    IServiceProvider serviceProvider,
    ILogger<GovernanceOutboxProcessor> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<GovernanceDbContext>();
                var pending = await db.OutboxMessages
                    .Where(m => m.ProcessedOn == null)
                    .OrderBy(m => m.OccurredOn)
                    .Take(50)
                    .ToListAsync(stoppingToken);

                foreach (var message in pending)
                {
                    message.ProcessedOn = DateTime.UtcNow;
                    logger.LogInformation("Governance outbox processed {Type} for aggregate {AggregateId}", message.Type, message.AggregateId);
                }

                if (pending.Count > 0)
                    await db.SaveChangesAsync(stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogWarning(ex, "Governance outbox processor iteration failed");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
