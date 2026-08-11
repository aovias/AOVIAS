using AIOrchestratorService.Application.AI.Knowledge;

namespace AIOrchestratorService.Infrastructure.Background;

/// <summary>
/// Periodically ensures a baseline knowledge index exists for demo/dev hospitals.
/// Production reindex should be driven by outbox events (document publish, OE change, CAPA close).
/// </summary>
public sealed class KnowledgeIndexerHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<KnowledgeIndexerHostedService> _logger;
    private readonly IConfiguration _configuration;

    public KnowledgeIndexerHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<KnowledgeIndexerHostedService> logger,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(8), stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var hospitalIdText = _configuration["AIKnowledge:DefaultHospitalId"];
                if (Guid.TryParse(hospitalIdText, out var hospitalId) && hospitalId != Guid.Empty)
                {
                    using var scope = _scopeFactory.CreateScope();
                    var search = scope.ServiceProvider.GetRequiredService<IKnowledgeSearchService>();
                    Guid? frameworkId = null;
                    if (Guid.TryParse(_configuration["AIKnowledge:DefaultFrameworkId"], out var fw))
                    {
                        frameworkId = fw;
                    }

                    await search.SeedDemoIndexAsync(hospitalId, frameworkId, stoppingToken);
                    _logger.LogInformation("Knowledge index seed completed for hospital {HospitalId}", hospitalId);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Knowledge indexer cycle failed");
            }

            await Task.Delay(TimeSpan.FromHours(6), stoppingToken);
        }
    }
}
