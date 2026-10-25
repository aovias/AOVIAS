using IdentityService.Infrastructure;

using Microsoft.EntityFrameworkCore;



namespace IdentityService.Infrastructure.Background;



public class IdentityOutboxProcessor : BackgroundService

{

    private const int MaxRetries = 5;

    private readonly IServiceProvider _services;

    private readonly ILogger<IdentityOutboxProcessor> _logger;



    public IdentityOutboxProcessor(IServiceProvider services, ILogger<IdentityOutboxProcessor> logger)

    {

        _services = services;

        _logger = logger;

    }



    protected override async Task ExecuteAsync(CancellationToken stoppingToken)

    {

        await Task.Delay(TimeSpan.FromSeconds(20), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)

        {

            try

            {

                using var scope = _services.CreateScope();

                var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();

                var pending = await db.OutboxMessages

                    .Where(m => m.ProcessedOn == null && m.RetryCount < MaxRetries)

                    .OrderBy(m => m.OccurredOn)

                    .Take(20)

                    .ToListAsync(stoppingToken);



                foreach (var message in pending)

                {

                    try

                    {

                        _logger.LogInformation(

                            "Identity outbox event {Type} for aggregate {AggregateId}",

                            message.Type,

                            message.AggregateId);

                        message.ProcessedOn = DateTime.UtcNow;

                        message.Error = null;

                    }

                    catch (Exception ex)

                    {

                        message.RetryCount++;

                        message.Error = ex.Message;

                        _logger.LogError(ex, "Identity outbox message {MessageId} failed (retry {RetryCount})",

                            message.Id, message.RetryCount);

                    }

                }



                if (pending.Count > 0)

                    await db.SaveChangesAsync(stoppingToken);

            }

            catch (Exception ex)

            {

                _logger.LogError(ex, "Identity outbox processor error");

            }



            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        }

    }

}


