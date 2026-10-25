using ComplianceService.Application.Copilot.Models;
using ComplianceService.Domain.Copilot;
using ComplianceService.Domain.Copilot.Enums;

namespace ComplianceService.Application.Copilot.Contracts;

/// <summary>
/// Defines the structural contract for a draft type.
/// Templates declare their section schema and build metadata — they do NOT generate content.
/// Content generation is delegated to <see cref="IDraftContentGenerator"/>.
/// </summary>
public interface IDraftTemplate
{
    /// <summary>The draft type this template handles.</summary>
    DraftType DraftType { get; }

    /// <summary>Human-readable template name for logging and Source Intelligence.</summary>
    string TemplateName { get; }

    /// <summary>Primary source type that this template is optimized for.</summary>
    DraftSourceType PrimarySourceType { get; }

    /// <summary>Ordered section headings defined by this template.</summary>
    IReadOnlyList<string> SectionNames { get; }

    /// <summary>Generates the draft title from available context data.</summary>
    string BuildTitle(DraftGenerationContext context);

    /// <summary>Generates the executive summary sentence(s) from context.</summary>
    string BuildSummary(DraftGenerationContext context);

    /// <summary>Builds all draft sections from context. Each section contains generated markdown content.</summary>
    IReadOnlyList<DraftSection> BuildSections(DraftGenerationContext context);

    /// <summary>
    /// Builds the Source Intelligence metadata from context.
    /// Must reference all recommendation IDs, KPI values, deficiencies, CAPAs,
    /// obligations, analytics metrics, and rationale objects used in this draft.
    /// </summary>
    DraftSourceIntelligence BuildSourceIntelligence(DraftGenerationContext context);
}
