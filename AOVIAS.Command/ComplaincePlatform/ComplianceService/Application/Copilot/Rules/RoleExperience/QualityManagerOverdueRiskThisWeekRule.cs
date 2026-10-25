using ComplianceService.Application.Copilot.Models;
using ComplianceService.Domain.Copilot;
using ComplianceService.Domain.Copilot.Enums;

namespace ComplianceService.Application.Copilot.Rules.RoleExperience;

/// <summary>Phase 3 Sprint 3 — QM NBA: work becoming overdue this week (SLA at-risk / warning).</summary>
public sealed class QualityManagerOverdueRiskThisWeekRule : RecommendationRuleBase
{
    public override string RuleName => "role.qm_overdue_risk_this_week";
    public override RecommendationCategory Category => RecommendationCategory.Task;
    public override IReadOnlyList<CopilotPersona> TargetPersonas => [CopilotPersona.QualityManagerAssistant];

    public override Task<IReadOnlyList<RecommendationCandidate>> EvaluateAsync(
        RecommendationRuleContext context,
        CancellationToken cancellationToken = default)
    {
        var results = new List<RecommendationCandidate>();
        var sla = context.SlaMetrics;
        var atRisk = sla?.AtRiskCount ?? 0;
        var warning = sla?.WarningCount ?? 0;
        var evidenceRejected = context.EvidenceSummary?.Rejected ?? 0;
        var pendingEvidence = context.EvidenceSummary?.PendingReview ?? 0;
        if (atRisk <= 0 && warning <= 0 && evidenceRejected <= 0)
            return Task.FromResult<IReadOnlyList<RecommendationCandidate>>(results);

        var rationale = MakeRationale(
            RuleName,
            metrics: new()
            {
                ["SlaAtRisk"] = atRisk.ToString(),
                ["SlaWarning"] = warning.ToString(),
                ["EvidenceRejected"] = evidenceRejected.ToString(),
                ["EvidencePendingReview"] = pendingEvidence.ToString(),
            },
            scope: context.Scope);

        results.AddRange(BuildForAllPersonas(
            severity: atRisk >= 5 || evidenceRejected >= 3
                ? RecommendationSeverity.High
                : RecommendationSeverity.Medium,
            title: "Clear items becoming overdue this week",
            rationale: $"{atRisk} SLA at-risk, {warning} warnings, {evidenceRejected} rejected evidence items need QM follow-up.",
            expectedOutcome: "This week's due work is reassigned or completed before SLA breach.",
            riskIfIgnored: "At-risk items tip into breached SLA and CAPA escalation.",
            rationaleData: rationale,
            deepLink: "/app/tasks?filter=overdue",
            expiresAtUtc: DateTime.UtcNow.AddDays(1)));

        return Task.FromResult<IReadOnlyList<RecommendationCandidate>>(results);
    }
}
