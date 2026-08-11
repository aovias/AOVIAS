using ComplianceService.Domain.Enums;

namespace ComplianceService.Domain;

/// <summary>Seed templates for org-level SLA policies (Sprint 10).</summary>
public static class SlaPolicyDefaults
{
    public sealed record Template(
        DeficiencySeverity Severity,
        string Name,
        int TargetResolutionHours,
        int WarningThresholdPercent,
        IReadOnlyList<EscalationLevelTemplate> EscalationLevels);

    public sealed record EscalationLevelTemplate(
        int Level,
        EscalationTargetType TargetType,
        string TargetLabel,
        string? TargetDesignation,
        int TriggerAtSlaPercent);

    public static readonly IReadOnlyList<Template> Templates =
    [
        new(
            DeficiencySeverity.Critical,
            "Critical — 24 Hour SLA",
            24,
            80,
            [
                new(1, EscalationTargetType.DepartmentOwner, "Department Owner", null, 100),
                new(2, EscalationTargetType.Designation, "Quality Head", "Quality Head", 100),
                new(3, EscalationTargetType.Designation, "Medical Superintendent", "Medical Superintendent", 100),
            ]),
        new(
            DeficiencySeverity.Major,
            "High — 72 Hour SLA",
            72,
            80,
            [
                new(1, EscalationTargetType.DepartmentOwner, "Department Owner", null, 100),
                new(2, EscalationTargetType.Designation, "Quality Head", "Quality Head", 100),
            ]),
        new(
            DeficiencySeverity.Moderate,
            "Medium — 7 Day SLA",
            168,
            80,
            [
                new(1, EscalationTargetType.DepartmentOwner, "Department Owner", null, 100),
            ]),
        new(
            DeficiencySeverity.Minor,
            "Low — 30 Day SLA",
            720,
            80,
            [
                new(1, EscalationTargetType.TaskOwner, "Task Owner", null, 100),
            ]),
    ];
}
