using ComplianceService.Application.Copilot.Models;
using ComplianceService.Domain.Copilot;
using ComplianceService.Domain.Copilot.Enums;

namespace ComplianceService.Application.Copilot.Rules.RoleExperience;

/// <summary>Phase 3 Sprint 1 stub — consultant portfolio readiness risk.</summary>
public sealed class ConsultantPortfolioRiskRule : RecommendationRuleBase
{
    public override string RuleName => "role.consultant_portfolio_risk";
    public override RecommendationCategory Category => RecommendationCategory.Accreditation;
    public override IReadOnlyList<CopilotPersona> TargetPersonas => [CopilotPersona.ExecutiveAdvisor];

    public override Task<IReadOnlyList<RecommendationCandidate>> EvaluateAsync(
        RecommendationRuleContext context,
        CancellationToken cancellationToken = default)
    {
        var results = new List<RecommendationCandidate>();
        var readiness = context.ReadinessMetrics?.JourneyAccreditationReadiness ?? 100m;
        var blocked = context.StageEvaluations?.Count(e => !e.CriteriaMet) ?? 0;

        if (readiness >= 80m && blocked <= 0)
            return Task.FromResult<IReadOnlyList<RecommendationCandidate>>(results);

        var rationale = MakeRationale(
            RuleName,
            metrics: new()
            {
                ["ReadinessScore"] = readiness.ToString("F1"),
                ["BlockedStages"] = blocked.ToString(),
            },
            scope: context.Scope);

        results.AddRange(BuildForAllPersonas(
            severity: readiness < 60m ? RecommendationSeverity.Critical : RecommendationSeverity.High,
            title: "Consultant: portfolio readiness gaps need a remediation plan",
            rationale: $"Readiness at {readiness:F1}% with {blocked} blocked maturity stages.",
            expectedOutcome: "Consultant produces a focused remediation plan for the next assessment window.",
            riskIfIgnored: "Assessment windows are missed and client confidence drops.",
            rationaleData: rationale,
            deepLink: "/app/compliance-maturity",
            expiresAtUtc: DateTime.UtcNow.AddDays(3)));

        return Task.FromResult<IReadOnlyList<RecommendationCandidate>>(results);
    }
}
