using ComplianceService.Application.Copilot.Contracts;
using ComplianceService.Application.Copilot.Models;
using ComplianceService.Domain.Copilot;
using ComplianceService.Domain.Copilot.Enums;

namespace ComplianceService.Application.Copilot.Templates;

/// <summary>
/// Abstract base for all draft templates.
/// Provides shared markdown formatting helpers, safe data-access utilities,
/// and a default Source Intelligence builder that subclasses can extend.
/// </summary>
public abstract class DraftTemplateBase : IDraftTemplate
{
    public abstract DraftType DraftType { get; }
    public abstract string TemplateName { get; }
    public abstract DraftSourceType PrimarySourceType { get; }
    public abstract IReadOnlyList<string> SectionNames { get; }
    public abstract string BuildTitle(DraftGenerationContext context);
    public abstract string BuildSummary(DraftGenerationContext context);
    public abstract IReadOnlyList<DraftSection> BuildSections(DraftGenerationContext context);

    /// <summary>Default Source Intelligence builder — collects all data from context into metadata.</summary>
    public virtual DraftSourceIntelligence BuildSourceIntelligence(DraftGenerationContext context)
    {
        var si = new DraftSourceIntelligence
        {
            TemplateName   = TemplateName,
            GeneratorName  = "TemplateDraftContentGenerator",
            OrganizationId = context.Request.OrganizationId.ToString(),
            GeneratedAtUtc = context.SnapshotUtc.ToString("O"),
        };

        if (context.Request.RecommendationId.HasValue)
            si.RecommendationIds.Add(context.Request.RecommendationId.Value.ToString());

        if (context.SourceRecommendation is { } rec)
        {
            if (!si.RecommendationIds.Contains(rec.Id.ToString()))
                si.RecommendationIds.Add(rec.Id.ToString());

            if (!string.IsNullOrEmpty(rec.RationaleJson))
                si.RationaleObjects.Add(rec.RationaleJson);

            if (rec.SourceEntityId.HasValue)
                si.DeficiencyRefs.Add(rec.SourceEntityId.Value.ToString());
        }

        // Deficiency KPIs (actual DTO field names)
        if (context.DeficiencyMetrics is { } def)
        {
            si.KpiValues["TotalDeficiencies"]       = def.Total.ToString();
            si.KpiValues["CriticalOpenDeficiencies"] = def.CriticalOpen.ToString();
            si.KpiValues["BlockingDeficiencies"]     = def.BlockingOpen.ToString();
            si.AnalyticsMetrics["DeficiencyOpen"]    = def.Open.ToString();
            si.AnalyticsMetrics["DeficiencyResolved"] = def.Closed.ToString();
            si.AnalyticsMetrics["RecurringDeficiencies"] = def.RecurringOpen.ToString();
        }

        // CAPA KPIs (actual CapaMetricsDto field names)
        if (context.CapaMetrics is { } capa)
        {
            si.KpiValues["OpenCapas"]            = capa.OpenCapas.ToString();
            si.KpiValues["OverdueCapas"]         = capa.OverdueCapas.ToString();
            si.KpiValues["EffectiveCapas"]       = capa.EffectiveCapas.ToString();
            si.AnalyticsMetrics["IneffectiveCapas"] = capa.IneffectiveCapas.ToString();
            si.AnalyticsMetrics["CapaEffectivenessRate"] =
                $"{capa.CapaEffectivenessRate:P0}";
        }

        // SLA KPIs (actual SlaEscalationMetricsDto field names)
        if (context.SlaMetrics is { } sla)
        {
            si.KpiValues["SlaBreached"]         = sla.BreachedCount.ToString();
            si.KpiValues["SlaAtRisk"]           = sla.AtRiskCount.ToString();
            si.KpiValues["SlaEscalated"]        = sla.EscalatedCount.ToString();
            si.AnalyticsMetrics["SlaOnTrack"]   = sla.OnTrackCount.ToString();
            si.AnalyticsMetrics["SlaBreachRate"] = $"{sla.BreachRate:P0}";
        }

        // Readiness KPIs (actual AnalyticsReadinessDto field names)
        if (context.ReadinessMetrics is { } rdy)
        {
            si.KpiValues["JourneyReadinessScore"]     = rdy.JourneyAccreditationReadiness.ToString("F1");
            si.KpiValues["FrameworkReadinessScore"]   = rdy.FrameworkAssessmentReadiness.ToString("F1");
        }

        // Source entity reference
        if (context.Request.SourceEntityId.HasValue)
        {
            var sourceRef = context.Request.SourceEntityId.Value.ToString();
            if (!si.DeficiencyRefs.Contains(sourceRef))
                si.DeficiencyRefs.Add(sourceRef);
        }

        return si;
    }

    // ── Markdown helpers ──────────────────────────────────────────────────────

    protected static string H2(string heading) => $"## {heading}";
    protected static string H3(string heading) => $"### {heading}";
    protected static string Bold(string text)  => $"**{text}**";

    protected static string BulletList(IEnumerable<string> items) =>
        string.Join("\n", items.Select(i => $"- {i}"));

    protected static string KeyValue(string key, string value) =>
        $"- **{key}:** {value}";

    protected static string NotAvailable(string fieldName) =>
        $"*{fieldName} data not available for this snapshot.*";

    protected static string WarningBadge(int count, string label) =>
        count > 0 ? $"⚠️ {count} {label}" : $"✅ No {label}";

    protected static string ScoreBadge(decimal score) =>
        score >= 80 ? $"🟢 {score:F1} (On Track)" :
        score >= 60 ? $"🟡 {score:F1} (At Risk)"  :
                      $"🔴 {score:F1} (Critical)";

    protected static DraftSection Section(
        string name, string content, int order, bool required = true) =>
        new() { Name = name, Content = content, Order = order, IsRequired = required };

    /// <summary>Joins full markdown from sections with H2 headings.</summary>
    protected static string AssembleMarkdown(IReadOnlyList<DraftSection> sections) =>
        string.Join("\n\n", sections
            .OrderBy(s => s.Order)
            .Select(s => $"## {s.Name}\n\n{s.Content}"));
}
