using Microsoft.Extensions.Options;
using OrganizationService.Application.Services;
using OrganizationService.Configuration;

namespace OrganizationService.Infrastructure.Background;

/// <summary>CRP Phase 5 — daily trial lifecycle evaluation worker.</summary>
public sealed class TrialLifecycleHostedService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<TrialLifecycleHostedService> _logger;

    public TrialLifecycleHostedService(IServiceProvider services, ILogger<TrialLifecycleHostedService> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            var intervalHours = 24;
            try
            {
                using var scope = _services.CreateScope();
                var options = scope.ServiceProvider.GetRequiredService<IOptions<TrialLifecycleOptions>>().Value;
                intervalHours = Math.Max(1, options.EvaluationIntervalHours);
                var engine = scope.ServiceProvider.GetRequiredService<TrialLifecycleEngine>();
                var result = await engine.EvaluateAllAsync("Daily", cancellationToken: stoppingToken);
                _logger.LogInformation(
                    "TrialLifecycle Daily run {RunId}: processed={Processed} transitions={Transitions} reminders={Reminders} failures={Failures}",
                    result.RunId, result.ProcessedCount, result.TransitionCount, result.ReminderCount, result.FailureCount);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "TrialLifecycleHostedService iteration failed");
            }

            await Task.Delay(TimeSpan.FromHours(intervalHours), stoppingToken);
        }
    }
}
