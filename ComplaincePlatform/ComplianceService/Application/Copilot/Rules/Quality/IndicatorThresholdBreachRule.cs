using ComplianceService.Application.Copilot.Models;
using ComplianceService.Domain.Copilot;
using ComplianceService.Domain.Copilot.Enums;
using ComplianceService.Domain.Enums;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ComplianceService.Application.Copilot.Rules.Quality;

/// <summary>Sprint 17 — Advisory recommendation when clinical indicators breach thresholds.</summary>
public sealed class IndicatorThresholdBreachRule : RecommendationRuleBase
{
    private readonly ComplianceDbContext _db;

    public IndicatorThresholdBreachRule(ComplianceDbContext db) => _db = db;

    public override string RuleName => "quality.indicator_threshold_breach";
    public override RecommendationCategory Category => RecommendationCategory.Analytics;
    public override IReadOnlyList<CopilotPersona> TargetPersonas =>
        [CopilotPersona.QualityManagerAssistant, CopilotPersona.ExecutiveAdvisor, CopilotPersona.CapaAdvisor];

    public override async Task<IReadOnlyList<RecommendationCandidate>> EvaluateAsync(
        RecommendationRuleContext context,
        CancellationToken cancellationToken = default)
    {
        var results = new List<RecommendationCandidate>();
        if (context.Scope.OrganizationId == Guid.Empty) return results;

        var breached = await _db.ClinicalIndicators.AsNoTracking()
            .Where(i => i.TenantId == context.Scope.OrganizationId
                && i.Status == IndicatorStatus.Active
                && (i.ThresholdStatus == ThresholdBand.Red || i.ThresholdStatus == ThresholdBand.Critical))
            .OrderByDescending(i => i.RiskLevel)
            .Take(5)
            .ToListAsync(cancellationToken);

        if (breached.Count == 0) return results;

        foreach (var persona in TargetPersonas)
        {
            var top = breached.First();
            results.Add(Build(
                persona: persona.ToString(),
                severity: RecommendationSeverity.High,
                title: $"{breached.Count} clinical indicator{(breached.Count != 1 ? "s" : "")} breached threshold",
                rationale: $"Top breach: {top.Name} ({top.ThresholdStatus}). Threshold breaches require investigation — never auto-remediate.",
                expectedOutcome: "Leadership review and targeted investigation improve quality outcomes and accreditation readiness.",
                riskIfIgnored: "Sustained indicator breaches threaten patient safety and accreditation readiness.",
                rationaleData: MakeRationale(RuleName, metrics: new() { ["BreachedCount"] = breached.Count.ToString() }, scope: context.Scope),
                sourceEntityType: "ClinicalIndicator",
                sourceEntityId: top.Id,
                deepLink: $"/app/quality-intelligence/indicators?selected={top.Id}",
                expiresAtUtc: DateTime.UtcNow.AddHours(24)));
        }

        return results;
    }
}
