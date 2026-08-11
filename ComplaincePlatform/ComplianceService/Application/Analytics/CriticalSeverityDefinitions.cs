namespace ComplianceService.Application.Analytics;

/// <summary>
/// Documents canonical meanings of "critical" across PRAMAN (Sprint 12B-0).
/// </summary>
public static class CriticalSeverityDefinitions
{
    /// <summary>
    /// <b>Critical Deficiency</b> — operational record on <c>ComplianceDeficiencies</c>
    /// where <c>Severity == Critical</c>. Used by DCC, stage unlock blocking, SLA escalation.
    /// </summary>
    public const string CriticalDeficiency = "ComplianceDeficiency.Severity.Critical";

    /// <summary>
    /// <b>Critical Risk</b> — assessed risk matrix on deficiency (<c>AssessedRiskLevel == Critical</c>).
    /// </summary>
    public const string CriticalRisk = "ComplianceDeficiency.AssessedRiskLevel.Critical";

    /// <summary>
    /// <b>Critical OE Gap / NC</b> — assessment scoring engine output (<c>ComplianceScoreResult.CriticalCount</c>).
    /// Reflects objective-element non-conformance from assessments, NOT deficiency table counts.
    /// </summary>
    public const string CriticalOeGap = "ComplianceScoreResult.CriticalCount";

    /// <summary>
    /// <b>Critical Finding</b> — discovery record (<c>Findings.Severity == Critical</c>), org-scoped.
    /// </summary>
    public const string CriticalFinding = "Finding.Severity.Critical";

    /// <summary>
    /// <b>Open Critical Deficiencies (Unlock)</b> — open critical deficiencies blocking stage unlock.
    /// Same predicate as Critical Deficiency + IsOpen.
    /// </summary>
    public const string OpenCriticalDeficiencyUnlock = "StageUnlock.OpenCriticalDeficiencies";
}
