using Microsoft.Extensions.Options;

namespace ComplianceService.Application.Dashboard.Projection;

/// <summary>
/// Rebuilds dashboard projections from live snapshot builder (recovery / backfill).
/// </summary>
public sealed class DashboardProjectionRebuildService
{
    private readonly IDashboardProjectionEngine _engine;
    private readonly IDashboardProjectionRepository _repository;
    private readonly DashboardProjectionOptions _options;
    private readonly ILogger<DashboardProjectionRebuildService> _logger;

    public DashboardProjectionRebuildService(
        IDashboardProjectionEngine engine,
        IDashboardProjectionRepository repository,
        IOptions<DashboardProjectionOptions> options,
        ILogger<DashboardProjectionRebuildService> logger)
    {
        _engine = engine;
        _repository = repository;
        _options = options.Value;
        _logger = logger;
    }

    public async Task RebuildHospitalAsync(
        Guid organizationId,
        Guid hospitalId,
        Guid? frameworkId = null,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return;
        }

        var scope = DashboardProjectionScope.ForHospital(organizationId, hospitalId, frameworkId);
        await _engine.RebuildAsync(scope, "ManualRebuild", cancellationToken: cancellationToken);
    }

    public async Task RebuildCorruptedAsync(CancellationToken cancellationToken = default)
    {
        // Corruption recovery scans are performed via repository queries in diagnostics endpoint.
        _logger.LogInformation("Dashboard projection corruption recovery invoked");
        await Task.CompletedTask;
    }

    public async Task MarkCorruptedAsync(
        DashboardProjectionScope scope,
        string reason,
        CancellationToken cancellationToken = default)
    {
        await _repository.MarkCorruptedAsync(scope, reason, cancellationToken);
    }
}
