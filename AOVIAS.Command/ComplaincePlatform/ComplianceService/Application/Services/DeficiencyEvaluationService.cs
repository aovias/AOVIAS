using ComplianceService.Application.Contracts;
using ComplianceService.Domain;
using ComplianceService.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace ComplianceService.Application.Services;

/// <summary>
/// Orchestrates deficiency evaluation for a single obligation:
/// 1. Runs the rule engine to determine which deficiencies are triggered.
/// 2. Upserts ComplianceDeficiency records (create / update / close).
/// 3. Dispatches domain events via IDomainEventDispatcher.
/// Persistence is handled by the calling command handler (SaveChanges not called here).
/// </summary>
public class DeficiencyEvaluationService : IDeficiencyEvaluationService
{
    private readonly IDeficiencyRuleEngine _ruleEngine;
    private readonly IDeficiencySeverityEngine _severityEngine;
    private readonly IDeficiencyRepository _repository;
    private readonly IDomainEventDispatcher _eventDispatcher;
    private readonly ISlaEvaluationService _slaEvaluation;
    private readonly ILogger<DeficiencyEvaluationService> _logger;

    public DeficiencyEvaluationService(
        IDeficiencyRuleEngine ruleEngine,
        IDeficiencySeverityEngine severityEngine,
        IDeficiencyRepository repository,
        IDomainEventDispatcher eventDispatcher,
        ISlaEvaluationService slaEvaluation,
        ILogger<DeficiencyEvaluationService> logger)
    {
        _ruleEngine      = ruleEngine;
        _severityEngine  = severityEngine;
        _repository      = repository;
        _eventDispatcher = eventDispatcher;
        _slaEvaluation   = slaEvaluation;
        _logger          = logger;
    }

    public async Task<IReadOnlyList<ComplianceDeficiency>> EvaluateAsync(
        OrganizationComplianceObligation obligation,
        CancellationToken ct = default)
    {
        var triggeredRules = await _ruleEngine.GetApplicableRulesAsync(obligation, ct);

        var triggeredTypes = triggeredRules
            .Select(r => r.DeficiencyType)
            .ToHashSet();

        var processed = new List<ComplianceDeficiency>();

        // 1. Handle triggered rules (create or update)
        foreach (var rule in triggeredRules)
        {
            var severity = _severityEngine.CalculateSeverity(
                rule.DeficiencyType,
                obligation.MaturityLevel,
                obligation.RiskCategory);

            var existing = await _repository.FindOpenAsync(obligation.Id, rule.DeficiencyType, ct);

            if (existing != null)
            {
                existing.UpdateEvaluation(severity, updatedBy: "evaluation-engine");
                processed.Add(existing);

                _logger.LogDebug(
                    "DeficiencyEvaluation: Updated existing deficiency {DefId} ({Type}) for obligation {OblId}.",
                    existing.Id, rule.DeficiencyType, obligation.Id);
            }
            else
            {
                var code = GenerateCode(obligation, rule.DeficiencyType);

                var deficiency = ComplianceDeficiency.Create(
                    tenantId:              obligation.TenantId,
                    organizationId:        obligation.OrganizationId,
                    hospitalId:            obligation.HospitalId,
                    frameworkId:           obligation.FrameworkId,
                    objectiveElementId:    obligation.ObjectiveElementId,
                    complianceObligationId: obligation.Id,
                    deficiencyCode:        code,
                    title:                 rule.Title,
                    description:           rule.Description,
                    deficiencyType:        rule.DeficiencyType,
                    severity:              severity,
                    maturityLevel:         obligation.MaturityLevel,
                    riskCategory:          obligation.RiskCategory,
                    source:                "EvaluationEngine",
                    createdBy:             "evaluation-engine");

                await _repository.AddAsync(deficiency, ct);
                await _slaEvaluation.AssignSlaToDeficiencyAsync(deficiency, ct);
                processed.Add(deficiency);

                _logger.LogInformation(
                    "DeficiencyEvaluation: Created deficiency {Code} ({Type}) Severity={Sev} for obligation {OblId}.",
                    code, rule.DeficiencyType, severity, obligation.Id);
            }
        }

        // 2. Close deficiencies whose rules are no longer triggered
        var openDeficiencies = await _repository.GetOpenByObligationAsync(obligation.Id, ct);

        foreach (var open in openDeficiencies)
        {
            if (triggeredTypes.Contains(open.DeficiencyType))
            {
                continue;
            }

            // DEF-M5-001 — the rule no longer fires, but that is a proposal to close, not a right to.
            //
            // A deficiency under an active CAPA still has to be verified first. Uploading evidence
            // clears the "evidence missing" rule immediately, so without this check the engine tried
            // to close a deficiency that was still InProgress, EnsureCloseable() threw, and the
            // exception aborted the whole evidence upload — including the very transition
            // (Uploaded → EvidenceSubmitted) that would have started it toward Verified.
            //
            // Skipping is not a bypass: the deficiency stays open and is closed later by the
            // verification path once evidence is approved.
            if (!open.CanClose())
            {
                _logger.LogInformation(
                    "DeficiencyEvaluation: Deficiency {DefId} ({Type}) is resolved by rules but not yet "
                    + "closeable (status {Status}); leaving open for the verification path.",
                    open.Id, open.DeficiencyType, open.Status);
                continue;
            }

            open.Close(closedBy: "evaluation-engine");
            processed.Add(open);

            _logger.LogInformation(
                "DeficiencyEvaluation: Closed resolved deficiency {DefId} ({Type}) for obligation {OblId}.",
                open.Id, open.DeficiencyType, obligation.Id);
        }

        // 3. Dispatch all domain events from processed deficiencies
        var allEvents = processed
            .SelectMany(d => d.DomainEvents)
            .ToList();

        if (allEvents.Count > 0)
        {
            await _eventDispatcher.DispatchAsync(allEvents, ct);
            foreach (var d in processed)
                d.ClearDomainEvents();
        }

        return processed;
    }

    private static string GenerateCode(OrganizationComplianceObligation obligation, DeficiencyType type)
    {
        var year   = DateTime.UtcNow.Year;
        var seq    = Guid.NewGuid().ToString()[..8].ToUpperInvariant();
        var prefix = type switch
        {
            DeficiencyType.Implementation => "IMP",
            DeficiencyType.Evidence       => "EVD",
            DeficiencyType.Verification   => "VER",
            DeficiencyType.Assessment     => "ASS",
            DeficiencyType.Maturity       => "MAT",
            _                             => "DEF"
        };
        return $"DEF-{prefix}-{year}-{seq}";
    }
}
