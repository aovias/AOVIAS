using Aovias.Runtime.Agent.Application.Deployment;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aovias.Runtime.Agent.Infrastructure.Deployment;

/// <summary>
/// Background worker that executes one platform deployment at a time.
/// </summary>
public sealed class PlatformDeploymentWorker : BackgroundService
{
    private readonly IServiceScopeFactory scopeFactory;
    private readonly ILogger<PlatformDeploymentWorker> logger;

    public PlatformDeploymentWorker(IServiceScopeFactory scopeFactory, ILogger<PlatformDeploymentWorker> logger)
    {
        this.scopeFactory = scopeFactory;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<PlatformDeploymentService>();
                service.RecoverInterruptedDeployments();
                await service.ProcessNextQueuedDeploymentAsync(stoppingToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Platform deployment worker iteration failed.");
            }

            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }
    }
}
