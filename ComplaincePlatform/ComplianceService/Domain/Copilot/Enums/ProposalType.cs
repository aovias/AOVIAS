namespace ComplianceService.Domain.Copilot.Enums;

/// <summary>
/// Identifies the type of operational action proposed by the Copilot.
/// Each type maps to one or more existing application service commands.
/// No new workflow engines — all execution delegates to existing handlers.
/// </summary>
public enum ProposalType
{
    /// <summary>Create a compliance remediation task linked to a deficiency.</summary>
    CreateTask = 0,

    /// <summary>Assign an existing task to a specific user.</summary>
    AssignTask = 1,

    /// <summary>Create a CAPA (corrective + preventive action) record on a deficiency.</summary>
    CreateCapa = 2,

    /// <summary>Schedule an effectiveness review for an existing CAPA.</summary>
    ScheduleEffectivenessReview = 3,

    /// <summary>Open or generate an evidence checklist for a stage or obligation group.</summary>
    OpenEvidenceChecklist = 4,

    /// <summary>Generate and distribute an executive compliance briefing draft.</summary>
    GenerateExecutiveBriefing = 5,

    /// <summary>Create an audit finding linked to a deficiency or evidence gap.</summary>
    CreateFinding = 6,

    /// <summary>Create a new compliance deficiency record.</summary>
    CreateDeficiency = 7,

    /// <summary>Create a structured accreditation readiness improvement plan.</summary>
    CreateReadinessPlan = 8,

    /// <summary>Create a remediation plan for a cluster of related deficiencies.</summary>
    CreateRemediationPlan = 9,
}
