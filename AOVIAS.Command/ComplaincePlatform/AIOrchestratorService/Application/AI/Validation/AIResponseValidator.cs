using AIOrchestratorService.Application.AI.Classification;
using AIOrchestratorService.Application.AI.Governance;
using AIOrchestratorService.Application.AI.Models;

namespace AIOrchestratorService.Application.AI.Validation;

public interface IAIResponseValidator
{
    AIResponse ValidateAndNormalize(AIRequest request, AIResponse response);
}

public sealed class AIResponseValidator : IAIResponseValidator
{
    private readonly IResponseSanitizer _sanitizer;
    private readonly IHallucinationGuard _hallucinationGuard;

    public AIResponseValidator(IResponseSanitizer sanitizer, IHallucinationGuard hallucinationGuard)
    {
        _sanitizer = sanitizer;
        _hallucinationGuard = hallucinationGuard;
    }

    public AIResponse ValidateAndNormalize(AIRequest request, AIResponse response)
    {
        if (string.IsNullOrWhiteSpace(response.ComplianceHint))
        {
            response.ValidationOutcome = AIValidationOutcome.Reject;
            response.ComplianceHint = "Response rejected: missing compliance hint.";
            response.ConfidenceScore = 0m;
            response.CompositeConfidenceScore = 0m;
            response.Reasoning = new List<AIReasoningStep>
            {
                new() { Code = "VALIDATION_REJECT", Message = "Missing required compliance hint." }
            };
            return response;
        }

        var corrected = false;

        if (response.ConfidenceScore < 0m || response.ConfidenceScore > 1m)
        {
            response.ConfidenceScore = Math.Clamp(response.ConfidenceScore, 0m, 1m);
            corrected = true;
        }

        if (response.CompositeConfidenceScore < 0m || response.CompositeConfidenceScore > 1m)
        {
            response.CompositeConfidenceScore = Math.Clamp(response.CompositeConfidenceScore, 0m, 1m);
            corrected = true;
        }

        if (response.SourceReferences.Count == 0)
        {
            corrected = true;

            // Mode-aware: never surface "System / fallback" to end users. General-knowledge
            // answers are attributed to model knowledge; organization-data answers with no
            // records keep an empty source list (the hint already explains there were none).
            // SmallTalk / general modes keep a synthetic attribution; org/hybrid modes do not
            // invent citations — confidence is reduced below instead.
            if (response.AnswerMode is AssistantAnswerMode.SmallTalk
                or AssistantAnswerMode.GeneralKnowledge
                or AssistantAnswerMode.ComplianceKnowledge)
            {
                response.SourceReferences.Add(new AISourceReference
                {
                    SourceType = "Model Knowledge",
                    SourceId = "general-compliance",
                    Summary = "Answered from general healthcare compliance knowledge.",
                    RelevanceScore = 0.85m
                });
            }
        }

        // Phase 5.2 — OrganizationSpecific / Hybrid answers without grounded citations
        // must not present as high-confidence. SmallTalk is intentionally untouched.
        if (RequiresCitationGrounding(response.AnswerMode) && !HasGroundedCitation(response.SourceReferences))
        {
            corrected = true;
            response.ConfidenceScore = Math.Min(response.ConfidenceScore, 0.45m);
            if (response.CompositeConfidenceScore > 0m)
            {
                response.CompositeConfidenceScore = Math.Min(response.CompositeConfidenceScore, 0.45m);
            }

            var note = _sanitizer.MissingCitationReasoningNote(response.AnswerMode);
            if (!response.Reasoning.Any(r =>
                    string.Equals(r.Code, "CITATION_GAP", StringComparison.OrdinalIgnoreCase)))
            {
                response.Reasoning.Insert(0, new AIReasoningStep
                {
                    Code = "CITATION_GAP",
                    Message = note
                });
            }
        }

        if (response.SuggestedAction != null &&
            string.Equals(response.SuggestedAction.ToolName, "set_compliance_status", StringComparison.OrdinalIgnoreCase))
        {
            response.ValidationOutcome = AIValidationOutcome.Reject;
            response.SuggestedAction = null;
            response.HumanApprovalRequired = true;
            response.Reasoning.Insert(0, new AIReasoningStep
            {
                Code = "POLICY_REJECT",
                Message = "AI cannot override deterministic compliance status."
            });
            return response;
        }

        // Light citation-only hallucination pass (retrieval facts applied in orchestration).
        var guard = _hallucinationGuard.Evaluate(
            response.ComplianceHint,
            response.SourceReferences,
            facts: null,
            response.ConfidenceScore);
        if (guard.IsFlagged)
        {
            response.ConfidenceScore = guard.AdjustedConfidence;
            corrected = true;
            response.Reasoning ??= new List<AIReasoningStep>();
            if (!response.Reasoning.Any(r =>
                    string.Equals(r.Code, "HALLUCINATION_GUARD", StringComparison.OrdinalIgnoreCase)))
            {
                response.Reasoning.Insert(0, new AIReasoningStep
                {
                    Code = "HALLUCINATION_GUARD",
                    Message = guard.Reason
                });
            }
        }

        response.ValidationOutcome = corrected ? AIValidationOutcome.Correct : AIValidationOutcome.Accept;
        response.ContextVersionId = request.ContextVersionId;
        response.PromptTemplateId = request.PromptTemplateId;
        response.PromptTemplateVersion = request.PromptTemplateVersion;
        return response;
    }

    private static bool RequiresCitationGrounding(AssistantAnswerMode mode) =>
        mode is AssistantAnswerMode.OrganizationSpecific or AssistantAnswerMode.Hybrid;

    private static bool HasGroundedCitation(IReadOnlyList<AISourceReference> sources)
    {
        foreach (var source in sources)
        {
            if (string.IsNullOrWhiteSpace(source.SourceType) && string.IsNullOrWhiteSpace(source.SourceId))
            {
                continue;
            }

            var type = source.SourceType?.Trim() ?? string.Empty;
            if (type.Equals("Model Knowledge", StringComparison.OrdinalIgnoreCase) ||
                type.Equals("DeterministicFallback", StringComparison.OrdinalIgnoreCase) ||
                type.Equals("System", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            return true;
        }

        return false;
    }
}
