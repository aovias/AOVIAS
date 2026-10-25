using ComplianceService.Application.Contracts;
using ComplianceService.Domain;

namespace ComplianceService.Application.Services;

/// <summary>
/// Rule-based router that maps user questions to CopilotQuestionType using simple, deterministic rules.
/// No NLP libraries, no AI calls, no fuzzy matching.
/// If intent cannot be determined, returns a safe default with HumanReviewRequired = true.
/// </summary>
public class CopilotIntentRouter : ICopilotIntentRouter
{
    public CopilotIntent Route(
        string userQuestion,
        Guid? programId = null,
        Guid? controlId = null,
        Guid? taskId = null,
        Guid? auditId = null)
    {
        // Rule: Handle null or empty questions with safe default
        if (string.IsNullOrWhiteSpace(userQuestion))
        {
            return new CopilotIntent
            {
                QuestionType = CopilotQuestionType.WhyNonCompliant, // Safe default
                OriginalQuestion = string.Empty,
                TargetEntityType = DetermineTargetEntityType(programId, controlId, taskId, auditId),
                ProgramId = programId,
                ControlId = controlId,
                TaskId = taskId,
                AuditId = auditId
            };
        }

        var lowerQuestion = userQuestion.ToLowerInvariant().Trim();
        var intent = new CopilotIntent
        {
            OriginalQuestion = userQuestion,
            ProgramId = programId,
            ControlId = controlId,
            TaskId = taskId,
            AuditId = auditId
        };

        // Rule: Determine target entity type based on provided IDs (priority order matters)
        intent.TargetEntityType = DetermineTargetEntityType(programId, controlId, taskId, auditId);

        // Rule: Determine question type using simple, deterministic keyword matching
        // No fuzzy matching - exact substring checks only
        intent.QuestionType = DetermineQuestionType(lowerQuestion);

        return intent;
    }

    /// <summary>
    /// Determines the target entity type based on provided context identifiers.
    /// Priority: Audit > Task > Control > Program > None
    /// </summary>
    private static TargetEntityType DetermineTargetEntityType(
        Guid? programId,
        Guid? controlId,
        Guid? taskId,
        Guid? auditId)
    {
        // Rule: If AuditId is provided, target is Audit (highest priority)
        if (auditId.HasValue && auditId.Value != Guid.Empty)
        {
            return TargetEntityType.Audit;
        }

        // Rule: If TaskId is provided, target is Task
        if (taskId.HasValue && taskId.Value != Guid.Empty)
        {
            return TargetEntityType.Task;
        }

        // Rule: If ControlId is provided, target is Control
        if (controlId.HasValue && controlId.Value != Guid.Empty)
        {
            return TargetEntityType.Control;
        }

        // Rule: If ProgramId is provided, target is Program
        if (programId.HasValue && programId.Value != Guid.Empty)
        {
            return TargetEntityType.Program;
        }

        // Rule: No identifiers provided, target is None
        return TargetEntityType.None;
    }

    /// <summary>
    /// Determines the question type using simple, deterministic keyword matching.
    /// Rules are checked in order - first match wins.
    /// No NLP, no AI, no fuzzy matching - exact substring checks only.
    /// </summary>
    private static CopilotQuestionType DetermineQuestionType(string lowerQuestion)
    {
        // Rule: "why" + "non-compliant" → WhyNonCompliant
        // Check for both keywords present (order doesn't matter)
        if (lowerQuestion.Contains("why", StringComparison.OrdinalIgnoreCase) &&
            lowerQuestion.Contains("non-compliant", StringComparison.OrdinalIgnoreCase))
        {
            return CopilotQuestionType.WhyNonCompliant;
        }

        // Rule: "missing" → WhatIsMissing
        // Simple substring check - if "missing" appears anywhere, it's WhatIsMissing
        if (lowerQuestion.Contains("missing", StringComparison.OrdinalIgnoreCase))
        {
            return CopilotQuestionType.WhatIsMissing;
        }

        // Rule: "prepare" + "audit" → AuditPreparation
        // Check for both keywords present (order doesn't matter)
        if (lowerQuestion.Contains("prepare", StringComparison.OrdinalIgnoreCase) &&
            lowerQuestion.Contains("audit", StringComparison.OrdinalIgnoreCase))
        {
            return CopilotQuestionType.AuditPreparation;
        }

        // Rule: "auditor" + "ask" → AuditorQuestionsPreview
        // Check for both keywords present (order doesn't matter)
        if (lowerQuestion.Contains("auditor", StringComparison.OrdinalIgnoreCase) &&
            lowerQuestion.Contains("ask", StringComparison.OrdinalIgnoreCase))
        {
            return CopilotQuestionType.AuditorQuestionsPreview;
        }

        // Rule: Safe default if intent cannot be determined
        // Return WhyNonCompliant as safe default - requires human review
        // This ensures we don't make assumptions about user intent
        return CopilotQuestionType.WhyNonCompliant;
    }
}
