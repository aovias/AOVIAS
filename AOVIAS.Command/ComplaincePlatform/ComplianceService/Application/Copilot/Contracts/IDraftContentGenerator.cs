using ComplianceService.Application.Copilot.Models;
using ComplianceService.Domain.Copilot.Enums;

namespace ComplianceService.Application.Copilot.Contracts;

/// <summary>
/// Produces <see cref="DraftContent"/> from a template and context.
/// Sprint 13C ships <c>TemplateDraftContentGenerator</c> (deterministic).
/// Extension point <c>LlmDraftContentGenerator</c> is registered for future LLM integration.
/// </summary>
public interface IDraftContentGenerator
{
    /// <summary>Display name used in Source Intelligence provenance tracking.</summary>
    string GeneratorName { get; }

    /// <summary>Returns true when this generator can handle the given draft type.</summary>
    bool CanGenerate(DraftType draftType);

    /// <summary>
    /// Generates full draft content using the template structure and the pre-fetched context.
    /// Must NOT invoke any write operations or workflow side effects.
    /// </summary>
    Task<DraftContent> GenerateAsync(
        IDraftTemplate template,
        DraftGenerationContext context,
        CancellationToken cancellationToken = default);
}
