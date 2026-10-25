using System.Text;
using ComplianceService.Application.Analytics;
using ComplianceService.Application.Contracts;
using ComplianceService.Application.DTOs;
using ComplianceService.Application.Journey;
using ComplianceService.Domain.Enums;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Domain.Enums;

namespace ComplianceService.Application.Services;

/// <summary>
/// Generates audit report content reusing analytics SSOT (Sprint 13C draft pattern — advisory only).
/// </summary>
public sealed class AuditorReportService : IAuditorReportService
{
    private readonly ComplianceDbContext _db;
    private readonly IAuditorWorkspaceQueryService _workspace;
    private readonly IAnalyticsAggregationService _analytics;

    public AuditorReportService(
        ComplianceDbContext db,
        IAuditorWorkspaceQueryService workspace,
        IAnalyticsAggregationService analytics)
    {
        _db = db;
        _workspace = workspace;
        _analytics = analytics;
    }

    public async Task<AuditorSessionReportDto?> GenerateReportAsync(
        Guid organizationId, Guid hospitalId, Guid sessionId, CancellationToken ct = default)
    {
        var session = await _workspace.GetSessionByIdAsync(organizationId, sessionId, ct);
        if (session == null) return null;

        var strengths = session.Observations
            .Where(o => o.Type is ObservationType.PositivePractice or ObservationType.Strength)
            .Select(o => $"{o.Title}: {o.Description}")
            .ToList();

        var recommendations = session.Observations
            .Where(o => o.Type is ObservationType.Suggestion or ObservationType.Opportunity)
            .Select(o => o.Description)
            .Concat(session.Comments
                .Where(c => c.Type == AuditCommentType.Recommendation)
                .Select(c => c.Content))
            .Distinct()
            .ToList();

        var criticalCount = session.Findings.Count(f => f.Severity == FindingSeverity.Critical);
        var majorCount = session.Findings.Count(f => f.Severity == FindingSeverity.Major);
        var riskSummary = $"Critical: {criticalCount}, Major: {majorCount}, Total Findings: {session.FindingsCount}";

        var evidenceGaps = new List<string>();
        if (session.FrameworkId.HasValue)
        {
            var context = await _workspace.GetAssessmentContextAsync(organizationId, hospitalId, session.FrameworkId, ct);
            if (context != null)
            {
                foreach (var ch in context.Chapters)
                {
                    foreach (var st in ch.Standards)
                    {
                        foreach (var oe in st.ObjectiveElements.Where(o => o.EvidenceCount == 0))
                        {
                            evidenceGaps.Add($"{oe.Code}: No evidence linked");
                        }
                    }
                }
            }
        }

        decimal readiness = 0;
        var journey = session.FrameworkId.HasValue
            ? await ComplianceJourneySnapshotResolver.GetActiveJourneyForSnapshotAsync(
                _db, hospitalId, session.FrameworkId.Value, ct)
            : await _db.ComplianceJourneys.IgnoreQueryFilters().AsNoTracking()
                .Where(j => j.HospitalId == hospitalId)
                .OrderByDescending(j => j.UpdatedAt)
                .FirstOrDefaultAsync(ct);
        if (journey != null)
        {
            var scope = AnalyticsScope.ForHospital(organizationId, hospitalId, journey.FrameworkId);
            var r = await _analytics.GetReadinessAsync(scope, ct);
            readiness = r.JourneyAccreditationReadiness;
        }

        var markdown = BuildMarkdown(session, strengths, recommendations, riskSummary, evidenceGaps, readiness);

        return new AuditorSessionReportDto
        {
            SessionId = sessionId,
            SessionNumber = session.SessionNumber,
            Title = $"Audit Report — {session.SessionNumber}",
            GeneratedAt = DateTime.UtcNow,
            Summary = session.Summary ?? $"Audit session {session.SessionNumber} ({session.AuditType})",
            Strengths = strengths,
            Observations = session.Observations,
            Findings = session.Findings,
            RiskSummary = riskSummary,
            Recommendations = recommendations,
            EvidenceGaps = evidenceGaps.Take(20).ToList(),
            ReadinessSummary = readiness,
            MarkdownContent = markdown,
        };
    }

    private static string BuildMarkdown(
        AuditSessionDetailDto session,
        IReadOnlyList<string> strengths,
        IReadOnlyList<string> recommendations,
        string riskSummary,
        IReadOnlyList<string> evidenceGaps,
        decimal readiness)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# Audit Report — {session.SessionNumber}");
        sb.AppendLine();
        sb.AppendLine($"**Type:** {session.AuditType}  ");
        sb.AppendLine($"**Status:** {session.Status}  ");
        sb.AppendLine($"**Scope:** {session.Scope}  ");
        sb.AppendLine($"**Lead Auditor:** {session.LeadAuditorName ?? "N/A"}  ");
        sb.AppendLine();
        sb.AppendLine("## Summary");
        sb.AppendLine(session.Summary ?? "_No summary provided._");
        sb.AppendLine();
        sb.AppendLine("## Strengths");
        foreach (var s in strengths) sb.AppendLine($"- {s}");
        if (strengths.Count == 0) sb.AppendLine("_None recorded._");
        sb.AppendLine();
        sb.AppendLine("## Observations");
        foreach (var o in session.Observations)
            sb.AppendLine($"- **[{o.Type}]** {o.Title}: {o.Description}");
        sb.AppendLine();
        sb.AppendLine("## Findings");
        foreach (var f in session.Findings)
            sb.AppendLine($"- **{f.FindingNumber}** [{f.Severity}] {f.Title} — {f.Status}");
        sb.AppendLine();
        sb.AppendLine("## Risk Summary");
        sb.AppendLine(riskSummary);
        sb.AppendLine();
        sb.AppendLine("## Recommendations");
        foreach (var r in recommendations) sb.AppendLine($"- {r}");
        sb.AppendLine();
        sb.AppendLine("## Evidence Gaps");
        foreach (var g in evidenceGaps.Take(20)) sb.AppendLine($"- {g}");
        sb.AppendLine();
        sb.AppendLine($"## Readiness Summary: {readiness:F1}%");
        return sb.ToString();
    }
}
