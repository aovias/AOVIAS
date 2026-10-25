using ComplianceService.Application.Copilot.Models;
using ComplianceService.Domain.Copilot;
using ComplianceService.Domain.Copilot.Enums;

namespace ComplianceService.Application.Copilot.Rules.RoleExperience;

/// <summary>Phase 3 Sprint 2 — Owner NBA: departments with declining / below-threshold SLA performance.</summary>
public sealed class OwnerDecliningDepartmentsRule : RecommendationRuleBase
{
    private const double DepartmentRiskThreshold = 60.0;

    public override string RuleName => "role.owner_declining_departments";
    public override RecommendationCategory Category => RecommendationCategory.Department;
    public override IReadOnlyList<CopilotPersona> TargetPersonas => [CopilotPersona.ExecutiveAdvisor];

    public override Task<IReadOnlyList<RecommendationCandidate>> EvaluateAsync(
        RecommendationRuleContext context,
        CancellationToken cancellationToken = default)
    {
        var results = new List<RecommendationCandidate>();
        var sla = context.SlaMetrics;
        if (sla is null || sla.DepartmentSlaCompliance.Count == 0)
            return Task.FromResult<IReadOnlyList<RecommendationCandidate>>(results);

        var atRisk = sla.DepartmentSlaCompliance
            .Where(d => d.Count < DepartmentRiskThreshold)
            .OrderBy(d => d.Count)
            .Take(5)
            .ToList();

        if (atRisk.Count == 0)
            return Task.FromResult<IReadOnlyList<RecommendationCandidate>>(results);

        var names = string.Join(", ", atRisk.Select(d => d.Label).Take(3));
        var rationale = MakeRationale(
            RuleName,
            metrics: new()
            {
                ["AtRiskDepartments"] = atRisk.Count.ToString(),
                ["Departments"] = names,
                ["SlaThreshold"] = $"{DepartmentRiskThreshold}%",
            },
            scope: context.Scope);

        results.AddRange(BuildForAllPersonas(
            severity: atRisk.Count >= 3 ? RecommendationSeverity.High : RecommendationSeverity.Medium,
            title: $"{atRisk.Count} department{(atRisk.Count == 1 ? "" : "s")} declining on compliance performance",
            rationale: $"Departments below {DepartmentRiskThreshold}% SLA compliance: {names}.",
            expectedOutcome: "Executive coaching and resource reallocation reverse department decline.",
            riskIfIgnored: "Weak departments become systemic accreditation blockers.",
            rationaleData: rationale,
            deepLink: "/app/analytics/departments",
            expiresAtUtc: DateTime.UtcNow.AddDays(3)));

        return Task.FromResult<IReadOnlyList<RecommendationCandidate>>(results);
    }
}
