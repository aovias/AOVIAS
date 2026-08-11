using ComplianceService.Application.Copilot.Models;
using ComplianceService.Domain.Copilot;
using ComplianceService.Domain.Copilot.Enums;

namespace ComplianceService.Application.Copilot.Templates;

/// <summary>
/// Template for evidence checklist drafts.
/// Sources: Obligations, Evidence library, Verification records.
/// Sections: Objective Element, Required Evidence, Missing Evidence,
///           Verification Status, Upload Recommendations.
/// </summary>
public sealed class EvidenceChecklistTemplate : DraftTemplateBase
{
    public override DraftType DraftType => DraftType.EvidenceChecklist;
    public override string TemplateName => "Evidence Checklist";
    public override DraftSourceType PrimarySourceType => DraftSourceType.Maturity;

    public override IReadOnlyList<string> SectionNames =>
    [
        "Objective Element",
        "Required Evidence",
        "Missing Evidence",
        "Verification Status",
        "Upload Recommendations",
    ];

    public override string BuildTitle(DraftGenerationContext context)
    {
        var date = context.SnapshotUtc.ToString("yyyy-MM-dd");
        if (context.EvidenceSummary is { TotalObligations: > 0 } ev)
            return $"Evidence Checklist — {ev.TotalObligations} Obligations — {date}";

        return $"Evidence Checklist — {date}";
    }

    public override string BuildSummary(DraftGenerationContext context)
    {
        var parts = new List<string>();

        if (context.EvidenceSummary is { } ev)
        {
            parts.Add($"{ev.TotalObligations} total obligations");
            if (ev.MissingEvidence > 0)
                parts.Add($"{ev.MissingEvidence} missing evidence items require upload");
            if (ev.PendingVerification > 0)
                parts.Add($"{ev.PendingVerification} items awaiting verification");
        }
        else if (context.StageEvaluations is { } stages)
        {
            var missing = stages.Sum(s => s.MissingEvidenceCount);
            var pending = stages.Sum(s => s.PendingVerificationCount);
            if (missing > 0) parts.Add($"{missing} missing evidence items");
            if (pending > 0) parts.Add($"{pending} pending verifications");
        }

        if (parts.Count == 0)
            parts.Add("Evidence checklist generated from obligation and verification data");

        return string.Join("; ", parts) + ". Review and upload missing items to advance stage progress.";
    }

    public override IReadOnlyList<DraftSection> BuildSections(DraftGenerationContext context)
    {
        return
        [
            BuildObjectiveElement(context),
            BuildRequiredEvidence(context),
            BuildMissingEvidence(context),
            BuildVerificationStatus(context),
            BuildUploadRecommendations(context),
        ];
    }

    // ── Section builders ──────────────────────────────────────────────────────

    private DraftSection BuildObjectiveElement(DraftGenerationContext ctx)
    {
        var lines = new List<string>
        {
            "**Evidence Checklist Scope:**",
            $"*Generated: {ctx.SnapshotUtc:yyyy-MM-dd HH:mm} UTC*",
        };

        if (ctx.SourceRecommendation is { } rec)
        {
            lines.Add($"\n**Source Recommendation:** {rec.Title}");
            if (!string.IsNullOrEmpty(rec.Description))
                lines.Add($"\n{rec.Description}");
        }

        if (ctx.StageEvaluations is { Count: > 0 } stages)
        {
            lines.Add("\n**Applicable Accreditation Stages:**");
            foreach (var stage in stages)
            {
                var badge = stage.IsBlocked ? "🔴" : stage.CompletionPercent >= 100 ? "✅" : "🔄";
                lines.Add($"- {badge} {stage.StageName} ({stage.CompletionPercent:F0}% complete)");
            }
        }

        if (ctx.EvidenceSummary is { } ev)
        {
            lines.Add($"\n**Total Obligation Coverage:** {ev.TotalObligations} obligations");
            var coverage = ev.TotalObligations > 0
                ? (ev.VerifiedEvidence * 100.0m / ev.TotalObligations)
                : 0;
            lines.Add(KeyValue("Verification Coverage", $"{coverage:F1}%"));
        }

        return Section("Objective Element", string.Join("\n", lines), 1);
    }

    private DraftSection BuildRequiredEvidence(DraftGenerationContext ctx)
    {
        var lines = new List<string> { "**Required Evidence Inventory:**" };

        if (ctx.EvidenceSummary is { } ev)
        {
            lines.Add("");
            lines.Add("| Obligation | Status | Evidence Type | Notes |");
            lines.Add("|------------|--------|---------------|-------|");

            // Top missing obligations as rows
            foreach (var obl in ev.TopMissingObligations.Take(15))
            {
                lines.Add($"| {obl} | ❌ Missing | *Document* | *To be uploaded* |");
            }

            if (ev.VerifiedEvidence > 0)
            {
                lines.Add($"\n*{ev.VerifiedEvidence} additional obligations are verified and compliant.*");
            }

            if (ev.TotalObligations > ev.TopMissingObligations.Count + ev.VerifiedEvidence)
            {
                lines.Add($"\n*Additional obligations not listed require manual review in the Evidence Library.*");
            }
        }
        else
        {
            lines.Add(NotAvailable("Obligation inventory data"));
            lines.Add("\n> Navigate to the Evidence Library to view the complete obligation list.");
        }

        return Section("Required Evidence", string.Join("\n", lines), 2);
    }

    private DraftSection BuildMissingEvidence(DraftGenerationContext ctx)
    {
        var lines = new List<string>();

        int totalMissing = ctx.EvidenceSummary?.MissingEvidence ?? 0;
        if (totalMissing == 0 && ctx.StageEvaluations is { } stages)
            totalMissing = stages.Sum(s => s.MissingEvidenceCount);

        if (totalMissing > 0)
        {
            lines.Add($"**{WarningBadge(totalMissing, "missing evidence items")} require upload:**");

            if (ctx.EvidenceSummary is { } ev && ev.TopMissingObligations.Count > 0)
            {
                lines.Add("\n**Priority Missing Obligations:**");
                foreach (var obl in ev.TopMissingObligations)
                {
                    lines.Add($"- [ ] Upload evidence for: **{obl}**");
                }
            }
            else if (ctx.StageEvaluations is { } evalStages)
            {
                foreach (var stage in evalStages.Where(s => s.MissingEvidenceCount > 0))
                {
                    lines.Add($"\n**Stage: {stage.StageName}** — {stage.MissingEvidenceCount} missing items");
                    lines.Add($"*Review stage obligations in the Stage Unlock module*");
                }
            }
        }
        else
        {
            lines.Add("✅ No missing evidence items detected at this time.");
            lines.Add("\n*Continue monitoring as new obligations may be added when progressing stages.*");
        }

        return Section("Missing Evidence", string.Join("\n", lines), 3);
    }

    private DraftSection BuildVerificationStatus(DraftGenerationContext ctx)
    {
        var lines = new List<string>();

        if (ctx.EvidenceSummary is { } ev)
        {
            lines.Add("**Verification Status Overview:**");
            lines.Add("");
            lines.Add("| Status | Count | Percentage |");
            lines.Add("|--------|-------|------------|");

            var total = ev.TotalObligations > 0 ? ev.TotalObligations : 1;
            lines.Add($"| ✅ Verified | {ev.VerifiedEvidence} | {ev.VerifiedEvidence * 100m / total:F1}% |");
            lines.Add($"| ⏳ Pending Verification | {ev.PendingVerification} | {ev.PendingVerification * 100m / total:F1}% |");
            lines.Add($"| ❌ Missing | {ev.MissingEvidence} | {ev.MissingEvidence * 100m / total:F1}% |");
        }
        else if (ctx.StageEvaluations is { } stages)
        {
            lines.Add("**Verification by Stage:**");
            lines.Add("");
            lines.Add("| Stage | Missing | Pending Verification |");
            lines.Add("|-------|---------|----------------------|");

            foreach (var stage in stages)
            {
                lines.Add($"| {stage.StageName} | " +
                          $"{WarningBadge(stage.MissingEvidenceCount, "missing")} | " +
                          $"{stage.PendingVerificationCount} |");
            }
        }
        else
        {
            lines.Add(NotAvailable("Verification status data"));
        }

        return Section("Verification Status", string.Join("\n", lines), 4);
    }

    private DraftSection BuildUploadRecommendations(DraftGenerationContext ctx)
    {
        var lines = new List<string> { "**Evidence Upload Guidance:**" };

        lines.Add("\n**General Upload Requirements:**");
        lines.Add("- Accepted formats: PDF, DOCX, XLSX, JPEG, PNG (max 10 MB per file)");
        lines.Add("- Document must clearly reference the obligation it satisfies");
        lines.Add("- Include date stamp and responsible officer signature where applicable");
        lines.Add("- All uploaded evidence requires verification by an authorized verifier");

        if (ctx.EvidenceSummary is { MissingEvidence: > 0 } ev)
        {
            lines.Add($"\n**Immediate Priority:** {ev.MissingEvidence} items need upload to maintain readiness trajectory.");
        }

        if (ctx.StageEvaluations is { } stages)
        {
            var criticalStages = stages.Where(s => s.IsBlocked && s.MissingEvidenceCount > 0).ToList();
            if (criticalStages.Count > 0)
            {
                lines.Add("\n**Blocked Stage Evidence — Upload First:**");
                foreach (var stage in criticalStages)
                {
                    lines.Add($"- {stage.StageName}: {stage.MissingEvidenceCount} items blocking stage unlock");
                }
            }
        }

        lines.Add("\n**Verification Workflow:**");
        lines.Add("1. Upload document in the Evidence Library");
        lines.Add("2. Link to the corresponding obligation");
        lines.Add("3. Submit for verification by authorized verifier");
        lines.Add("4. Track verification status in the dashboard");
        lines.Add("5. Confirmed verified evidence automatically updates readiness scores");

        return Section("Upload Recommendations", string.Join("\n", lines), 5);
    }

    public override DraftSourceIntelligence BuildSourceIntelligence(DraftGenerationContext context)
    {
        var si = base.BuildSourceIntelligence(context);

        if (context.EvidenceSummary is { } ev)
        {
            si.ObligationRefs.AddRange(ev.TopMissingObligations);
            si.AnalyticsMetrics["MissingEvidence"]     = ev.MissingEvidence.ToString();
            si.AnalyticsMetrics["PendingVerification"] = ev.PendingVerification.ToString();
            si.AnalyticsMetrics["VerifiedEvidence"]    = ev.VerifiedEvidence.ToString();
            si.AnalyticsMetrics["TotalObligations"]    = ev.TotalObligations.ToString();
        }

        return si;
    }
}
