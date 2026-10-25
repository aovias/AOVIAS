using ComplianceService.Application.Copilot.Models;
using ComplianceService.Domain.Copilot;
using ComplianceService.Domain.Copilot.Enums;

namespace ComplianceService.Application.Copilot.Templates;

/// <summary>
/// Template for Corrective and Preventive Action proposal drafts.
/// Sources: Deficiency data, CAPA records, Recommendation rationale, Root Cause analysis.
/// Sections: Problem Statement, Observed Deficiency, Root Cause Summary,
///           Corrective Action, Preventive Action, Success Criteria,
///           Target Completion, Effectiveness Review Plan.
/// </summary>
public sealed class CapaDraftTemplate : DraftTemplateBase
{
    public override DraftType DraftType => DraftType.CapaDraft;
    public override string TemplateName => "CAPA Proposal Draft";
    public override DraftSourceType PrimarySourceType => DraftSourceType.Deficiency;

    public override IReadOnlyList<string> SectionNames =>
    [
        "Problem Statement",
        "Observed Deficiency",
        "Root Cause Summary",
        "Corrective Action",
        "Preventive Action",
        "Success Criteria",
        "Target Completion",
        "Effectiveness Review Plan",
    ];

    public override string BuildTitle(DraftGenerationContext context)
    {
        if (context.SourceRecommendation is { } rec)
            return $"CAPA Proposal — {rec.Title}";

        if (context.DeficiencyMetrics is { CriticalOpen: > 0 } def)
            return $"CAPA Proposal — {def.CriticalOpen} Critical Open Deficiencies";

        return "CAPA Proposal Draft";
    }

    public override string BuildSummary(DraftGenerationContext context)
    {
        var parts = new List<string>();

        if (context.CapaMetrics is { } capa)
            parts.Add($"{capa.OverdueCapas} overdue CAPAs and {capa.OpenCapas} open CAPAs identified");

        if (context.DeficiencyMetrics is { } def && def.CriticalOpen > 0)
            parts.Add($"{def.CriticalOpen} critical deficiencies require immediate corrective action");

        if (parts.Count == 0)
            parts.Add("Corrective and preventive action required to address identified compliance deficiencies");

        return string.Join(". ", parts) +
               ". This draft is advisory — human review and approval are required before any action is taken.";
    }

    public override IReadOnlyList<DraftSection> BuildSections(DraftGenerationContext context)
    {
        var sections = new List<DraftSection>
        {
            BuildProblemStatement(context),
            BuildObservedDeficiency(context),
            BuildRootCauseSummary(context),
            BuildCorrectiveAction(context),
            BuildPreventiveAction(context),
            BuildSuccessCriteria(context),
            BuildTargetCompletion(context),
            BuildEffectivenessReviewPlan(context),
        };
        return sections;
    }

    // ── Section builders ──────────────────────────────────────────────────────

    private DraftSection BuildProblemStatement(DraftGenerationContext ctx)
    {
        var lines = new List<string>();

        if (ctx.SourceRecommendation is { } rec)
        {
            lines.Add($"**Recommendation Trigger:** {rec.Title}");
            if (!string.IsNullOrEmpty(rec.Description))
                lines.Add($"\n{rec.Description}");
            lines.Add($"\n**Risk if Ignored:** {rec.RiskIfIgnored}");
        }

        if (ctx.DeficiencyMetrics is { } def)
        {
            lines.Add($"\n**Deficiency Status at Time of Generation:**");
            lines.Add(KeyValue("Total Deficiencies", def.Total.ToString()));
            lines.Add(KeyValue("Critical Open", def.CriticalOpen.ToString()));
            lines.Add(KeyValue("Blocking Open", def.BlockingOpen.ToString()));
            lines.Add(KeyValue("Recurring Open", def.RecurringOpen.ToString()));
        }

        if (lines.Count == 0)
            lines.Add("Compliance deficiencies have been identified that require corrective and preventive action to maintain accreditation readiness.");

        return Section("Problem Statement", string.Join("\n", lines), 1);
    }

    private DraftSection BuildObservedDeficiency(DraftGenerationContext ctx)
    {
        var lines = new List<string>();

        if (ctx.DeficiencyMetrics is { } def)
        {
            lines.Add($"**Deficiency Overview** *(snapshot: {ctx.SnapshotUtc:yyyy-MM-dd})*");
            lines.Add(KeyValue("Total Deficiencies", def.Total.ToString()));
            lines.Add(KeyValue("Open", def.Open.ToString()));
            lines.Add(KeyValue("Critical Open", $"{WarningBadge(def.CriticalOpen, "critical")}"));
            lines.Add(KeyValue("Recurring Open", $"{WarningBadge(def.RecurringOpen, "recurring")}"));
            lines.Add(KeyValue("Audit Origin Open", def.AuditOriginOpen.ToString()));
            lines.Add(KeyValue("Escalated Open", def.EscalatedOpen.ToString()));
        }
        else
        {
            lines.Add(NotAvailable("Deficiency metrics"));
        }

        if (ctx.SourceRecommendation?.SourceEntityId.HasValue == true)
        {
            lines.Add($"\n**Source Entity Reference:** `{ctx.SourceRecommendation.SourceEntityId}`");
            lines.Add($"**Entity Type:** {ctx.SourceRecommendation.SourceEntityType ?? "Deficiency"}");
        }

        return Section("Observed Deficiency", string.Join("\n", lines), 2);
    }

    private DraftSection BuildRootCauseSummary(DraftGenerationContext ctx)
    {
        var lines = new List<string>();

        if (ctx.SourceRecommendation?.RationaleJson is { } rationaleJson
            && !string.IsNullOrWhiteSpace(rationaleJson))
        {
            lines.Add("**Machine-Generated Root Cause Analysis (from Recommendation Rationale):**");
            lines.Add($"\n```json\n{rationaleJson}\n```");
            lines.Add("\n> This rationale was generated deterministically from operational data. " +
                      "Human clinical expertise should validate and expand upon this analysis.");
        }

        if (ctx.CapaMetrics is { } capa && capa.RecurringIssues > 0)
        {
            lines.Add($"\n**Recurring Pattern Indicator:** {capa.RecurringIssues} recurring issues detected");
            if (capa.TopRootCauses.Count > 0)
            {
                lines.Add("\n**Top Root Causes by Frequency:**");
                foreach (var rc in capa.TopRootCauses.Take(5))
                    lines.Add($"- {rc.Label}: {rc.Count} occurrences");
            }
        }

        if (lines.Count == 0)
            lines.Add("> Root cause analysis must be completed by the responsible department head " +
                      "prior to CAPA submission. Reference observed deficiency data in the prior section.");

        return Section("Root Cause Summary", string.Join("\n", lines), 3);
    }

    private DraftSection BuildCorrectiveAction(DraftGenerationContext ctx)
    {
        var lines = new List<string>();

        lines.Add("**Proposed Corrective Actions** *(Draft — requires human validation)*");

        if (ctx.SourceRecommendation is { } rec && !string.IsNullOrEmpty(rec.ExpectedOutcome))
        {
            lines.Add($"\n**Expected Outcome:** {rec.ExpectedOutcome}");
        }

        lines.Add("\n**Immediate Actions (0–30 days):**");
        lines.Add("- [ ] Assign a responsible officer for each open critical deficiency");
        lines.Add("- [ ] Conduct root cause analysis workshop with department heads");
        lines.Add("- [ ] Escalate unresolved critical deficiencies to Quality Committee");

        if (ctx.DeficiencyMetrics is { CriticalOpen: > 0 } def)
        {
            lines.Add($"- [ ] Review and remediate {def.CriticalOpen} critical open deficiencies");
        }

        lines.Add("\n**Short-Term Actions (30–90 days):**");
        lines.Add("- [ ] Implement process controls identified in root cause analysis");
        lines.Add("- [ ] Complete corrective action implementation and evidence upload");
        lines.Add("- [ ] Verify effectiveness through re-assessment");

        return Section("Corrective Action", string.Join("\n", lines), 4);
    }

    private DraftSection BuildPreventiveAction(DraftGenerationContext ctx)
    {
        var lines = new List<string>();

        lines.Add("**Proposed Preventive Actions** *(Draft — requires human validation)*");
        lines.Add("\n**Systemic Prevention Measures:**");
        lines.Add("- [ ] Update standard operating procedures to prevent recurrence");
        lines.Add("- [ ] Schedule staff refresher training on affected compliance areas");
        lines.Add("- [ ] Implement early-warning monitoring for leading indicators");

        if (ctx.CapaMetrics is { RecurringIssues: > 0 } capa)
        {
            lines.Add($"- [ ] Address root causes of {capa.RecurringIssues} recurring issues");
            lines.Add("- [ ] Review CAPA effectiveness protocols for recurring pattern areas");
        }

        lines.Add("\n**Monitoring and Review:**");
        lines.Add("- [ ] Establish monthly deficiency trend review cadence");
        lines.Add("- [ ] Set up automated alerts for new critical deficiency creation");
        lines.Add("- [ ] Include in next Management Review agenda");

        return Section("Preventive Action", string.Join("\n", lines), 5);
    }

    private DraftSection BuildSuccessCriteria(DraftGenerationContext ctx)
    {
        var lines = new List<string>();

        lines.Add("**Measurable Success Criteria:**");

        if (ctx.DeficiencyMetrics is { } def)
        {
            lines.Add(KeyValue("Critical deficiency reduction target",
                $"Reduce from {def.CriticalOpen} → 0 within 90 days"));
        }

        if (ctx.CapaMetrics is { } capa)
        {
            lines.Add(KeyValue("CAPA overdue target",
                $"Reduce overdue CAPAs from {capa.OverdueCapas} → 0"));
            lines.Add(KeyValue("Effectiveness rate target",
                $"Maintain CAPA effectiveness ≥ 85% (current: {capa.CapaEffectivenessRate:P0})"));
        }

        if (ctx.ReadinessMetrics is { } rdy)
        {
            lines.Add(KeyValue("Readiness score target",
                $"Improve journey readiness from {rdy.JourneyAccreditationReadiness:F1} → ≥ 80"));
        }

        lines.Add(KeyValue("Recurrence prevention", "No recurrence of same deficiency category within 6 months"));
        lines.Add(KeyValue("Evidence completion", "100% of required corrective action evidence uploaded and verified"));

        return Section("Success Criteria", string.Join("\n", lines), 6);
    }

    private static DraftSection BuildTargetCompletion(DraftGenerationContext ctx)
    {
        var now = DateTime.UtcNow;
        var lines = new List<string>
        {
            "**Proposed Timeline:**",
            KeyValue("Draft Creation Date", now.ToString("yyyy-MM-dd")),
            KeyValue("Immediate Actions Deadline", now.AddDays(30).ToString("yyyy-MM-dd")),
            KeyValue("Short-Term Actions Deadline", now.AddDays(90).ToString("yyyy-MM-dd")),
            KeyValue("Effectiveness Review Date", now.AddDays(120).ToString("yyyy-MM-dd")),
            KeyValue("Final Close-Out Date", now.AddDays(180).ToString("yyyy-MM-dd")),
            "\n> Timeline is advisory. Responsible officers must confirm and adjust based on operational constraints.",
        };
        return Section("Target Completion", string.Join("\n", lines), 7);
    }

    private static DraftSection BuildEffectivenessReviewPlan(DraftGenerationContext ctx)
    {
        var lines = new List<string>
        {
            "**Effectiveness Review Framework:**",
            KeyValue("Review Type", "Post-implementation verification"),
            KeyValue("Reviewer", "Quality Management Officer *(to be assigned)*"),
            KeyValue("Review Criteria", "All corrective actions completed, evidence verified, no recurrence observed"),
            "\n**Review Steps:**",
            "1. Verify all corrective action checklist items are marked complete",
            "2. Confirm evidence documents are uploaded and approved in the system",
            "3. Conduct re-inspection or audit of affected area",
            "4. Document findings and update CAPA record with outcome",
            "5. Escalate to Quality Committee if recurrence is detected",
            "\n**Escalation Threshold:**",
            "If corrective actions are not completed by target date, escalate to:",
            "- Department Head",
            "- Quality Management Officer",
            "- Compliance Committee (for critical items)",
        };
        return Section("Effectiveness Review Plan", string.Join("\n", lines), 8);
    }

    public override DraftSourceIntelligence BuildSourceIntelligence(DraftGenerationContext context)
    {
        var si = base.BuildSourceIntelligence(context);

        if (context.CapaMetrics is { } capa)
        {
            si.CapaRefs.AddRange(capa.TopRootCauses.Select(rc => rc.Label));
        }

        return si;
    }
}
