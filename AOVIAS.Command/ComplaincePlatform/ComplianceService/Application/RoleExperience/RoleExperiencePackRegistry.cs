using ComplianceService.Domain.Copilot;
using Shared.Infrastructure.Auth;

namespace ComplianceService.Application.RoleExperience;

/// <summary>
/// Code-first role experience packs (Phase 3 Sprint 1).
/// Maps ApplicationRoles / WebApp role aliases → Copilot persona, home route, widget allow-list, and nav sections.
/// Staff / Doctor / Nurse packs are deferred.
/// </summary>
public static class RoleExperiencePackRegistry
{
    public const string RolePackPermissionPrefix = "role-pack:";

    private static readonly IReadOnlyList<RoleExperiencePack> Packs = BuildPacks();

    public static IReadOnlyList<RoleExperiencePack> GetAll() => Packs;

    public static string ToPermissionTag(string packId) => $"{RolePackPermissionPrefix}{packId}";

    /// <summary>
    /// Resolves the highest-priority pack matching any of the given role / alias strings.
    /// Falls back to Quality Manager when no match.
    /// </summary>
    public static RoleExperiencePack Resolve(IEnumerable<string>? roles)
    {
        var list = (roles ?? Array.Empty<string>())
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Select(r => r.Trim())
            .ToList();

        if (list.Count == 0)
            return GetById("quality-manager");

        // Priority: admin → auditor → department-head (alias before MANAGER) → consultant → manager → default
        foreach (var packId in new[] { "hospital-admin", "auditor", "department-head", "consultant", "quality-manager" })
        {
            var pack = GetById(packId);
            if (list.Any(r => pack.MatchesRole(r)))
                return pack;
        }

        return GetById("quality-manager");
    }

    public static RoleExperiencePack GetById(string packId) =>
        Packs.FirstOrDefault(p => string.Equals(p.PackId, packId, StringComparison.OrdinalIgnoreCase))
        ?? Packs.First(p => p.PackId == "quality-manager");

    public static bool TryResolvePersona(IEnumerable<string>? roles, out CopilotPersona persona)
    {
        var pack = Resolve(roles);
        return Enum.TryParse(pack.Persona, ignoreCase: true, out persona);
    }

    private static IReadOnlyList<RoleExperiencePack> BuildPacks() =>
    [
        new RoleExperiencePack
        {
            PackId = "hospital-admin",
            DisplayName = "Hospital Admin / Owner",
            Persona = nameof(CopilotPersona.ExecutiveAdvisor),
            DefaultHomeRoute = "/app/dashboard?persona=hospital-admin",
            RoleAliases =
            [
                ApplicationRoles.ORG_ADMIN,
                ApplicationRoles.GROUP_ADMIN,
                ApplicationRoles.SUPER_ADMIN,
                "HospitalOwner",
                "HospitalAdmin",
                "Hospital Admin",
                "Hospital Owner",
                "Administrator",
            ],
            WidgetAllowList =
            [
                "home-executive-kpi-strip",
                "home-alert-banner",
                "home-compliance-gauge",
                "home-hospital-overview",
                "home-framework-overview",
                "home-department-heatmap",
                "home-risk-distribution",
                "home-milestones",
                "home-score-trend",
                "cc-executive-kpi-bar",
                "cc-audit-readiness",
                "cc-risk-command",
                "cc-ai-intelligence",
            ],
            NavSectionIds = ["dashboard", "action-center"],
            DefaultQuickActions =
            [
                new("Generate report", "/app/reports/readiness"),
                new("Review risks", "/app/ccc/risk-escalation"),
                new("Audit calendar", "/app/audit/calendar"),
                new("Approvals", "/app/governance/approvals"),
                new("Department performance", "/app/analytics/departments"),
                new("Maturity", "/app/compliance-maturity"),
            ],
        },
        new RoleExperiencePack
        {
            PackId = "quality-manager",
            DisplayName = "Quality Manager",
            Persona = nameof(CopilotPersona.QualityManagerAssistant),
            // Operational home: composed QM workspace (deep-links Tasks / DCC / evidence / audit).
            DefaultHomeRoute = "/app/role-homes/qm",
            RoleAliases =
            [
                ApplicationRoles.MANAGER,
                "QualityManager",
                "Quality Manager",
                "Compliance Officer",
            ],
            WidgetAllowList =
            [
                "home-executive-kpi-strip",
                "home-alert-banner",
                "home-compliance-gauge",
                "home-department-heatmap",
                "home-risk-distribution",
                "home-milestones",
                "home-framework-overview",
                "home-task-type-breakdown",
                "cc-action-center",
                "cc-smart-alerts",
                "cc-risk-command",
                "cc-compliance-analytics",
            ],
            NavSectionIds = ["dashboard", "compliance-execution", "action-center"],
            DefaultQuickActions =
            [
                new("Assign work", "/app/tasks"),
                new("Review evidence", "/app/evidence/review-queue"),
                new("Launch audit", "/app/audit/planning"),
                new("Generate CAPA", "/app/audit/corrective-actions"),
                new("View trends", "/app/analytics"),
                new("Control center", "/app/control-center"),
            ],
        },
        new RoleExperiencePack
        {
            PackId = "department-head",
            DisplayName = "Department Head",
            Persona = nameof(CopilotPersona.DepartmentHeadAssistant),
            DefaultHomeRoute = "/app/department-head",
            RoleAliases =
            [
                ApplicationRoles.DEPARTMENT_HEAD,
                "DepartmentHead",
                "DEPARTMENT_HEAD",
                "Department Head",
                "DeptHead",
            ],
            WidgetAllowList =
            [
                "home-alert-banner",
                "home-department-heatmap",
                "home-risk-distribution",
                "home-milestones",
                "home-task-type-breakdown",
                "home-activity-stream",
            ],
            NavSectionIds = ["dashboard", "compliance-execution", "action-center"],
            DefaultQuickActions =
            [
                new("Upload evidence", "/app/evidence"),
                new("Complete tasks", "/app/tasks?quick=myTasks"),
                new("Respond to CAPA", "/app/audit/corrective-actions"),
            ],
        },
        new RoleExperiencePack
        {
            PackId = "consultant",
            DisplayName = "Consultant",
            Persona = nameof(CopilotPersona.ExecutiveAdvisor),
            DefaultHomeRoute = "/app/consultant",
            RoleAliases =
            [
                ApplicationRoles.COMPLIANCE_VIEWER,
                ApplicationRoles.REGULATORY_ADMIN,
                ApplicationRoles.AUTHORITY,
                "Consultant",
                "NABH Consultant",
            ],
            WidgetAllowList =
            [
                "home-executive-kpi-strip",
                "home-alert-banner",
                "home-compliance-gauge",
                "home-framework-overview",
                "home-milestones",
                "home-risk-distribution",
                "home-score-trend",
                "cc-audit-readiness",
            ],
            NavSectionIds = ["dashboard", "compliance-execution"],
            DefaultQuickActions =
            [
                new("Portfolio", "/app/consultant"),
                new("Readiness", "/app/compliance-maturity"),
                new("Audit calendar", "/app/audit/planning"),
            ],
        },
        new RoleExperiencePack
        {
            PackId = "auditor",
            DisplayName = "Auditor",
            Persona = nameof(CopilotPersona.AuditorAssistant),
            DefaultHomeRoute = "/app/auditor/dashboard",
            RoleAliases =
            [
                ApplicationRoles.EXTERNAL_AUDITOR,
                ApplicationRoles.REGULATOR,
                "Auditor",
                "External Auditor",
                "Internal Auditor",
            ],
            WidgetAllowList =
            [
                "home-alert-banner",
                "home-compliance-gauge",
                "home-risk-distribution",
                "cc-audit-readiness",
                "cc-activity-stream",
            ],
            NavSectionIds = ["dashboard", "action-center"],
            DefaultQuickActions =
            [
                new("Auditor dashboard", "/app/auditor/dashboard"),
                new("Sessions", "/app/auditor/sessions"),
                new("Evidence review", "/app/auditor/evidence-review"),
                new("Observations", "/app/auditor/observations"),
            ],
        },
    ];
}

/// <summary>Immutable role experience pack definition.</summary>
public sealed class RoleExperiencePack
{
    public required string PackId { get; init; }
    public required string DisplayName { get; init; }
    /// <summary><see cref="CopilotPersona"/> name.</summary>
    public required string Persona { get; init; }
    public required string DefaultHomeRoute { get; init; }
    public required IReadOnlyList<string> RoleAliases { get; init; }
    public required IReadOnlyList<string> WidgetAllowList { get; init; }
    public required IReadOnlyList<string> NavSectionIds { get; init; }
    public required IReadOnlyList<RoleExperienceQuickAction> DefaultQuickActions { get; init; }

    public bool MatchesRole(string roleOrAlias) =>
        RoleAliases.Any(a => string.Equals(a, roleOrAlias, StringComparison.OrdinalIgnoreCase));
}

public sealed record RoleExperienceQuickAction(string Label, string Href);
