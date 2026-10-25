using ComplianceService.Application.Copilot.Models;
using ComplianceService.Domain.Copilot;
using ComplianceService.Domain.Copilot.Enums;

namespace ComplianceService.Application.Copilot.Rules.RoleExperience;

/// <summary>Phase 3 Sprint 1 stub — department head SLA / task backlog cue.</summary>
public sealed class DepartmentHeadTaskBacklogRule : RecommendationRuleBase
{
    public override string RuleName => "role.dept_head_task_backlog";
    public override RecommendationCategory Category => RecommendationCategory.Department;
    public override IReadOnlyList<CopilotPersona> TargetPersonas => [CopilotPersona.DepartmentHeadAssistant];

    public override Task<IReadOnlyList<RecommendationCandidate>> EvaluateAsync(
        RecommendationRuleContext context,
        CancellationToken cancellationToken = default)
    {
        var results = new List<RecommendationCandidate>();
        var sla = context.SlaMetrics;
        var atRisk = sla?.DepartmentSlaCompliance?
            .Count(d => d.Count < 70) ?? 0;
        var unassigned = context.DeficiencyMetrics?.AuditOriginOpen ?? 0;
        var breached = sla?.BreachedCount ?? 0;

        if (atRisk <= 0 && unassigned <= 0 && breached <= 0)
            return Task.FromResult<IReadOnlyList<RecommendationCandidate>>(results);

        var rationale = MakeRationale(
            RuleName,
            metrics: new()
            {
                ["AtRiskDepartments"] = atRisk.ToString(),
                ["AuditOriginOpen"] = unassigned.ToString(),
                ["BreachedCount"] = breached.ToString(),
            },
            scope: context.Scope);

        results.AddRange(BuildForAllPersonas(
            severity: atRisk >= 2 || breached >= 3 ? RecommendationSeverity.High : RecommendationSeverity.Medium,
            title: "Department Head: clear team backlog and SLA risk",
            rationale: $"{atRisk} departments below SLA threshold; {breached} SLA breaches; {unassigned} audit-origin items need owners.",
            expectedOutcome: "Department tasks are assigned and SLA breaches are prevented.",
            riskIfIgnored: "Department scores drag hospital readiness and trigger escalations.",
            rationaleData: rationale,
            deepLink: "/app/tasks",
            expiresAtUtc: DateTime.UtcNow.AddDays(2)));

        return Task.FromResult<IReadOnlyList<RecommendationCandidate>>(results);
    }
}
