using ComplianceService.Application.Commands;
using ComplianceService.Application.Contracts;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ComplianceService.Application.Services;

/// <summary>
/// Loads the obligation, runs the deficiency evaluation engine, persists results,
/// and triggers the outbox processor to propagate events to the snapshot pipeline.
/// </summary>
public class EvaluateObligationDeficienciesCommandHandler
{
    private readonly ComplianceDbContext _db;
    private readonly IDeficiencyEvaluationService _evaluationService;
    private readonly InProcessOutboxProcessor _outboxProcessor;
    private readonly ILogger<EvaluateObligationDeficienciesCommandHandler> _logger;

    public EvaluateObligationDeficienciesCommandHandler(
        ComplianceDbContext db,
        IDeficiencyEvaluationService evaluationService,
        InProcessOutboxProcessor outboxProcessor,
        ILogger<EvaluateObligationDeficienciesCommandHandler> logger)
    {
        _db                = db;
        _evaluationService = evaluationService;
        _outboxProcessor   = outboxProcessor;
        _logger            = logger;
    }

    public async Task HandleAsync(
        EvaluateObligationDeficienciesCommand command,
        CancellationToken ct = default)
    {
        // Tenant filter deliberately applies here (Wave 2.2C). This loads by Id alone and the
        // handler then creates/updates deficiencies against the result, so bypassing the filter
        // allowed a caller holding another tenant's obligation id to drive writes into that tenant.
        // Unlike its sibling handlers, this one carries no explicit `OrganizationId` predicate to
        // fall back on, so the global filter is the only boundary.
        var obligation = await _db.OrganizationComplianceObligations
            .FirstOrDefaultAsync(o => o.Id == command.ObligationId, ct);

        if (obligation == null)
        {
            _logger.LogWarning(
                "EvaluateDeficiencies: Obligation {Id} not found. Skipping.",
                command.ObligationId);
            return;
        }

        var deficiencies = await _evaluationService.EvaluateAsync(obligation, ct);

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "EvaluateDeficiencies: Processed {Count} deficiencies for obligation {ObligationId} (source={Source}).",
            deficiencies.Count, command.ObligationId, command.Source);

        if (command.FlushOutbox)
        {
            // Flush deficiency domain events from the outbox immediately — this fixes the
            // InProcessOutboxProcessor gap and triggers snapshot recalculation synchronously.
            await _outboxProcessor.ProcessPendingAsync(ct);
        }
    }

    /// <summary>Flushes pending outbox messages after a batch of deficiency evaluations.</summary>
    public Task FlushOutboxAsync(CancellationToken ct = default) =>
        _outboxProcessor.ProcessPendingAsync(ct);
}
