using ComplianceService.Application.Contracts;
using ComplianceService.Domain;
using Shared.Infrastructure.DTO;
using Shared.Infrastructure.Services;

namespace ComplianceService.Application.Services;

/// <summary>
/// Composes explainability metadata and prepares explanation input.
/// Responsibilities:
/// - Accept: CopilotIntent, ComplianceContext, List&lt;Gap&gt;
/// - Generate Explainability metadata via ReasoningCapture pattern
/// 
/// Explanation Design (Phase 3):
/// - Explanations are intentionally aggregate: A single explanation summarizes all findings
/// - Individual gaps remain structured and traceable: Each Gap in the input list maintains
///   its structure (GapType, Description, AffectedEntityType, AffectedEntityId, RulesReferenced)
/// - Explanation summarizes findings for human review: The reasoning text provides an
///   aggregate summary of context, findings, and evidence references
/// - Per-gap explanations are intentionally not generated in Phase 3: This design decision
///   ensures the explanation remains focused on the overall analysis rather than individual gap details
/// 
/// Explanation rules:
/// - Summarize detected gaps clearly
/// - Reference affected entities and rules explicitly
/// - If no gaps are detected, explain that no gaps were found
/// - If context is incomplete, explain limitations clearly
/// - Use conservative, assistive language only
/// 
/// Constraints:
/// - Do NOT detect new gaps
/// - Do NOT infer severity or risk
/// - Do NOT predict outcomes
/// - Do NOT call AI directly
/// </summary>
public class ExplanationComposer : IExplanationComposer
{
    private readonly ILogger<ExplanationComposer> _logger;

    public ExplanationComposer(ILogger<ExplanationComposer> logger)
    {
        _logger = logger;
    }

    public ExplanationCompositionResult Compose(
        CopilotIntent intent,
        ComplianceContext context,
        List<Gap> gaps)
    {
        if (intent == null)
        {
            throw new ArgumentNullException(nameof(intent));
        }

        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (gaps == null)
        {
            throw new ArgumentNullException(nameof(gaps));
        }

        // Build rules referenced
        var rulesReferenced = BuildRulesReferenced(intent, context, gaps);

        // Build reasoning (cautious, assistive language, facts only)
        // NOTE: This generates an aggregate explanation that summarizes all gaps together.
        // Individual gaps remain structured and traceable in the input List<Gap> parameter.
        // Per-gap explanations are intentionally not generated in Phase 3 - the explanation
        // summarizes findings for human review rather than providing individual gap details.
        var reasoning = BuildReasoning(intent, context, gaps, rulesReferenced);

        // Calculate conservative confidence score
        var confidenceScore = CalculateConservativeConfidence(intent, context, gaps);

        // Build explainability DTO
        var explainability = new ExplainabilityDto
        {
            Reasoning = reasoning,
            RulesReferenced = rulesReferenced,
            ConfidenceScore = confidenceScore,
            HumanReviewRequired = true, // Always require human review
            GeneratedBy = "ComplianceService.ExplanationComposer",
            GeneratedAt = DateTime.UtcNow
        };

        // Build explanation input for LLMAdapter
        var explanationInput = BuildExplanationInput(intent, context, gaps, rulesReferenced);

        return new ExplanationCompositionResult
        {
            Explainability = explainability,
            ExplanationInput = explanationInput
        };
    }

    private List<string> BuildRulesReferenced(
        CopilotIntent intent,
        ComplianceContext context,
        List<Gap> gaps)
    {
        var rules = new List<string>();

        // Add question type rule
        rules.Add($"QuestionType: {intent.QuestionType}");

        // Add gap detection rules from the gaps themselves
        var gapTypes = gaps.Select(g => g.GapType).Distinct().ToList();
        foreach (var gapType in gapTypes)
        {
            rules.Add($"GapDetection: {gapType}");
        }

        // Add rules referenced from gaps
        foreach (var gap in gaps)
        {
            foreach (var rule in gap.RulesReferenced)
            {
                if (!rules.Contains(rule))
                {
                    rules.Add(rule);
                }
            }
        }

        // Add context validation rules
        if (!context.IsValid)
        {
            rules.Add("ContextValidation: PartialData");
        }

        // Add entity presence rules
        if (context.ComplianceProgram != null)
        {
            rules.Add("Entity: ComplianceProgram");
        }

        if (context.Control != null)
        {
            rules.Add("Entity: Control");
        }

        if (context.ComplianceTask != null)
        {
            rules.Add("Entity: ComplianceTask");
        }

        if (context.Regulation != null)
        {
            rules.Add("Entity: Regulation");
        }

        if (context.ComplianceStandard != null)
        {
            rules.Add("Entity: ComplianceStandard");
        }

        if (context.AuditSession != null)
        {
            rules.Add("Entity: AuditSession");
        }

        if (context.AuditFindings.Count > 0)
        {
            rules.Add("Entity: AuditFindings");
        }

        return rules;
    }

    private string BuildReasoning(
        CopilotIntent intent,
        ComplianceContext context,
        List<Gap> gaps,
        List<string> rulesReferenced)
    {
        var reasoningParts = new List<string>();

        // 1. Context summary (what was analyzed)
        reasoningParts.Add("Context Summary:");
        reasoningParts.Add("Based on available information, the following was analyzed:");
        
        // Add question context (factual only)
        reasoningParts.Add($"- User question: \"{intent.OriginalQuestion}\"");
        reasoningParts.Add($"- Question type: {intent.QuestionType}");

        // Add context facts (no inference, no absolute claims)
        var contextItems = new List<string>();
        if (context.ComplianceProgram != null)
        {
            contextItems.Add($"compliance program {context.ComplianceProgram.PlatformId} ({context.ComplianceProgram.Name})");
        }

        if (context.Control != null)
        {
            contextItems.Add($"control {context.Control.Code}");
        }

        if (context.Regulation != null)
        {
            contextItems.Add($"regulation with status {context.Regulation.Status}");
        }

        if (context.ComplianceStandard != null)
        {
            contextItems.Add($"compliance standard {context.ComplianceStandard.Code} ({context.ComplianceStandard.Name})");
        }

        if (context.ComplianceTask != null)
        {
            contextItems.Add($"compliance task {context.ComplianceTask.Id}");
        }

        if (context.ProgramControls.Count > 0)
        {
            contextItems.Add($"{context.ProgramControls.Count} control(s)");
        }

        if (context.ProgramTasks.Count > 0)
        {
            contextItems.Add($"{context.ProgramTasks.Count} task(s)");
        }

        if (context.Evidence.Count > 0)
        {
            contextItems.Add($"{context.Evidence.Count} evidence item(s)");
        }

        if (context.AuditSession != null)
        {
            contextItems.Add($"audit session {context.AuditSession.PlatformId}");
        }

        if (context.AuditFindings.Count > 0)
        {
            contextItems.Add($"{context.AuditFindings.Count} audit finding(s)");
        }

        if (contextItems.Any())
        {
            reasoningParts.Add($"- Analyzed entities: {string.Join(", ", contextItems)}");
        }

        // Note limitations if context is incomplete
        if (!context.IsValid)
        {
            reasoningParts.Add($"- Note: {context.ValidationErrors.Count} validation warning(s) were present. " +
                             "Some data may be incomplete based on available information.");
        }

        reasoningParts.Add("");

        // 2. Findings summary (what gaps were detected)
        // NOTE: This is an aggregate summary of all gaps. Individual gaps remain structured
        // and traceable in the List<Gap> parameter - they are not lost or hidden.
        // Per-gap explanations are intentionally not generated in Phase 3.
        reasoningParts.Add("Findings Summary:");
        reasoningParts.Add("Based on available information:");
        
        if (gaps.Count > 0)
        {
            reasoningParts.Add($"- {gaps.Count} potential gap(s) were identified in the analyzed context.");
            
            // Group gaps by type for aggregate summary
            // Individual gaps remain accessible via the List<Gap> parameter for traceability
            var gapCountsByType = gaps.GroupBy(g => g.GapType)
                .ToDictionary(g => g.Key, g => g.Count());
            
            var gapTypeDescriptions = gapCountsByType.Select(kvp => 
                $"{kvp.Value} {kvp.Key} gap(s)").ToList();
            if (gapTypeDescriptions.Any())
            {
                reasoningParts.Add($"- Gap types identified: {string.Join(", ", gapTypeDescriptions)}");
            }
        }
        else
        {
            reasoningParts.Add("- No gaps were identified in the analyzed context based on available information.");
        }

        reasoningParts.Add("");

        // 3. Evidence references (entity IDs, rules)
        reasoningParts.Add("Evidence References:");
        reasoningParts.Add("Based on available information, the following references were used:");

        // Reference affected entities explicitly
        if (gaps.Count > 0)
        {
            var affectedEntityRefs = gaps
                .Where(g => g.AffectedEntityId.HasValue)
                .GroupBy(g => new { g.AffectedEntityType, g.AffectedEntityId })
                .Select(g => $"{g.Key.AffectedEntityType} (ID: {g.Key.AffectedEntityId})")
                .Distinct()
                .ToList();
            
            if (affectedEntityRefs.Any())
            {
                reasoningParts.Add($"- Affected entities: {string.Join(", ", affectedEntityRefs)}");
            }

            // Reference rules from gaps
            var allRulesFromGaps = gaps
                .SelectMany(g => g.RulesReferenced)
                .Distinct()
                .ToList();
            
            if (allRulesFromGaps.Any())
            {
                reasoningParts.Add($"- Rules referenced in gap detection: {string.Join(", ", allRulesFromGaps)}");
            }
        }

        // Reference general rules applied
        if (rulesReferenced.Any())
        {
            reasoningParts.Add($"- Rules applied during analysis: {string.Join(", ", rulesReferenced)}");
        }

        // Reference entity IDs from intent
        var intentEntityRefs = new List<string>();
        if (intent.ProgramId.HasValue)
        {
            intentEntityRefs.Add($"Program ID: {intent.ProgramId.Value}");
        }
        if (intent.ControlId.HasValue)
        {
            intentEntityRefs.Add($"Control ID: {intent.ControlId.Value}");
        }
        if (intent.TaskId.HasValue)
        {
            intentEntityRefs.Add($"Task ID: {intent.TaskId.Value}");
        }
        if (intent.RegulationId.HasValue)
        {
            intentEntityRefs.Add($"Regulation ID: {intent.RegulationId.Value}");
        }
        if (intent.AuditId.HasValue)
        {
            intentEntityRefs.Add($"Audit ID: {intent.AuditId.Value}");
        }

        if (intentEntityRefs.Any())
        {
            reasoningParts.Add($"- Entity identifiers from request: {string.Join(", ", intentEntityRefs)}");
        }

        reasoningParts.Add("");

        // 4. Human review notice
        reasoningParts.Add("Human Review Notice:");
        reasoningParts.Add("This analysis is based on available information at the time of processing. " +
                         "The findings presented are not absolute claims or compliance declarations. " +
                         "Human review is required to validate findings, verify completeness of available information, " +
                         "and determine appropriate actions based on your organization's specific context and requirements.");

        return string.Join("\n", reasoningParts);
    }

    private double CalculateConservativeConfidence(
        CopilotIntent intent,
        ComplianceContext context,
        List<Gap> gaps)
    {
        // Start with base confidence
        double confidence = 0.5; // Conservative base

        // Adjust based on context completeness
        if (context.IsValid)
        {
            confidence += 0.1; // Valid context increases confidence slightly
        }
        else
        {
            confidence -= 0.2; // Invalid context decreases confidence
        }

        // Adjust based on entity presence
        var entityCount = 0;
        if (context.ComplianceProgram != null) entityCount++;
        if (context.Control != null) entityCount++;
        if (context.ComplianceTask != null) entityCount++;
        if (context.Regulation != null) entityCount++;
        if (context.ComplianceStandard != null) entityCount++;

        if (entityCount >= 3)
        {
            confidence += 0.1; // More entities = slightly more confidence
        }
        else if (entityCount < 2)
        {
            confidence -= 0.15; // Few entities = less confidence
        }

        // Adjust based on gap presence (not severity - we don't infer that)
        if (gaps.Count > 0)
        {
            // Having gaps detected suggests the analysis worked
            confidence += 0.05;
        }

        // Adjust based on question type clarity
        if (intent.QuestionType != CopilotQuestionType.WhyNonCompliant) // Default fallback
        {
            confidence += 0.05; // Specific question type = slightly more confidence
        }

        // Clamp between 0.2 and 0.7 (conservative range)
        confidence = Math.Max(0.2, Math.Min(0.7, confidence));

        return Math.Round(confidence, 2);
    }

    private ExplanationInput BuildExplanationInput(
        CopilotIntent intent,
        ComplianceContext context,
        List<Gap> gaps,
        List<string> rulesReferenced)
    {
        var input = new ExplanationInput
        {
            UserQuestion = intent.OriginalQuestion,
            QuestionType = intent.QuestionType,
            AppliedRules = rulesReferenced
        };

        // Build context summary (facts only, no inference)
        input.ContextSummary = BuildContextSummary(context);

        // Build gap summary (facts only, no inference)
        input.GapSummary = BuildGapSummary(gaps);

        // Build entity references
        input.EntityReferences = BuildEntityReferences(intent, context);

        return input;
    }

    private string BuildContextSummary(ComplianceContext context)
    {
        var summaryParts = new List<string>();

        summaryParts.Add("Compliance Context Summary (based on available information):");

        if (context.ComplianceProgram != null)
        {
            summaryParts.Add($"- Program: {context.ComplianceProgram.PlatformId} ({context.ComplianceProgram.Name})");
        }

        if (context.Control != null)
        {
            summaryParts.Add($"- Control: {context.Control.Code} - {context.Control.Description}");
        }

        if (context.Regulation != null)
        {
            summaryParts.Add($"- Regulation: Status={context.Regulation.Status}, " +
                           $"StartDate={context.Regulation.StartDate:yyyy-MM-dd}");
        }

        if (context.ProgramControls.Count > 0)
        {
            summaryParts.Add($"- Program contains {context.ProgramControls.Count} control(s) (based on available information)");
        }

        if (context.ProgramTasks.Count > 0)
        {
            summaryParts.Add($"- Program contains {context.ProgramTasks.Count} task(s) (based on available information)");
        }

        if (context.Evidence.Count > 0)
        {
            summaryParts.Add($"- {context.Evidence.Count} evidence item(s) present (based on available information)");
        }

        if (context.AuditSession != null)
        {
            summaryParts.Add($"- Audit Session: {context.AuditSession.PlatformId}, Status={context.AuditSession.Status}");
        }

        if (context.AuditFindings.Count > 0)
        {
            summaryParts.Add($"- {context.AuditFindings.Count} audit finding(s) present (based on available information)");
        }

        if (!context.IsValid)
        {
            summaryParts.Add($"- Note: {context.ValidationErrors.Count} validation warning(s) present. " +
                           "Some data may be incomplete based on available information.");
        }

        return string.Join("\n", summaryParts);
    }

    /// <summary>
    /// Builds an aggregate summary of gaps for explanation input.
    /// NOTE: This is intentionally an aggregate summary, not per-gap explanations.
    /// Individual gaps remain structured and traceable in the List<Gap> parameter.
    /// Per-gap explanations are intentionally not generated in Phase 3.
    /// </summary>
    private string BuildGapSummary(List<Gap> gaps)
    {
        if (gaps.Count == 0)
        {
            return "Based on available information, no gaps were identified in the analyzed context.";
        }

        // Aggregate summary: Groups gaps by type and entity for human review.
        // Individual gaps remain accessible and traceable via the List<Gap> parameter.
        var summaryParts = new List<string>();
        summaryParts.Add($"Based on available information, {gaps.Count} potential gap(s) were identified:");

        // Group gaps by type
        var gapCountsByType = gaps.GroupBy(g => g.GapType)
            .ToDictionary(g => g.Key, g => g.Count());

        summaryParts.Add("Gap types (based on available information):");
        foreach (var kvp in gapCountsByType)
        {
            summaryParts.Add($"  - {kvp.Key}: {kvp.Value} gap(s)");
        }

        // Group by affected entity type
        var gapsByEntityType = gaps
            .Where(g => g.AffectedEntityId.HasValue)
            .GroupBy(g => g.AffectedEntityType)
            .ToList();

        if (gapsByEntityType.Any())
        {
            summaryParts.Add("Affected entities (based on available information):");
            foreach (var group in gapsByEntityType)
            {
                summaryParts.Add($"  - {group.Key}: {group.Count()} gap(s)");
            }
        }

        // List first few gaps (factual descriptions only, no severity inference, no absolute claims)
        // NOTE: This shows sample gaps for context, but this is still an aggregate summary.
        // Individual gaps remain structured and traceable - each Gap maintains its GapType,
        // Description, AffectedEntityType, AffectedEntityId, and RulesReferenced.
        // Per-gap explanations are intentionally not generated in Phase 3.
        summaryParts.Add("Sample gaps identified (first 5, based on available information):");
        foreach (var gap in gaps.Take(5))
        {
            var entityRef = gap.AffectedEntityId.HasValue 
                ? $" (Entity: {gap.AffectedEntityType}, ID: {gap.AffectedEntityId})" 
                : "";
            summaryParts.Add($"  - {gap.GapType}: {gap.Description}{entityRef}");
            
            // Reference rules explicitly if present
            // Individual gap structure is preserved - this summary does not replace it
            if (gap.RulesReferenced.Any())
            {
                summaryParts.Add($"    Rules referenced: {string.Join(", ", gap.RulesReferenced)}");
            }
        }

        if (gaps.Count > 5)
        {
            summaryParts.Add($"  ... and {gaps.Count - 5} more gap(s) (based on available information)");
        }

        return string.Join("\n", summaryParts);
    }

    private Dictionary<string, string> BuildEntityReferences(CopilotIntent intent, ComplianceContext context)
    {
        var references = new Dictionary<string, string>();

        if (intent.ProgramId.HasValue)
        {
            references["ProgramId"] = intent.ProgramId.Value.ToString();
        }

        if (intent.ControlId.HasValue)
        {
            references["ControlId"] = intent.ControlId.Value.ToString();
        }

        if (intent.RegulationId.HasValue)
        {
            references["RegulationId"] = intent.RegulationId.Value.ToString();
        }

        if (intent.TaskId.HasValue)
        {
            references["TaskId"] = intent.TaskId.Value.ToString();
        }

        if (context.ComplianceProgram != null)
        {
            references["ProgramPlatformId"] = context.ComplianceProgram.PlatformId;
        }

        if (context.Control != null)
        {
            references["ControlCode"] = context.Control.Code;
        }

        if (context.Regulation != null)
        {
            references["RegulationStatus"] = context.Regulation.Status;
        }

        if (context.ComplianceStandard != null)
        {
            references["ComplianceStandardCode"] = context.ComplianceStandard.Code;
        }

        return references;
    }

    /// <summary>
    /// Creates a safe fallback Explainability when normal composition cannot be performed.
    /// This ensures CopilotResponse always has Explainability, even when errors occur.
    /// </summary>
    public ExplainabilityDto CreateFallbackExplainability(string reason, CopilotIntent? intent = null)
    {
        _logger.LogWarning("Creating fallback explainability. Reason: {Reason}", reason);

        var reasoningParts = new List<string>();
        reasoningParts.Add("Context Summary:");
        reasoningParts.Add("Based on available information, explainability metadata could not be fully generated.");
        reasoningParts.Add("");
        reasoningParts.Add("Findings Summary:");
        reasoningParts.Add("Analysis was limited due to: " + reason);
        reasoningParts.Add("");
        reasoningParts.Add("Evidence References:");
        reasoningParts.Add("Limited references available due to incomplete context.");
        
        if (intent != null)
        {
            reasoningParts.Add($"- User question: \"{intent.OriginalQuestion}\"");
            reasoningParts.Add($"- Question type: {intent.QuestionType}");
        }
        
        reasoningParts.Add("");
        reasoningParts.Add("Human Review Notice:");
        reasoningParts.Add("This analysis is based on limited available information. " +
                         "The explainability metadata could not be fully generated due to: " + reason + ". " +
                         "Human review is required to validate findings and determine appropriate actions. " +
                         "Please review the compliance context and ensure all required data is available.");

        var rulesReferenced = new List<string> { "FallbackExplainability", "HumanReviewRequired" };
        if (intent != null)
        {
            rulesReferenced.Add($"QuestionType: {intent.QuestionType}");
        }

        return new ExplainabilityDto
        {
            Reasoning = string.Join("\n", reasoningParts),
            RulesReferenced = rulesReferenced,
            ConfidenceScore = 0.1, // Very low confidence for fallback
            HumanReviewRequired = true, // Always require human review for fallback
            GeneratedBy = "ComplianceService.ExplanationComposer.Fallback",
            GeneratedAt = DateTime.UtcNow
        };
    }
}
