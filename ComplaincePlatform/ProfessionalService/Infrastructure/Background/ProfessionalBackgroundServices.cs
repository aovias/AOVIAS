using ProfessionalService.Infrastructure.MasterData;
using ProfessionalService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ProfessionalService.Infrastructure.Background;

public sealed class ProfessionalMasterDataBootstrapHostedService : BackgroundService
{
    private readonly IServiceProvider _services;
    public ProfessionalMasterDataBootstrapHostedService(IServiceProvider services) => _services = services;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ProfessionalDbContext>();
        await ProfessionalMasterDataSeeder.SeedAsync(db, stoppingToken);
    }
}

public class ProfessionalOutboxProcessor : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<ProfessionalOutboxProcessor> _logger;

    public ProfessionalOutboxProcessor(IServiceProvider services, ILogger<ProfessionalOutboxProcessor> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ProfessionalDbContext>();
                var pending = await db.OutboxMessages.Where(m => m.ProcessedOn == null && m.RetryCount < 5)
                    .OrderBy(m => m.OccurredOn).Take(20).ToListAsync(stoppingToken);
                foreach (var msg in pending)
                {
                    _logger.LogInformation("Professional outbox {Type} {AggregateId}", msg.Type, msg.AggregateId);
                    msg.ProcessedOn = DateTime.UtcNow;
                }
                if (pending.Count > 0) await db.SaveChangesAsync(stoppingToken);
            }
            catch (Exception ex) { _logger.LogError(ex, "Professional outbox error"); }
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}
