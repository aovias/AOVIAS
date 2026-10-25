using ComplianceService.Application.Copilot.Models;
using ComplianceService.Domain.Copilot;
using ComplianceService.Domain.Copilot.Enums;

namespace ComplianceService.Application.Copilot.Templates;

/// <summary>
/// Template for leadership-facing compliance briefings.
/// Sources: Analytics Hub, Compliance Maturity metrics, Copilot Recommendations.
/// Sections: Executive Summary, What Changed, Current Risks, Readiness Status,
///           Critical Deficiencies, CAPA Health, SLA Health, Escalations,
///           Recommendations, Leadership Attention Items.
/// </summary>
public sealed class ExecutiveComplianceBriefingTemplate : DraftTemplateBase
{
    public override DraftType DraftType => DraftType.ExecutiveComplianceBriefing;
    public override string TemplateName => "Executive Compliance Briefing";
    public override DraftSourceType PrimarySourceType => DraftSourceType.Analytics;

    public override IReadOnlyList<string> SectionNames =>
    [
        "Executive Summary",
        "What Changed",
        "Current Risks",
        "Readiness Status",
        "Critical Deficiencies",
        "CAPA Health",
        "SLA Health",
        "Escalations",
        "Recommendations",
        "Leadership Attention Items",
    ];

    public override string BuildTitle(DraftGenerationContext context)
    {
        var date = context.SnapshotUtc.ToString("MMMM yyyy");
        return $"Executive Compliance Briefing — {date}";
    }

    public override string BuildSummary(DraftGenerationContext context)
    {
        var parts = new List<string>();

        if (context.ReadinessMetrics is { } rdy)
        {
            var score = rdy.JourneyAccreditationReadiness;
            var status = score >= 80 ? "on track" : score >= 60 ? "at risk" : "critical";
            parts.Add($"Accreditation readiness is {status} at {score:F1}%");
        }

        if (context.DeficiencyMetrics is { CriticalOpen: > 0 } def)
            parts.Add($"{def.CriticalOpen} critical deficiencies require leadership attention");

        if (context.SlaMetrics is { BreachedCount: > 0 } sla)
            parts.Add($"{sla.BreachedCount} SLA breaches recorded");

        if (parts.Count == 0)
            parts.Add("Compliance status summary for leadership review");

        return string.Join("; ", parts) + ". " +
               "This briefing is generated from operational data and requires human validation prior to distribution.";
    }

    public override IReadOnlyList<DraftSection> BuildSections(DraftGenerationContext context)
    {
        return
        [
            BuildExecutiveSummary(context),
            BuildWhatChanged(context),
            BuildCurrentRisks(context),
            BuildReadinessStatus(context),
            BuildCriticalDeficiencies(context),
            BuildCapaHealth(context),
            BuildSlaHealth(context),
            BuildEscalations(context),
            BuildRecommendations(context),
            BuildLeadershipAttentionItems(context),
        ];
    }

    // ── Section builders ──────────────────────────────────────────────────────

    private DraftSection BuildExecutiveSummary(DraftGenerationContext ctx)
    {
        var lines = new List<string>
        {
            $"**Compliance Status Report** | Generated: {ctx.SnapshotUtc:yyyy-MM-dd HH:mm} UTC",
            $"**Audience:** Executive Leadership | **Classification:** Internal",
            "",
        };

        if (ctx.ExecutiveAnalytics is { } exec)
        {
            lines.Add(KeyValue("Accreditation Readiness",
                ScoreBadge(exec.AccreditationReadiness.Score)));
            lines.Add(KeyValue("Compliance Framework Readiness",
                ScoreBadge(exec.ComplianceReadiness.Score)));
            lines.Add(KeyValue("Critical Escalations",
                WarningBadge(exec.EscalationExposure.CriticalEscalations, "critical escalations")));
        }
        else if (ctx.ReadinessMetrics is { } rdy)
        {
            lines.Add(KeyValue("Journey Readiness",
                ScoreBadge(rdy.JourneyAccreditationReadiness)));
            lines.Add(KeyValue("Framework Readiness",
                ScoreBadge(rdy.FrameworkAssessmentReadiness)));
        }

        lines.Add("\n> **Important:** This briefing is AI-assisted and based on operational data. " +
                  "Leadership should review with Quality Management before formal distribution.");

        return Section("Executive Summary", string.Join("\n", lines), 1);
    }

    private DraftSection BuildWhatChanged(DraftGenerationContext ctx)
    {
        var lines = new List<string>
        {
            "**Key Developments Since Last Review** *(requires human completion)*",
            "",
            "| Area | Status | Change |",
            "|------|--------|--------|",
        };

        if (ctx.DeficiencyMetrics is { } def)
        {
            lines.Add($"| Deficiencies | {WarningBadge(def.CriticalOpen, "critical")} | *Trend data pending* |");
        }

        if (ctx.CapaMetrics is { } capa)
        {
            lines.Add($"| CAPA | {WarningBadge(capa.OverdueCapas, "overdue")} | *Trend data pending* |");
        }

        if (ctx.SlaMetrics is { } sla)
        {
            lines.Add($"| SLA Compliance | {WarningBadge(sla.BreachedCount, "breached")} | *Trend data pending* |");
        }

        lines.Add("\n> Complete trend analysis requires historical snapshot comparison. " +
                  "Quality Management to supplement with month-on-month trend narrative.");

        return Section("What Changed", string.Join("\n", lines), 2);
    }

    private DraftSection BuildCurrentRisks(DraftGenerationContext ctx)
    {
        var lines = new List<string> { "**Current Risk Areas Requiring Attention:**" };

        var risks = new List<(string area, int severity, string detail)>();

        if (ctx.DeficiencyMetrics is { } def)
        {
            if (def.CriticalOpen > 0)
                risks.Add(("Critical Deficiencies", 1, $"{def.CriticalOpen} critical open deficiencies"));
            if (def.BlockingOpen > 0)
                risks.Add(("Blocking Deficiencies", 2, $"{def.BlockingOpen} blocking deficiencies"));
        }

        if (ctx.CapaMetrics is { OverdueCapas: > 0 } capa)
            risks.Add(("Overdue CAPAs", 2, $"{capa.OverdueCapas} CAPAs overdue"));

        if (ctx.SlaMetrics is { } sla)
        {
            if (sla.BreachedCount > 0)
                risks.Add(("SLA Breaches", 1, $"{sla.BreachedCount} SLA breaches"));
            if (sla.CriticalEscalations > 0)
                risks.Add(("Critical Escalations", 1, $"{sla.CriticalEscalations} critical escalations"));
        }

        foreach (var (area, severity, detail) in risks.OrderBy(r => r.severity))
        {
            var badge = severity == 1 ? "🔴 HIGH" : "🟡 MEDIUM";
            lines.Add($"- {badge} — **{area}:** {detail}");
        }

        if (risks.Count == 0)
            lines.Add("- 🟢 No critical risk areas identified at this time.");

        return Section("Current Risks", string.Join("\n", lines), 3);
    }

    private DraftSection BuildReadinessStatus(DraftGenerationContext ctx)
    {
        var lines = new List<string>();

        if (ctx.ExecutiveAnalytics is { } exec)
        {
            lines.Add("**Accreditation Readiness Dashboard:**");
            lines.Add(KeyValue("Journey Accreditation Readiness",
                ScoreBadge(exec.AccreditationReadiness.Score)));
            lines.Add(KeyValue("Compliance Framework Readiness",
                ScoreBadge(exec.ComplianceReadiness.Score)));
            lines.Add(KeyValue("Deep Link", exec.AccreditationReadiness.DeepLink?.Route ?? "N/A"));
        }
        else if (ctx.ReadinessMetrics is { } rdy)
        {
            lines.Add("**Readiness Scores:**");
            lines.Add(KeyValue("Journey Accreditation", ScoreBadge(rdy.JourneyAccreditationReadiness)));
            lines.Add(KeyValue("Framework Assessment", ScoreBadge(rdy.FrameworkAssessmentReadiness)));
        }
        else
        {
            lines.Add(NotAvailable("Readiness metrics"));
        }

        if (ctx.StageEvaluations is { } stages)
        {
            lines.Add("\n**Stage Progress:**");
            foreach (var stage in stages)
            {
                var blockedBadge = stage.IsBlocked ? " ⚠️ BLOCKED" : "";
                lines.Add($"- **{stage.StageName}:** {stage.CompletionPercent:F0}%{blockedBadge}" +
                          (stage.BlockingIssueCount > 0 ? $" ({stage.BlockingIssueCount} issues)" : ""));
            }
        }

        return Section("Readiness Status", string.Join("\n", lines), 4);
    }

    private DraftSection BuildCriticalDeficiencies(DraftGenerationContext ctx)
    {
        var lines = new List<string>();

        if (ctx.DeficiencyMetrics is { } def)
        {
            lines.Add("**Deficiency Summary:**");
            lines.Add(KeyValue("Total", def.Total.ToString()));
            lines.Add(KeyValue("Open", def.Open.ToString()));
            lines.Add(KeyValue("Critical Open", $"**{WarningBadge(def.CriticalOpen, "critical")}**"));
            lines.Add(KeyValue("Blocking Open", WarningBadge(def.BlockingOpen, "blocking")));
            lines.Add(KeyValue("Recurring Open", WarningBadge(def.RecurringOpen, "recurring")));
            lines.Add(KeyValue("Audit Origin Open", def.AuditOriginOpen.ToString()));
            lines.Add(KeyValue("Escalated Open", def.EscalatedOpen.ToString()));
        }
        else
        {
            lines.Add(NotAvailable("Deficiency metrics"));
        }

        return Section("Critical Deficiencies", string.Join("\n", lines), 5);
    }

    private DraftSection BuildCapaHealth(DraftGenerationContext ctx)
    {
        var lines = new List<string>();

        if (ctx.CapaMetrics is { } capa)
        {
            lines.Add("**CAPA Health Metrics:**");
            lines.Add(KeyValue("Open CAPAs", capa.OpenCapas.ToString()));
            lines.Add(KeyValue("Overdue CAPAs", $"**{WarningBadge(capa.OverdueCapas, "overdue")}**"));
            lines.Add(KeyValue("Effective CAPAs", capa.EffectiveCapas.ToString()));
            lines.Add(KeyValue("Ineffective CAPAs", WarningBadge(capa.IneffectiveCapas, "ineffective")));
            lines.Add(KeyValue("Recurring Issues", WarningBadge(capa.RecurringIssues, "recurring")));
            lines.Add(KeyValue("Pending Reviews", capa.PendingReviews.ToString()));
            lines.Add(KeyValue("Effectiveness Rate",
                $"{capa.CapaEffectivenessRate:P0} *(target: ≥85%)*"));
            lines.Add(KeyValue("Avg Closure Days", $"{capa.AverageClosureDays:F0} days"));
        }
        else if (ctx.GovernanceMetrics?.Capa is { } govCapa)
        {
            lines.Add("**CAPA Governance Summary:**");
            lines.Add(KeyValue("Pending Reviews", govCapa.PendingReviews.ToString()));
            lines.Add(KeyValue("Recurring Issues", WarningBadge(govCapa.RecurringIssues, "recurring")));
            lines.Add(KeyValue("Completion Rate", $"{govCapa.CapaCompletionRate:P0}"));
        }
        else
        {
            lines.Add(NotAvailable("CAPA metrics"));
        }

        return Section("CAPA Health", string.Join("\n", lines), 6);
    }

    private DraftSection BuildSlaHealth(DraftGenerationContext ctx)
    {
        var lines = new List<string>();

        if (ctx.SlaMetrics is { } sla)
        {
            lines.Add("**SLA Compliance Summary:**");
            lines.Add(KeyValue("Total Open with SLA", sla.TotalOpenWithSla.ToString()));
            lines.Add(KeyValue("On Track", $"🟢 {sla.OnTrackCount}"));
            lines.Add(KeyValue("Warning", $"🟡 {sla.WarningCount}"));
            lines.Add(KeyValue("At Risk", $"🟠 {sla.AtRiskCount}"));
            lines.Add(KeyValue("Breached", $"**🔴 {sla.BreachedCount}**"));
            lines.Add(KeyValue("Escalated", WarningBadge(sla.EscalatedCount, "escalated")));
            lines.Add(KeyValue("Breach Rate", $"{sla.BreachRate:P1}"));
            lines.Add(KeyValue("Avg Resolution", $"{sla.AverageResolutionHours:F0} hours"));
        }
        else if (ctx.GovernanceMetrics?.Sla is { } govSla)
        {
            lines.Add("**SLA Governance Summary:**");
            lines.Add(KeyValue("Open Critical Escalations", govSla.OpenCriticalEscalations.ToString()));
            lines.Add(KeyValue("Overdue Deficiencies", WarningBadge(govSla.OverdueDeficiencies, "overdue")));
        }
        else
        {
            lines.Add(NotAvailable("SLA metrics"));
        }

        return Section("SLA Health", string.Join("\n", lines), 7);
    }

    private DraftSection BuildEscalations(DraftGenerationContext ctx)
    {
        var lines = new List<string>();

        if (ctx.ExecutiveAnalytics?.EscalationExposure is { } esc)
        {
            lines.Add("**Escalation Exposure:**");
            lines.Add(KeyValue("Open Escalations", esc.OpenEscalations.ToString()));
            lines.Add(KeyValue("Critical Escalations", $"**{WarningBadge(esc.CriticalEscalations, "critical")}**"));
            lines.Add(KeyValue("Escalation Rate", $"{esc.EscalationRate:F1}%"));
            if (esc.DeepLink is not null)
                lines.Add(KeyValue("View Escalations", esc.DeepLink.Route));
        }
        else if (ctx.SlaMetrics is { } sla)
        {
            lines.Add("**Escalation Summary:**");
            lines.Add(KeyValue("Total Escalated", WarningBadge(sla.EscalatedCount, "escalated")));
            lines.Add(KeyValue("Critical Escalations", WarningBadge(sla.CriticalEscalations, "critical")));
            lines.Add(KeyValue("Escalation Rate", $"{sla.EscalationRate:P1}"));
        }
        else
        {
            lines.Add(NotAvailable("Escalation data"));
        }

        return Section("Escalations", string.Join("\n", lines), 8);
    }

    private DraftSection BuildRecommendations(DraftGenerationContext ctx)
    {
        var lines = new List<string> { "**Priority Actions for Leadership:**" };

        if (ctx.SourceRecommendation is { } rec)
        {
            lines.Add($"\n**Primary Recommendation:** {rec.Title}");
            if (!string.IsNullOrEmpty(rec.ExpectedOutcome))
                lines.Add($"**Expected Outcome:** {rec.ExpectedOutcome}");
        }

        // Generate contextual recommendations based on metrics
        var recs = new List<string>();

        if (ctx.DeficiencyMetrics is { CriticalOpen: > 0 } def)
            recs.Add($"Prioritize closure of {def.CriticalOpen} critical open deficiencies");

        if (ctx.CapaMetrics is { OverdueCapas: > 0 } capa)
            recs.Add($"Review and resolve {capa.OverdueCapas} overdue CAPAs immediately");

        if (ctx.SlaMetrics is { BreachedCount: > 0 } sla)
            recs.Add($"Investigate root causes for {sla.BreachedCount} SLA breaches");

        if (ctx.ReadinessMetrics is { JourneyAccreditationReadiness: < 80 } rdy)
            recs.Add($"Develop readiness improvement plan to address score of {rdy.JourneyAccreditationReadiness:F1}%");

        foreach (var r in recs)
            lines.Add($"- {r}");

        if (recs.Count == 0)
            lines.Add("- Continue monitoring current metrics; no immediate escalation required.");

        return Section("Recommendations", string.Join("\n", lines), 9);
    }

    private DraftSection BuildLeadershipAttentionItems(DraftGenerationContext ctx)
    {
        var lines = new List<string>
        {
            "**Items Requiring Leadership Decision or Escalation:**",
            "",
            "| Priority | Item | Owner | Due Date |",
            "|----------|------|-------|----------|",
        };

        if (ctx.DeficiencyMetrics is { CriticalEscalatedOpen: > 0 } def)
            lines.Add($"| 🔴 HIGH | Review {def.CriticalEscalatedOpen} critical escalated deficiencies | " +
                      "Quality Director | *To be set* |");

        if (ctx.CapaMetrics is { IneffectiveCapas: > 0 } capa)
            lines.Add($"| 🟡 MED | Address {capa.IneffectiveCapas} ineffective CAPAs | " +
                      "CAPA Owner | *To be set* |");

        if (ctx.SlaMetrics is { CriticalEscalations: > 0 } sla)
            lines.Add($"| 🔴 HIGH | Resolve {sla.CriticalEscalations} critical SLA escalations | " +
                      "Operations Director | *To be set* |");

        lines.Add($"| 🔵 INFO | Approve this briefing for distribution | Leadership | " +
                  $"{DateTime.UtcNow.AddDays(3):yyyy-MM-dd} |");

        lines.Add("\n> Owners and due dates must be confirmed by leadership before distribution.");

        return Section("Leadership Attention Items", string.Join("\n", lines), 10);
    }
}
