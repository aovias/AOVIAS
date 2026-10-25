using System.Text.RegularExpressions;
using ComplianceService.Application.Contracts;
using ComplianceService.Application.DTOs;
using ComplianceService.Domain;
using Shared.Infrastructure.DTO;

namespace ComplianceService.Application.Services;

/// <summary>
/// Factory for creating CopilotResponse instances with enforced Explainability.
/// Ensures that every CopilotResponse always includes Explainability metadata,
/// using fallback explainability if normal composition fails.
/// Integrates LLM rewrite for improved clarity while preserving authoritative ExplanationComposer output.
/// </summary>
public class CopilotResponseFactory : ICopilotResponseFactory
{
    private readonly IExplanationComposer _explanationComposer;
    private readonly ILLMExplanationAdapter _llmAdapter;
    private readonly ILogger<CopilotResponseFactory> _logger;

    public CopilotResponseFactory(
        IExplanationComposer explanationComposer,
        ILLMExplanationAdapter llmAdapter,
        ILogger<CopilotResponseFactory> logger)
    {
        _explanationComposer = explanationComposer;
        _llmAdapter = llmAdapter;
        _logger = logger;
    }

    /// <summary>
    /// Creates a CopilotResponse with enforced Explainability.
    /// Always calls ExplanationComposer to generate explainability metadata.
    /// If composition fails, uses safe fallback explainability.
    /// Applies LLM rewrite to Reasoning text for improved clarity (user-facing).
    /// </summary>
    /// <param name="intent">The copilot intent.</param>
    /// <param name="context">The compliance context.</param>
    /// <param name="gaps">The list of detected gaps.</param>
    /// <param name="summary">The AI-generated summary text.</param>
    /// <param name="findings">The gap findings.</param>
    /// <param name="suggestedNextSteps">Optional suggested next steps.</param>
    /// <param name="sourceReferences">Optional source references.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A CopilotResponse with guaranteed Explainability.</returns>
    public async Task<CopilotResponse> CreateAsync(
        CopilotIntent intent,
        ComplianceContext context,
        List<Gap> gaps,
        string summary,
        List<GapResult> findings,
        List<SuggestedNextStep>? suggestedNextSteps = null,
        SourceReferences? sourceReferences = null,
        CancellationToken ct = default)
    {
        ExplainabilityDto explainability;

        try
        {
            // Always call ExplanationComposer to generate explainability
            var compositionResult = _explanationComposer.Compose(intent, context, gaps);
            explainability = compositionResult.Explainability;
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogWarning(ex, "ExplanationComposer failed due to null argument. Using fallback explainability.");
            explainability = _explanationComposer.CreateFallbackExplainability(
                $"Required parameter was null: {ex.ParamName}", 
                intent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ExplanationComposer failed unexpectedly. Using fallback explainability.");
            explainability = _explanationComposer.CreateFallbackExplainability(
                $"Unexpected error during explainability composition: {ex.Message}", 
                intent);
        }

        // Ensure HumanReviewRequired is always true (safety enforcement)
        if (!explainability.HumanReviewRequired)
        {
            explainability = explainability with { HumanReviewRequired = true };
        }

        // Guardrail: Verify Explainability is never null
        // This is a safety assertion to prevent null Explainability from being returned
        if (explainability == null)
        {
            _logger.LogError("CRITICAL: Explainability is null after composition. Creating emergency fallback.");
            explainability = _explanationComposer.CreateFallbackExplainability(
                "Emergency fallback: Explainability was null after composition", 
                intent);
        }

        // Apply LLM rewrite to Reasoning text only (for user-facing clarity)
        // ExplanationComposer output remains authoritative - we only rewrite the Reasoning text
        // RulesReferenced, ConfidenceScore, HumanReviewRequired remain unchanged
        explainability = await ApplyReasoningRewriteAsync(explainability, intent, ct);

        return new CopilotResponse
        {
            QuestionType = intent.QuestionType,
            Summary = summary,
            Findings = findings,
            SuggestedNextSteps = suggestedNextSteps ?? new List<SuggestedNextStep>(),
            Explainability = explainability, // Always set - never null (verified by guardrail above)
            SourceReferences = sourceReferences ?? new SourceReferences()
        };
    }

    /// <summary>
    /// Applies LLM rewrite to Reasoning text only, preserving all other ExplainabilityDto fields.
    /// ExplanationComposer output remains authoritative - rewrite is for clarity only.
    /// </summary>
    private async Task<ExplainabilityDto> ApplyReasoningRewriteAsync(
        ExplainabilityDto explainability,
        CopilotIntent intent,
        CancellationToken ct = default)
    {
        // Store original reasoning (authoritative, internal)
        var originalReasoning = explainability.Reasoning;

        // If reasoning is empty, skip rewrite
        if (string.IsNullOrWhiteSpace(originalReasoning))
        {
            _logger.LogDebug("Skipping rewrite - reasoning text is empty.");
            return explainability;
        }

        try
        {
            // Determine audience and tone from intent (default to Neutral tone, ComplianceOfficer audience)
            // TODO: These could be extracted from user context or request parameters in the future
            var audienceType = ExplanationAudienceType.ComplianceOfficer;
            var tone = ExplanationTone.Neutral;

            // Create rewrite request
            var rewriteRequest = new ExplanationRewriteRequest
            {
                OriginalExplanationText = originalReasoning,
                AudienceType = audienceType,
                Tone = tone
            };

            // Apply LLM rewrite to Reasoning text only
            var rewrittenReasoning = await _llmAdapter.RewriteExplanationAsync(rewriteRequest, ct);

            // If rewrite failed (returned original), use original
            if (string.IsNullOrWhiteSpace(rewrittenReasoning))
            {
                return explainability with
                {
                    Reasoning = originalReasoning, // Original remains authoritative (internal)
                    RewrittenReasoning = string.Empty // No rewrite available
                };
            }

            // Safety check: Verify rewritten text doesn't contradict or omit key information
            if (!ValidateRewriteSafety(originalReasoning, rewrittenReasoning))
            {
                _logger.LogWarning(
                    "LLM rewrite failed safety check - rewritten text contradicts or omits key information. " +
                    "Discarding rewrite and using original explanation. " +
                    "Original length: {OriginalLength}, Rewritten length: {RewrittenLength}",
                    originalReasoning.Length, rewrittenReasoning.Length);
                
                // Discard rewritten text and use original
                return explainability with
                {
                    Reasoning = originalReasoning, // Original remains authoritative (internal)
                    RewrittenReasoning = string.Empty // Rewrite discarded due to safety check failure
                };
            }

            // Rewrite passed safety check - store both versions
            return explainability with
            {
                Reasoning = originalReasoning, // Original remains authoritative (internal)
                RewrittenReasoning = rewrittenReasoning // Rewritten version (user-facing, validated)
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to rewrite reasoning text. Using original reasoning.");
            // If rewrite fails, return original explainability unchanged
            // Original reasoning is already stored in explainability.Reasoning
            return explainability;
        }
    }

    /// <summary>
    /// Validates that rewritten text doesn't contradict or omit key information from original.
    /// Uses simple string containment checks - no AI involved.
    /// </summary>
    /// <param name="originalReasoning">The original authoritative reasoning text.</param>
    /// <param name="rewrittenReasoning">The LLM-rewritten reasoning text.</param>
    /// <returns>True if rewrite is safe (preserves key information), false otherwise.</returns>
    private bool ValidateRewriteSafety(string originalReasoning, string rewrittenReasoning)
    {
        if (string.IsNullOrWhiteSpace(originalReasoning) || string.IsNullOrWhiteSpace(rewrittenReasoning))
        {
            return false;
        }

        // Normalize for comparison (case-insensitive)
        var originalLower = originalReasoning.ToLowerInvariant();
        var rewrittenLower = rewrittenReasoning.ToLowerInvariant();

        // Check 1: Rewritten text should not be significantly shorter (might indicate omissions)
        // Allow up to 30% reduction in length (for clarity improvements)
        var lengthRatio = (double)rewrittenReasoning.Length / originalReasoning.Length;
        if (lengthRatio < 0.7)
        {
            _logger.LogWarning(
                "Rewrite safety check failed: Rewritten text is {Percentage:P0} shorter than original. " +
                "Possible omissions detected.",
                1 - lengthRatio);
            return false;
        }

        // Check 2: Extract and verify key compliance-related terms are preserved
        var keyTerms = ExtractKeyTerms(originalLower);
        if (keyTerms.Count == 0)
        {
            // No key terms found - accept rewrite (original might be very simple)
            return true;
        }

        // Check if key terms appear in rewritten text
        var missingTerms = keyTerms.Where(term => !rewrittenLower.Contains(term)).ToList();
        
        // Allow up to 20% of key terms to be missing (for natural language variations)
        var missingRatio = (double)missingTerms.Count / keyTerms.Count;
        if (missingRatio > 0.2)
        {
            _logger.LogWarning(
                "Rewrite safety check failed: {MissingCount} of {TotalCount} key terms missing from rewrite. " +
                "Missing terms: {MissingTerms}",
                missingTerms.Count, keyTerms.Count, string.Join(", ", missingTerms.Take(5)));
            return false;
        }

        // Check 3: Verify important numbers/facts are preserved
        // Extract numbers from original (gap counts, entity counts, etc.)
        var originalNumbers = ExtractNumbers(originalReasoning);
        if (originalNumbers.Count > 0)
        {
            var rewrittenNumbers = ExtractNumbers(rewrittenReasoning);
            var missingNumbers = originalNumbers.Except(rewrittenNumbers).ToList();
            
            // Critical numbers (like gap counts) must be preserved
            if (missingNumbers.Count > 0)
            {
                _logger.LogWarning(
                    "Rewrite safety check failed: {MissingCount} numbers missing from rewrite. " +
                    "Missing numbers: {MissingNumbers}",
                    missingNumbers.Count, string.Join(", ", missingNumbers.Take(5)));
                return false;
            }
        }

        // All safety checks passed
        return true;
    }

    /// <summary>
    /// Extracts key compliance-related terms from text for safety validation.
    /// Simple string-based extraction - no AI.
    /// </summary>
    private HashSet<string> ExtractKeyTerms(string text)
    {
        var keyTerms = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Compliance-specific key terms that must be preserved
        var importantTerms = new[]
        {
            "gap", "gaps", "finding", "findings", "compliance", "control", "controls",
            "task", "tasks", "evidence", "audit", "regulation", "program",
            "non-compliant", "missing", "required", "validation", "warning", "error"
        };

        foreach (var term in importantTerms)
        {
            if (text.Contains(term, StringComparison.OrdinalIgnoreCase))
            {
                keyTerms.Add(term);
            }
        }

        // Extract entity references (IDs, codes, platform IDs)
        // Look for patterns like "ID:", "Code:", "PlatformId:"
        var idPatterns = new[] { "id:", "code:", "platformid:", "platform id:" };
        foreach (var pattern in idPatterns)
        {
            if (text.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            {
                keyTerms.Add(pattern);
            }
        }

        return keyTerms;
    }

    /// <summary>
    /// Extracts numbers from text for safety validation.
    /// Numbers often represent critical facts (gap counts, entity counts, etc.).
    /// </summary>
    private HashSet<string> ExtractNumbers(string text)
    {
        var numbers = new HashSet<string>();
        
        // Extract numbers using simple regex-like approach
        // Look for patterns like "X gap(s)", "X finding(s)", "X control(s)", etc.
        var numberPatterns = new[]
        {
            @"\d+\s+gap", @"\d+\s+findings?", @"\d+\s+controls?", @"\d+\s+tasks?",
            @"\d+\s+evidence", @"\d+\s+audit", @"\d+\s+validation", @"\d+\s+warning"
        };

        foreach (var pattern in numberPatterns)
        {
            // Simple substring matching for numbers followed by key terms
            var matches = Regex.Matches(
                text, 
                pattern, 
                RegexOptions.IgnoreCase);
            
            foreach (Match match in matches)
            {
                // Extract the number part
                var numberMatch = Regex.Match(
                    match.Value, 
                    @"\d+");
                if (numberMatch.Success)
                {
                    numbers.Add(numberMatch.Value);
                }
            }
        }

        return numbers;
    }

    /// <summary>
    /// Creates a CopilotResponse with enforced Explainability when context or gaps are unavailable.
    /// Uses fallback explainability to ensure Explainability is always present.
    /// </summary>
    /// <param name="intent">The copilot intent.</param>
    /// <param name="summary">The AI-generated summary text.</param>
    /// <param name="findings">The gap findings.</param>
    /// <param name="reason">The reason why normal composition cannot be performed.</param>
    /// <param name="suggestedNextSteps">Optional suggested next steps.</param>
    /// <param name="sourceReferences">Optional source references.</param>
    /// <returns>A CopilotResponse with fallback Explainability.</returns>
    public async Task<CopilotResponse> CreateWithFallbackAsync(
        CopilotIntent intent,
        string summary,
        List<GapResult> findings,
        string reason,
        List<SuggestedNextStep>? suggestedNextSteps = null,
        SourceReferences? sourceReferences = null,
        CancellationToken ct = default)
    {
        // Use fallback explainability when context/gaps are unavailable
        var explainability = _explanationComposer.CreateFallbackExplainability(reason, intent);

        // Guardrail: Verify Explainability is never null
        // This is a safety assertion to prevent null Explainability from being returned
        if (explainability == null)
        {
            _logger.LogError("CRITICAL: Fallback Explainability is null. Creating emergency fallback.");
            // Emergency fallback: Create minimal explainability if even fallback fails
            explainability = new ExplainabilityDto
            {
                Reasoning = "Emergency fallback: Explainability generation failed. Human review required.",
                RulesReferenced = new List<string> { "EmergencyFallback", "HumanReviewRequired" },
                ConfidenceScore = 0.0, // Zero confidence for emergency fallback
                HumanReviewRequired = true,
                GeneratedBy = "ComplianceService.ExplanationComposer.EmergencyFallback",
                GeneratedAt = DateTime.UtcNow
            };
        }

        // Apply LLM rewrite to Reasoning text only (for user-facing clarity)
        // ExplanationComposer output remains authoritative - we only rewrite the Reasoning text
        // RulesReferenced, ConfidenceScore, HumanReviewRequired remain unchanged
        explainability = await ApplyReasoningRewriteAsync(explainability, intent, ct);

        return new CopilotResponse
        {
            QuestionType = intent.QuestionType,
            Summary = summary,
            Findings = findings,
            SuggestedNextSteps = suggestedNextSteps ?? new List<SuggestedNextStep>(),
            Explainability = explainability, // Always set - never null (verified by guardrail above)
            SourceReferences = sourceReferences ?? new SourceReferences()
        };
    }
}
