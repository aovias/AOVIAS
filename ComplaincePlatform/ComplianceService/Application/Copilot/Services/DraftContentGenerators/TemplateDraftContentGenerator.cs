using ComplianceService.Application.Copilot.Contracts;
using ComplianceService.Application.Copilot.Models;
using ComplianceService.Domain.Copilot.Enums;

namespace ComplianceService.Application.Copilot.Services.DraftContentGenerators;

/// <summary>
/// Sprint 13C primary content generator.
/// Uses deterministic templates to produce structured, auditable draft content
/// from pre-fetched operational data — no LLM dependency.
/// </summary>
public sealed class TemplateDraftContentGenerator : IDraftContentGenerator
{
    public string GeneratorName => "TemplateDraftContentGenerator";

    public bool CanGenerate(DraftType draftType) => true; // Handles all template types

    public Task<DraftContent> GenerateAsync(
        IDraftTemplate template,
        DraftGenerationContext context,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var sections  = template.BuildSections(context);
        var si        = template.BuildSourceIntelligence(context);
        var title     = template.BuildTitle(context);
        var summary   = template.BuildSummary(context);
        var fullContent = AssembleFullMarkdown(title, summary, sections, si);

        var content = new DraftContent
        {
            Title             = title,
            Summary           = summary,
            FullContent       = fullContent,
            Sections          = sections,
            SourceIntelligence = si,
            Confidence        = 0.85m,
            GeneratorName     = GeneratorName,
        };

        return Task.FromResult(content);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static string AssembleFullMarkdown(
        string title,
        string summary,
        IReadOnlyList<DraftSection> sections,
        Domain.Copilot.DraftSourceIntelligence si)
    {
        var sb = new System.Text.StringBuilder();

        sb.AppendLine($"# {title}");
        sb.AppendLine();
        sb.AppendLine($"> {summary}");
        sb.AppendLine();
        sb.AppendLine($"---");
        sb.AppendLine();

        foreach (var section in sections.OrderBy(s => s.Order))
        {
            sb.AppendLine($"## {section.Name}");
            sb.AppendLine();
            sb.AppendLine(section.Content);
            sb.AppendLine();
        }

        // Source Intelligence section (hidden metadata for auditability)
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("<!-- SOURCE_INTELLIGENCE_BEGIN");
        sb.AppendLine(System.Text.Json.JsonSerializer.Serialize(si,
            new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
        sb.AppendLine("SOURCE_INTELLIGENCE_END -->");

        return sb.ToString();
    }
}
