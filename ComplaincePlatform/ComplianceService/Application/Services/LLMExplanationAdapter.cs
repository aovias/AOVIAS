using ComplianceService.Application.Contracts;
using ComplianceService.Application.DTOs;
using ComplianceService.Domain;

namespace ComplianceService.Application.Services;

/// <summary>
/// Adapter that formats ExplanationInput into a structured prompt for LLM.
/// LLM receives structured inputs only and explains findings (does not detect them).
/// </summary>
public class LLMExplanationAdapter : ILLMExplanationAdapter
{
    /// <summary>
    /// MANDATORY system instruction for rewrite operations (non-configurable).
    /// This instruction is enforced in all rewrite requests and cannot be overridden.
    /// </summary>
    private const string RewriteSystemInstruction = @"=== SYSTEM INSTRUCTION (MANDATORY - NON-CONFIGURABLE) ===
You are rewriting text for clarity only.
You MUST preserve meaning exactly.
You MUST NOT add, remove, or infer any facts.
If unsure, return the text unchanged.
=== END SYSTEM INSTRUCTION ===";

    private readonly ILogger<LLMExplanationAdapter> _logger;

    public LLMExplanationAdapter(ILogger<LLMExplanationAdapter> logger)
    {
        _logger = logger;
    }

    public async Task<string> GenerateExplanationAsync(ExplanationInput input, CancellationToken ct = default)
    {
        if (input == null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        // Build structured prompt for LLM
        var prompt = BuildStructuredPrompt(input);

        _logger.LogInformation("Generating explanation for question type: {QuestionType}", input.QuestionType);

        // TODO: Integrate with actual LLM service (AIOrchestratorService or direct LLM provider)
        // For now, return a structured explanation based on the input
        // This is a placeholder that demonstrates the structure
        
        // In production, this would call:
        // 1. AIOrchestratorService's LLMAdapter via HTTP client, OR
        // 2. Direct LLM provider (Azure OpenAI, Claude, etc.)
        
        // Placeholder implementation
        return await Task.FromResult(GenerateStructuredExplanation(input, prompt));
    }

    private string BuildStructuredPrompt(ExplanationInput input)
    {
        var promptParts = new List<string>();

        promptParts.Add("You are a compliance assistant. Your role is to EXPLAIN findings, not to detect them.");
        promptParts.Add("All findings have already been detected by rule-based analysis.");
        promptParts.Add("");
        promptParts.Add("USER QUESTION:");
        promptParts.Add(input.UserQuestion);
        promptParts.Add("");
        promptParts.Add("QUESTION TYPE:");
        promptParts.Add(input.QuestionType.ToString());
        promptParts.Add("");
        promptParts.Add("COMPLIANCE CONTEXT (facts only):");
        promptParts.Add(input.ContextSummary);
        promptParts.Add("");
        promptParts.Add("GAP ANALYSIS RESULTS (facts only):");
        promptParts.Add(input.GapSummary);
        promptParts.Add("");
        promptParts.Add("APPLIED RULES:");
        promptParts.Add(string.Join(", ", input.AppliedRules));
        promptParts.Add("");
        promptParts.Add("INSTRUCTIONS:");
        promptParts.Add("- Explain the findings in a clear, helpful manner");
        promptParts.Add("- Use the context and gap analysis to provide insights");
        promptParts.Add("- Do NOT make new detections or inferences beyond what's in the data");
        promptParts.Add("- Use cautious, assistive language");
        promptParts.Add("- Focus on explaining what the data shows");
        promptParts.Add("");
        promptParts.Add("Please provide an explanation that helps the user understand the compliance status.");

        return string.Join("\n", promptParts);
    }

    private string GenerateStructuredExplanation(ExplanationInput input, string prompt)
    {
        // Placeholder: Generate explanation based on structured input
        // In production, this would be replaced with actual LLM call
        
        var explanationParts = new List<string>();

        explanationParts.Add($"Based on your question about {input.QuestionType}, here's what the analysis found:");

        // Explain based on question type
        switch (input.QuestionType)
        {
            case CopilotQuestionType.WhyNonCompliant:
                explanationParts.Add("The analysis identified potential reasons for non-compliance:");
                break;
            case CopilotQuestionType.WhatIsMissing:
                explanationParts.Add("The analysis identified what may be missing to achieve compliance:");
                break;
            case CopilotQuestionType.AuditPreparation:
                explanationParts.Add("The analysis provides information to help prepare for an audit:");
                break;
            case CopilotQuestionType.AuditorQuestionsPreview:
                explanationParts.Add("Based on the current compliance status, here are questions an auditor might ask:");
                break;
        }

        explanationParts.Add("");
        explanationParts.Add("Context:");
        explanationParts.Add(input.ContextSummary);
        explanationParts.Add("");
        explanationParts.Add("Findings:");
        explanationParts.Add(input.GapSummary);
        explanationParts.Add("");
        explanationParts.Add("Please review these findings carefully. The analysis is based on the data available and may require additional context or verification.");

        return string.Join("\n", explanationParts);
    }

    /// <summary>
    /// Rewrites an existing explanation text to improve clarity and readability.
    /// This is a REWRITE operation, not a generation operation.
    /// </summary>
    public async Task<string> RewriteExplanationAsync(ExplanationRewriteRequest request, CancellationToken ct = default)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.OriginalExplanationText))
        {
            _logger.LogWarning("RewriteExplanationAsync called with empty original text. Returning empty string.");
            return string.Empty;
        }

        try
        {
            // Build structured prompt for rewrite
            var prompt = BuildRewritePrompt(request);

            _logger.LogInformation("Rewriting explanation for audience: {AudienceType}, tone: {Tone}", 
                request.AudienceType, request.Tone);

            // TODO: Integrate with actual LLM service (AIOrchestratorService or direct LLM provider)
            // For now, return a placeholder rewrite that demonstrates the structure
            // In production, this would call:
            // 1. AIOrchestratorService's LLMAdapter via HTTP client, OR
            // 2. Direct LLM provider (Azure OpenAI, Claude, etc.)
            
            // Placeholder implementation
            return await Task.FromResult(RewriteExplanationText(request, prompt));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to rewrite explanation. Returning original text unchanged.");
            // Rule: If rewrite fails, return original text unchanged
            return request.OriginalExplanationText;
        }
    }

    private string BuildRewritePrompt(ExplanationRewriteRequest request)
    {
        var promptParts = new List<string>();

        // MANDATORY SYSTEM INSTRUCTION (non-configurable)
        // This instruction is enforced in ALL rewrite requests and cannot be overridden or configured.
        // It is defined as a class constant to ensure it cannot be changed.
        promptParts.Add(RewriteSystemInstruction);
        promptParts.Add("");

        promptParts.Add("ORIGINAL EXPLANATION:");
        promptParts.Add(request.OriginalExplanationText);
        promptParts.Add("");
        promptParts.Add("REWRITE REQUIREMENTS:");
        promptParts.Add($"- Target Audience: {request.AudienceType}");
        promptParts.Add($"- Desired Tone: {request.Tone}");
        promptParts.Add("");
        promptParts.Add("REWRITE RULES (STRICT - in addition to system instruction above):");
        promptParts.Add("- Preserve the SAME MEANING as the original (enforced by system instruction)");
        promptParts.Add("- Improve CLARITY: Make the explanation easier to understand");
        promptParts.Add("- Improve READABILITY: Make the text flow better and be more engaging");
        promptParts.Add("- Do NOT add new entities: Do not introduce new facts, data, or entities not in the original");
        promptParts.Add("- Do NOT add new conclusions: Do not infer or add conclusions not present in the original");
        promptParts.Add("- Do NOT change intent: The rewritten text must convey the same intent as the original");
        promptParts.Add("- Do NOT remove facts: All factual information from the original must be preserved");
        promptParts.Add("");
        promptParts.Add("Your task is to rewrite the explanation to be:");
        
        // Add audience-specific guidance
        switch (request.AudienceType)
        {
            case ExplanationAudienceType.Admin:
                promptParts.Add("- Suitable for administrative audience (high-level overview)");
                break;
            case ExplanationAudienceType.ComplianceOfficer:
                promptParts.Add("- Suitable for compliance officers (detailed technical information)");
                break;
            case ExplanationAudienceType.Auditor:
                promptParts.Add("- Suitable for auditors (structured, evidence-focused)");
                break;
        }

        // Add tone-specific guidance
        switch (request.Tone)
        {
            case ExplanationTone.Neutral:
                promptParts.Add("- Neutral tone: Objective and factual without emotional coloring");
                break;
            case ExplanationTone.Supportive:
                promptParts.Add("- Supportive tone: Helpful and assistive, guiding the reader");
                break;
            case ExplanationTone.Formal:
                promptParts.Add("- Formal tone: Professional and structured, suitable for official documentation");
                break;
        }

        promptParts.Add("");
        promptParts.Add("Please rewrite the explanation following these requirements while preserving all original meaning and facts.");

        return string.Join("\n", promptParts);
    }

    private string RewriteExplanationText(ExplanationRewriteRequest request, string prompt)
    {
        // Placeholder: Perform rewrite based on request
        // In production, this would be replaced with actual LLM call
        
        // For now, return a simple rewrite that demonstrates the structure
        // This is a placeholder - actual implementation would call LLM
        
        var rewrittenParts = new List<string>();
        
        // Add audience-appropriate header
        switch (request.AudienceType)
        {
            case ExplanationAudienceType.Admin:
                rewrittenParts.Add("[Administrative Summary]");
                break;
            case ExplanationAudienceType.ComplianceOfficer:
                rewrittenParts.Add("[Compliance Analysis]");
                break;
            case ExplanationAudienceType.Auditor:
                rewrittenParts.Add("[Audit-Ready Summary]");
                break;
        }
        
        rewrittenParts.Add("");
        
        // Apply tone-appropriate formatting
        var originalText = request.OriginalExplanationText;
        
        // Placeholder rewrite: In production, this would be the actual LLM response
        // For now, we return the original with a note that this is a placeholder
        // In real implementation, the LLM would rewrite it according to audience and tone
        
        // TODO: Replace with actual LLM call
        // For now, return original text (placeholder behavior)
        // In production, this would call the LLM and return the rewritten text
        
        _logger.LogWarning("RewriteExplanationText is using placeholder implementation. Actual LLM integration needed.");
        
        // Return original text as placeholder (in production, this would be the LLM rewrite)
        // The actual implementation would:
        // 1. Call LLM with the prompt
        // 2. Validate the rewrite preserves meaning and facts
        // 3. Return the rewritten text
        // 4. If validation fails or LLM call fails, return original text
        
        return originalText; // Placeholder: returns original unchanged
    }
}
