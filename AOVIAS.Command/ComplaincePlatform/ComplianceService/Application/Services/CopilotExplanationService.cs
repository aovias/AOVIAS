using ComplianceService.Application.Contracts;
using ComplianceService.Domain;
using Shared.Infrastructure.DTO;

namespace ComplianceService.Application.Services;

/// <summary>
/// Orchestrates copilot explanation generation by coordinating ExplanationComposer and LLMExplanationAdapter.
/// Ensures LLM receives structured inputs only and explains findings (does not detect them).
/// </summary>
public class CopilotExplanationService : ICopilotExplanationService
{
    private readonly IExplanationComposer _explanationComposer;
    private readonly ILLMExplanationAdapter _llmAdapter;
    private readonly ILogger<CopilotExplanationService> _logger;

    private const string RequiredDisclaimer = "AI-assisted explanation. Requires human review.";

    public CopilotExplanationService(
        IExplanationComposer explanationComposer,
        ILLMExplanationAdapter llmAdapter,
        ILogger<CopilotExplanationService> logger)
    {
        _explanationComposer = explanationComposer;
        _llmAdapter = llmAdapter;
        _logger = logger;
    }

    public async Task<CopilotExplanationResult> GenerateExplanationAsync(
        CopilotIntent intent,
        GapAnalysisResult gapResults,
        ComplianceContext context,
        CancellationToken ct = default)
    {
        try
        {
            // Step 1: Convert GapAnalysisResult to List<Gap> for ExplanationComposer
            var gaps = ConvertGapResultsToGaps(gapResults);
            
            // Step 2: Compose explanation metadata and structured input
            var compositionResult = _explanationComposer.Compose(intent, context, gaps);

            // Step 3: Generate explanation from LLM using structured input only
            string explanationText;
            try
            {
                explanationText = await _llmAdapter.GenerateExplanationAsync(
                    compositionResult.ExplanationInput,
                    ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate explanation from LLM");
                
                // Fallback: Return explanation based on metadata only
                explanationText = BuildFallbackExplanation(compositionResult.ExplanationInput);
            }

            // Step 4: Format explanation with required disclaimer
            var explanationWithDisclaimer = FormatExplanationWithDisclaimer(explanationText);

            return new CopilotExplanationResult
            {
                Explanation = explanationText,
                ExplanationWithDisclaimer = explanationWithDisclaimer,
                Explainability = compositionResult.Explainability,
                IsSuccess = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating copilot explanation");
            return new CopilotExplanationResult
            {
                IsSuccess = false,
                ErrorMessage = $"Failed to generate explanation: {ex.Message}",
                ExplanationWithDisclaimer = FormatExplanationWithDisclaimer(
                    "Unable to generate explanation at this time. Please review the compliance context and gap analysis results manually.")
            };
        }
    }

    private string FormatExplanationWithDisclaimer(string explanation)
    {
        // Ensure disclaimer is always visible
        return $"{explanation}\n\n---\n\n{RequiredDisclaimer}";
    }

    private string BuildFallbackExplanation(ExplanationInput input)
    {
        // Fallback explanation when LLM is unavailable
        var parts = new List<string>();
        
        parts.Add($"Based on your question: \"{input.UserQuestion}\"");
        parts.Add($"Question type: {input.QuestionType}");
        parts.Add("");
        parts.Add("Context Summary:");
        parts.Add(input.ContextSummary);
        parts.Add("");
        parts.Add("Gap Analysis:");
        parts.Add(input.GapSummary);
        parts.Add("");
        parts.Add("Please review the above information to understand the compliance status.");

        return string.Join("\n", parts);
    }

    /// <summary>
    /// Converts GapAnalysisResult (with GapResult objects) to List<Gap> for ExplanationComposer.
    /// Maps GapResult to Gap, extracting relevant information without inferring severity.
    /// </summary>
    private List<Gap> ConvertGapResultsToGaps(GapAnalysisResult gapResults)
    {
        var gaps = new List<Gap>();

        foreach (var gapResult in gapResults.Gaps)
        {
            var gap = new Gap
            {
                GapType = gapResult.GapType,
                Description = gapResult.Description,
                // Map from GapReferences to AffectedEntityType and AffectedEntityId
                AffectedEntityType = DetermineAffectedEntityType(gapResult.References),
                AffectedEntityId = DetermineAffectedEntityId(gapResult.References),
                // Extract rules referenced if available (GapResult doesn't have this, so we'll leave it empty)
                RulesReferenced = new List<string>()
            };

            gaps.Add(gap);
        }

        return gaps;
    }

    /// <summary>
    /// Determines the affected entity type from GapReferences.
    /// </summary>
    private TargetEntityType DetermineAffectedEntityType(GapReferences references)
    {
        // Determine based on which reference is present
        if (references.ControlId.HasValue)
        {
            return TargetEntityType.Control;
        }
        if (references.TaskId.HasValue)
        {
            return TargetEntityType.Task;
        }
        if (references.AuditFindingId.HasValue)
        {
            return TargetEntityType.Audit;
        }
        if (references.ProgramId.HasValue)
        {
            return TargetEntityType.Program;
        }
        // Note: ComplianceStandardId maps to Program as TargetEntityType has no Standard
        if (references.ComplianceStandardId.HasValue)
        {
            return TargetEntityType.Program;
        }

        // Default fallback
        return TargetEntityType.None;
    }

    /// <summary>
    /// Determines the affected entity ID from GapReferences.
    /// </summary>
    private Guid? DetermineAffectedEntityId(GapReferences references)
    {
        // Return the first available ID
        if (references.ControlId.HasValue)
        {
            return references.ControlId;
        }
        if (references.TaskId.HasValue)
        {
            return references.TaskId;
        }
        if (references.AuditFindingId.HasValue)
        {
            return references.AuditFindingId;
        }
        if (references.ProgramId.HasValue)
        {
            return references.ProgramId;
        }
        if (references.ComplianceStandardId.HasValue)
        {
            return references.ComplianceStandardId;
        }

        return null;
    }
}
