using ComplianceService.Application.Copilot.Contracts;
using ComplianceService.Application.Copilot.Models;
using ComplianceService.Domain.Copilot.Enums;

namespace ComplianceService.Application.Copilot.Services.DraftContentGenerators;

/// <summary>
/// Extension point for LLM-powered draft content generation.
/// Sprint 13C: NOT active — <see cref="TemplateDraftContentGenerator"/> is the registered implementation.
/// Sprint 13D+: Register this class via DI to enable AI-enhanced content generation.
///
/// IMPORTANT: When enabled, this generator must:
/// - Never invoke write operations or workflow mutations
/// - Always include Source Intelligence metadata from the template
/// - Pass deterministic template output as structured context to the LLM
/// - Log all LLM calls for audit trail
/// </summary>
public sealed class LlmDraftContentGenerator : IDraftContentGenerator
{
    public string GeneratorName => "LlmDraftContentGenerator";

    // When LLM integration is activated, inject IAiProvider here
    public LlmDraftContentGenerator() { }

    /// <summary>Not registered in DI — always false so this generator cannot be selected.</summary>
    public bool CanGenerate(DraftType draftType) => false;

    public Task<DraftContent> GenerateAsync(
        IDraftTemplate template,
        DraftGenerationContext context,
        CancellationToken cancellationToken = default)
    {
        // Deferred: AI-0/AI-1 commercial readiness (tracked backlog). TemplateDraftContentGenerator is active.
        return Task.FromException<DraftContent>(new InvalidOperationException(
            "LlmDraftContentGenerator is not enabled. Use TemplateDraftContentGenerator until LLM integration ships (Phase AI-0)."));
    }
}
