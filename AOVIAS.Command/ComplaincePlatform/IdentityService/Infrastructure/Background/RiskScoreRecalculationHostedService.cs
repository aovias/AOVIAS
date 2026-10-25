using IdentityService.Application.Contracts.Security;
using IdentityService.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Infrastructure.Background;

/// <summary>Periodic risk score recalculation for users with recent login activity.</summary>
public sealed class RiskScoreRecalculationHostedService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<RiskScoreRecalculationHostedService> _logger;

    public RiskScoreRecalculationHostedService(IServiceProvider services, ILogger<RiskScoreRecalculationHostedService> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RecalculateAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Risk score recalculation job failed");
            }

            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }

    private async Task RecalculateAsync(CancellationToken cancellationToken)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var risk = scope.ServiceProvider.GetRequiredService<IRiskScoringService>();
        var since = DateTime.UtcNow.AddDays(-7);

        var userIds = await db.LoginAttempts
            .Where(a => a.Success && a.CreatedAt >= since && a.UserId != null)
            .Select(a => a.UserId!.Value)
            .Distinct()
            .Take(500)
            .ToListAsync(cancellationToken);

        foreach (var userId in userIds)
            await risk.CalculateAndPersistAsync(userId, cancellationToken);

        if (userIds.Count > 0)
            _logger.LogInformation("Risk recalculation completed for {Count} users", userIds.Count);
    }
}
