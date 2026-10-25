using ComplianceService.Application.Copilot.Models;
using ComplianceService.Domain.Copilot;
using ComplianceService.Domain.Copilot.Enums;

namespace ComplianceService.Application.Copilot.Rules.Executive;

/// <summary>
/// Generates a recommendation when departments have poor SLA compliance performance.
/// Uses department-level SLA compliance data to identify at-risk departments.
/// Data source: <see cref="SlaEscalationMetricsDto.DepartmentSlaCompliance"/>.
/// </summary>
public sealed class DepartmentRiskRule : RecommendationRuleBase
{
    private const double DepartmentRiskThreshold = 60.0;  // departments below 60% SLA compliance

    public override string RuleName => "executive.department_sla_risk";
    public override RecommendationCategory Category => RecommendationCategory.Department;
    public override IReadOnlyList<CopilotPersona> TargetPersonas =>
        [CopilotPersona.ExecutiveAdvisor, CopilotPersona.DepartmentHeadAssistant];

    public override Task<IReadOnlyList<RecommendationCandidate>> EvaluateAsync(
        RecommendationRuleContext context,
        CancellationToken cancellationToken = default)
    {
        var results = new List<RecommendationCandidate>();
        var sla = context.SlaMetrics;
        if (sla is null || !sla.DepartmentSlaCompliance.Any())
            return Task.FromResult<IReadOnlyList<RecommendationCandidate>>(results);

        var atRiskDepts = sla.DepartmentSlaCompliance
            .Where(d => double.TryParse(d.Count.ToString(), out var v) && v < DepartmentRiskThreshold)
            .OrderBy(d => d.Count)
            .Take(5)
            .ToList();

        if (!atRiskDepts.Any()) return Task.FromResult<IReadOnlyList<RecommendationCandidate>>(results);

        var severity = atRiskDepts.Count >= 3 ? RecommendationSeverity.High : RecommendationSeverity.Medium;
        var deptNames = string.Join(", ", atRiskDepts.Select(d => d.Label).Take(3));

        var rationale = MakeRationale(
            RuleName,
            metrics: new()
            {
                ["AtRiskDepartments"] = atRiskDepts.Count.ToString(),
                ["Departments"]       = deptNames,
                ["SlaThreshold"]      = $"{DepartmentRiskThreshold}%",
            },
            sources: atRiskDepts.Select(d => new RationaleSourceEntity
            {
                EntityType = "Department",
                EntityId   = string.Empty,
                EntityRef  = d.Label,
            }).ToList(),
            scope: context.Scope);

        foreach (var persona in TargetPersonas)
        {
            results.Add(Build(
                persona: persona.ToString(),
                severity: severity,
                title: $"{atRiskDepts.Count} department{(atRiskDepts.Count != 1 ? "s" : "")} at compliance risk — SLA compliance below threshold",
                rationale: $"{atRiskDepts.Count} departments are below the {DepartmentRiskThreshold}% SLA compliance threshold: {deptNames}.",
                expectedOutcome: "Department-level intervention plans improve SLA compliance scores.",
                riskIfIgnored: "Systemic department-level failures are a leading indicator of broader compliance failure.",
                rationaleData: rationale,
                deepLink: "/analytics/departments",
                expiresAtUtc: DateTime.UtcNow.AddDays(5)));
        }

        return Task.FromResult<IReadOnlyList<RecommendationCandidate>>(results);
    }
}
