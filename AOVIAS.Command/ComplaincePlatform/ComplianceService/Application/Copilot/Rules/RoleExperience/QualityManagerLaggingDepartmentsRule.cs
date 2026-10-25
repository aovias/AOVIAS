using ComplianceService.Application.Copilot.Models;
using ComplianceService.Domain.Copilot;
using ComplianceService.Domain.Copilot.Enums;

namespace ComplianceService.Application.Copilot.Rules.RoleExperience;

/// <summary>Phase 3 Sprint 3 — QM NBA: lagging departments on CAPA / SLA performance.</summary>
public sealed class QualityManagerLaggingDepartmentsRule : RecommendationRuleBase
{
    private const double DepartmentRiskThreshold = 65.0;

    public override string RuleName => "role.qm_lagging_departments";
    public override RecommendationCategory Category => RecommendationCategory.Department;
    public override IReadOnlyList<CopilotPersona> TargetPersonas => [CopilotPersona.QualityManagerAssistant];

    public override Task<IReadOnlyList<RecommendationCandidate>> EvaluateAsync(
        RecommendationRuleContext context,
        CancellationToken cancellationToken = default)
    {
        var results = new List<RecommendationCandidate>();
        var sla = context.SlaMetrics;
        var overdueByDept = context.CapaMetrics?.DepartmentOverdueCapas ?? [];

        var laggingSla = sla?.DepartmentSlaCompliance?
            .Where(d => d.Count < DepartmentRiskThreshold)
            .OrderBy(d => d.Count)
            .Take(5)
            .ToList() ?? [];

        var laggingCapa = overdueByDept
            .Where(d => d.Count > 0)
            .OrderByDescending(d => d.Count)
            .Take(5)
            .ToList();

        if (laggingSla.Count == 0 && laggingCapa.Count == 0)
            return Task.FromResult<IReadOnlyList<RecommendationCandidate>>(results);

        var names = string.Join(", ",
            laggingSla.Select(d => d.Label)
                .Concat(laggingCapa.Select(d => d.Label))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(3));

        var rationale = MakeRationale(
            RuleName,
            metrics: new()
            {
                ["LaggingSlaDepartments"] = laggingSla.Count.ToString(),
                ["DepartmentsWithOverdueCapas"] = laggingCapa.Count.ToString(),
                ["Departments"] = names,
            },
            scope: context.Scope);

        results.AddRange(BuildForAllPersonas(
            severity: laggingSla.Count >= 2 || laggingCapa.Count >= 2
                ? RecommendationSeverity.High
                : RecommendationSeverity.Medium,
            title: "Lagging departments need QM intervention",
            rationale: $"Focus today on: {names}.",
            expectedOutcome: "QM coaches lagging departments and rebalances assignment load.",
            riskIfIgnored: "Department lag compounds into hospital-wide readiness decline.",
            rationaleData: rationale,
            deepLink: "/app/analytics/departments",
            expiresAtUtc: DateTime.UtcNow.AddDays(2)));

        return Task.FromResult<IReadOnlyList<RecommendationCandidate>>(results);
    }
}
