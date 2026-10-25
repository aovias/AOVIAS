using ComplianceService.Application.Copilot.Models;
using ComplianceService.Domain.Copilot;
using ComplianceService.Domain.Copilot.Enums;

namespace ComplianceService.Application.Copilot.Templates;

/// <summary>
/// Template for accreditation readiness summaries.
/// Sources: StageUnlocking, ComplianceSnapshots, Analytics, Recommendations.
/// Sections: Current Stage, Readiness Score, Unlock Progress, Blocking Issues,
///           Evidence Gaps, Verification Gaps, Department Performance,
///           Next Milestones, Recommended Actions.
/// </summary>
public sealed class ReadinessSummaryTemplate : DraftTemplateBase
{
    public override DraftType DraftType => DraftType.ReadinessSummary;
    public override string TemplateName => "Accreditation Readiness Summary";
    public override DraftSourceType PrimarySourceType => DraftSourceType.Maturity;

    public override IReadOnlyList<string> SectionNames =>
    [
        "Current Stage",
        "Readiness Score",
        "Unlock Progress",
        "Blocking Issues",
        "Evidence Gaps",
        "Verification Gaps",
        "Department Performance",
        "Next Milestones",
        "Recommended Actions",
    ];

    public override string BuildTitle(DraftGenerationContext context)
    {
        var date = context.SnapshotUtc.ToString("MMMM d, yyyy");
        if (context.ReadinessMetrics is { } rdy)
            return $"Accreditation Readiness Summary — {rdy.JourneyAccreditationReadiness:F1}% — {date}";

        return $"Accreditation Readiness Summary — {date}";
    }

    public override string BuildSummary(DraftGenerationContext context)
    {
        var parts = new List<string>();

        if (context.ReadinessMetrics is { } rdy)
        {
            var score = rdy.JourneyAccreditationReadiness;
            var status = score >= 80 ? "on track" : score >= 60 ? "at risk" : "requires immediate attention";
            parts.Add($"Accreditation readiness is at {score:F1}% and {status}");
        }

        if (context.StageEvaluations is { } stages)
        {
            var blocked = stages.Count(s => s.IsBlocked);
            if (blocked > 0)
                parts.Add($"{blocked} stage(s) are currently blocked");
        }

        if (context.EvidenceSummary is { MissingEvidence: > 0 } ev)
            parts.Add($"{ev.MissingEvidence} evidence items are missing");

        if (parts.Count == 0)
            parts.Add("Accreditation readiness snapshot generated from operational data");

        return string.Join("; ", parts) + ". Human review required before sharing with accreditation bodies.";
    }

    public override IReadOnlyList<DraftSection> BuildSections(DraftGenerationContext context)
    {
        return
        [
            BuildCurrentStage(context),
            BuildReadinessScore(context),
            BuildUnlockProgress(context),
            BuildBlockingIssues(context),
            BuildEvidenceGaps(context),
            BuildVerificationGaps(context),
            BuildDepartmentPerformance(context),
            BuildNextMilestones(context),
            BuildRecommendedActions(context),
        ];
    }

    // ── Section builders ──────────────────────────────────────────────────────

    private DraftSection BuildCurrentStage(DraftGenerationContext ctx)
    {
        var lines = new List<string>();

        if (ctx.StageEvaluations is { Count: > 0 } stages)
        {
            var current = stages.FirstOrDefault(s => s.IsBlocked) ?? stages[0];
            lines.Add($"**Current Active Stage:** {current.StageName}");
            lines.Add(KeyValue("Stage Completion", $"{current.CompletionPercent:F0}%"));
            lines.Add(KeyValue("Status", current.IsBlocked ? "🔴 BLOCKED" : "🟢 Active"));
            lines.Add(KeyValue("Blocking Issues", current.BlockingIssueCount.ToString()));
        }
        else
        {
            lines.Add(NotAvailable("Stage evaluation data"));
            lines.Add("\n> Navigate to Stage Unlock module to view current stage status.");
        }

        lines.Add($"\n*Snapshot captured: {ctx.SnapshotUtc:yyyy-MM-dd HH:mm} UTC*");

        return Section("Current Stage", string.Join("\n", lines), 1);
    }

    private DraftSection BuildReadinessScore(DraftGenerationContext ctx)
    {
        var lines = new List<string>();

        if (ctx.ReadinessMetrics is { } rdy)
        {
            lines.Add("**Readiness Score Breakdown:**");
            lines.Add(KeyValue("Journey Accreditation Readiness",
                ScoreBadge(rdy.JourneyAccreditationReadiness)));
            lines.Add(KeyValue("Framework Assessment Readiness",
                ScoreBadge(rdy.FrameworkAssessmentReadiness)));
            lines.Add($"\n**Source:** {rdy.JourneyReadinessSource}");
        }
        else if (ctx.ExecutiveAnalytics is { } exec)
        {
            lines.Add("**Readiness Scores:**");
            lines.Add(KeyValue("Accreditation Journey",
                ScoreBadge(exec.AccreditationReadiness.Score)));
            lines.Add(KeyValue("Compliance Framework",
                ScoreBadge(exec.ComplianceReadiness.Score)));
        }
        else
        {
            lines.Add(NotAvailable("Readiness score data"));
        }

        lines.Add("\n> Scores reflect the latest analytics snapshot. " +
                  "Scores are recalculated on each recommendation refresh.");

        return Section("Readiness Score", string.Join("\n", lines), 2);
    }

    private DraftSection BuildUnlockProgress(DraftGenerationContext ctx)
    {
        var lines = new List<string>();

        if (ctx.StageEvaluations is { } stages)
        {
            lines.Add("**Stage Unlock Progress:**");
            lines.Add("");
            lines.Add("| Stage | Completion | Status | Blocking Issues |");
            lines.Add("|-------|------------|--------|-----------------|");

            foreach (var stage in stages)
            {
                var status = stage.IsBlocked ? "🔴 Blocked"
                    : stage.CompletionPercent >= 100 ? "✅ Complete"
                    : "🔄 In Progress";

                lines.Add($"| {stage.StageName} | {stage.CompletionPercent:F0}% | {status} | " +
                          $"{stage.BlockingIssueCount} |");
            }
        }
        else
        {
            lines.Add(NotAvailable("Stage unlock progress data"));
        }

        return Section("Unlock Progress", string.Join("\n", lines), 3);
    }

    private DraftSection BuildBlockingIssues(DraftGenerationContext ctx)
    {
        var lines = new List<string>();

        var blocked = ctx.StageEvaluations?.Where(s => s.IsBlocked).ToList() ?? [];

        if (blocked.Count > 0)
        {
            lines.Add($"**{WarningBadge(blocked.Count, "blocked stage(s)")}**");
            lines.Add("");

            foreach (var stage in blocked)
            {
                lines.Add($"**Stage: {stage.StageName}**");
                lines.Add(KeyValue("Blocking Issues", stage.BlockingIssueCount.ToString()));
                lines.Add(KeyValue("Missing Evidence", stage.MissingEvidenceCount.ToString()));
                lines.Add(KeyValue("Pending Verification", stage.PendingVerificationCount.ToString()));
                lines.Add("");
            }
        }
        else
        {
            lines.Add("✅ No blocked stages identified at this time.");
        }

        if (ctx.DeficiencyMetrics is { BlockingOpen: > 0 } def)
        {
            lines.Add($"\n**Additional Blocking Deficiencies:** {def.BlockingOpen} open blocking deficiencies");
        }

        return Section("Blocking Issues", string.Join("\n", lines), 4);
    }

    private DraftSection BuildEvidenceGaps(DraftGenerationContext ctx)
    {
        var lines = new List<string>();

        if (ctx.EvidenceSummary is { } ev)
        {
            lines.Add("**Evidence Gap Summary:**");
            lines.Add(KeyValue("Total Obligations", ev.TotalObligations.ToString()));
            lines.Add(KeyValue("Missing Evidence", $"**{WarningBadge(ev.MissingEvidence, "missing")}**"));
            lines.Add(KeyValue("Pending Verification", WarningBadge(ev.PendingVerification, "pending")));
            lines.Add(KeyValue("Verified Evidence", $"✅ {ev.VerifiedEvidence}"));

            if (ev.TopMissingObligations.Count > 0)
            {
                lines.Add("\n**Top Missing Obligations:**");
                foreach (var obl in ev.TopMissingObligations.Take(10))
                    lines.Add($"- {obl}");
            }
        }
        else if (ctx.StageEvaluations is { } stages)
        {
            var totalMissing = stages.Sum(s => s.MissingEvidenceCount);
            if (totalMissing > 0)
            {
                lines.Add($"**Estimated Missing Evidence:** {totalMissing} items across {stages.Count(s => s.MissingEvidenceCount > 0)} stages");
            }
            else
            {
                lines.Add("✅ No evidence gaps detected from stage evaluation data.");
            }
        }
        else
        {
            lines.Add(NotAvailable("Evidence gap data"));
        }

        return Section("Evidence Gaps", string.Join("\n", lines), 5);
    }

    private DraftSection BuildVerificationGaps(DraftGenerationContext ctx)
    {
        var lines = new List<string>();

        if (ctx.EvidenceSummary is { } ev)
        {
            lines.Add("**Verification Status:**");
            lines.Add(KeyValue("Pending Verification", $"**{WarningBadge(ev.PendingVerification, "pending")}**"));
            lines.Add(KeyValue("Verified", $"✅ {ev.VerifiedEvidence}"));
            var coverage = ev.TotalObligations > 0
                ? (ev.VerifiedEvidence * 100.0m / ev.TotalObligations)
                : 0;
            lines.Add(KeyValue("Verification Coverage", $"{coverage:F1}%"));
        }
        else if (ctx.StageEvaluations is { } stages)
        {
            var totalPending = stages.Sum(s => s.PendingVerificationCount);
            if (totalPending > 0)
            {
                lines.Add($"**Pending Verification Items:** {totalPending} across active stages");
                foreach (var s in stages.Where(x => x.PendingVerificationCount > 0))
                    lines.Add(KeyValue(s.StageName, $"{s.PendingVerificationCount} pending"));
            }
            else
            {
                lines.Add("✅ No pending verification items from stage evaluation.");
            }
        }
        else
        {
            lines.Add(NotAvailable("Verification gap data"));
        }

        return Section("Verification Gaps", string.Join("\n", lines), 6);
    }

    private DraftSection BuildDepartmentPerformance(DraftGenerationContext ctx)
    {
        var lines = new List<string>();

        if (ctx.ExecutiveAnalytics?.DepartmentPerformance is { Count: > 0 } depts)
        {
            lines.Add("**Department Readiness Performance:**");
            lines.Add("");
            lines.Add("| Department | Open Issues | Deep Link |");
            lines.Add("|------------|-------------|-----------|");

            foreach (var dept in depts.OrderByDescending(d => d.Count).Take(10))
            {
                var link = dept.DeepLink?.Route is { Length: > 0 } route ? $"[View]({route})" : "—";
                lines.Add($"| {dept.Label} | {WarningBadge(dept.Count, "issues")} | {link} |");
            }
        }
        else
        {
            lines.Add(NotAvailable("Department performance data"));
            lines.Add("\n> Department breakdown requires Executive Analytics context. " +
                      "Request an Executive Briefing for full department analysis.");
        }

        return Section("Department Performance", string.Join("\n", lines), 7);
    }

    private DraftSection BuildNextMilestones(DraftGenerationContext ctx)
    {
        var now = DateTime.UtcNow;
        var lines = new List<string>
        {
            "**Recommended Next Milestones** *(Advisory — requires planning confirmation)*",
            "",
            "| Milestone | Target Date | Status |",
            "|-----------|-------------|--------|",
        };

        if (ctx.StageEvaluations is { } stages)
        {
            foreach (var stage in stages.Where(s => s.CompletionPercent < 100).Take(3))
            {
                var weeks = stage.IsBlocked ? 6 : Math.Max(2, (int)((100 - stage.CompletionPercent) / 10));
                lines.Add($"| Unlock: {stage.StageName} | " +
                          $"{now.AddDays(weeks * 7):yyyy-MM-dd} | " +
                          $"{(stage.IsBlocked ? "🔴 Blocked" : "🔄 In Progress")} |");
            }
        }

        lines.Add($"| Readiness Review | {now.AddDays(30):yyyy-MM-dd} | 📅 Scheduled |");
        lines.Add($"| Management Review | {now.AddDays(60):yyyy-MM-dd} | 📅 Planned |");
        lines.Add($"| Pre-Accreditation Audit | {now.AddDays(90):yyyy-MM-dd} | 📅 Target |");

        return Section("Next Milestones", string.Join("\n", lines), 8);
    }

    private DraftSection BuildRecommendedActions(DraftGenerationContext ctx)
    {
        var lines = new List<string> { "**Priority Actions to Improve Readiness:**" };

        var actions = new List<string>();

        if (ctx.StageEvaluations?.Any(s => s.IsBlocked) == true)
            actions.Add("Unblock blocked accreditation stages — resolve blocking issues immediately");

        if (ctx.EvidenceSummary is { MissingEvidence: > 0 } ev)
            actions.Add($"Upload {ev.MissingEvidence} missing evidence documents with verified verification");

        if (ctx.EvidenceSummary is { PendingVerification: > 0 } ev2)
            actions.Add($"Complete verification for {ev2.PendingVerification} pending evidence items");

        if (ctx.DeficiencyMetrics is { CriticalOpen: > 0 } def)
            actions.Add($"Resolve {def.CriticalOpen} critical open deficiencies");

        if (ctx.CapaMetrics is { OverdueCapas: > 0 } capa)
            actions.Add($"Close {capa.OverdueCapas} overdue CAPAs to improve governance score");

        if (ctx.SourceRecommendation is { } rec)
            actions.Add(rec.Title);

        if (actions.Count == 0)
            actions.Add("Continue current trajectory — readiness is on track");

        foreach (var action in actions)
            lines.Add($"- [ ] {action}");

        return Section("Recommended Actions", string.Join("\n", lines), 9);
    }
}
