namespace ComplianceService.Domain;

/// <summary>
/// Defines the supported question types for Compliance Copilot.
/// This enum represents the scope of question types that can be processed by the copilot system.
/// </summary>
public enum CopilotQuestionType
{
    /// <summary>
    /// Question type for understanding why a compliance item is non-compliant.
    /// </summary>
    WhyNonCompliant,

    /// <summary>
    /// Question type for identifying what is missing to achieve compliance.
    /// </summary>
    WhatIsMissing,

    /// <summary>
    /// Question type for preparing for an audit.
    /// </summary>
    AuditPreparation,

    /// <summary>
    /// Question type for previewing questions that an auditor might ask.
    /// </summary>
    AuditorQuestionsPreview
}
