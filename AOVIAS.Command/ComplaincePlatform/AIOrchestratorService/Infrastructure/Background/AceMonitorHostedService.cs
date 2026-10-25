using AIOrchestratorService.Application.AI.Ace;

namespace AIOrchestratorService.Infrastructure.Background;

/// <summary>
/// Periodically scans configured hospital context and creates prioritized ACE insights (advisory only).
/// </summary>
public sealed class AceMonitorHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AceMonitorHostedService> _logger;

    public AceMonitorHostedService(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<AceMonitorHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(20), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var hospitalIdText = _configuration["AIAce:DefaultHospitalId"]
                    ?? _configuration["AIKnowledge:DefaultHospitalId"];

                if (Guid.TryParse(hospitalIdText, out var hospitalId) && hospitalId != Guid.Empty)
                {
                    Guid? frameworkId = null;
                    var fwText = _configuration["AIAce:DefaultFrameworkId"]
                        ?? _configuration["AIKnowledge:DefaultFrameworkId"];
                    if (Guid.TryParse(fwText, out var fw))
                    {
                        frameworkId = fw;
                    }

                    using var scope = _scopeFactory.CreateScope();
                    var ace = scope.ServiceProvider.GetRequiredService<IAceEngine>();
                    var created = await ace.ScanAndCreateAsync(hospitalId, frameworkId, stoppingToken);
                    _logger.LogInformation(
                        "ACE monitor cycle complete for hospital {HospitalId}; created {Count}",
                        hospitalId,
                        created.Count);
                }
                else
                {
                    _logger.LogDebug("ACE monitor skipped — AIAce:DefaultHospitalId not configured");
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "ACE monitor cycle failed");
            }

            var minutes = 30;
            if (int.TryParse(_configuration["AIAce:MonitorIntervalMinutes"], out var configured) && configured > 0)
            {
                minutes = Math.Clamp(configured, 5, 1440);
            }

            await Task.Delay(TimeSpan.FromMinutes(minutes), stoppingToken);
        }
    }
}
