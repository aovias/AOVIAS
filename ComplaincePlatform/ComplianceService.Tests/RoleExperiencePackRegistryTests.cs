using ComplianceService.Application.RoleExperience;
using ComplianceService.Domain.Copilot;
using Shared.Infrastructure.Auth;
using Xunit;

namespace ComplianceService.Tests;

public class RoleExperiencePackRegistryTests
{
    [Theory]
    [InlineData(ApplicationRoles.ORG_ADMIN, "hospital-admin", nameof(CopilotPersona.ExecutiveAdvisor))]
    [InlineData(ApplicationRoles.GROUP_ADMIN, "hospital-admin", nameof(CopilotPersona.ExecutiveAdvisor))]
    [InlineData(ApplicationRoles.SUPER_ADMIN, "hospital-admin", nameof(CopilotPersona.ExecutiveAdvisor))]
    [InlineData(ApplicationRoles.MANAGER, "quality-manager", nameof(CopilotPersona.QualityManagerAssistant))]
    [InlineData("Department Head", "department-head", nameof(CopilotPersona.DepartmentHeadAssistant))]
    [InlineData(ApplicationRoles.DEPARTMENT_HEAD, "department-head", nameof(CopilotPersona.DepartmentHeadAssistant))]
    [InlineData(ApplicationRoles.COMPLIANCE_VIEWER, "consultant", nameof(CopilotPersona.ExecutiveAdvisor))]
    [InlineData(ApplicationRoles.EXTERNAL_AUDITOR, "auditor", nameof(CopilotPersona.AuditorAssistant))]
    [InlineData(ApplicationRoles.REGULATOR, "auditor", nameof(CopilotPersona.AuditorAssistant))]
    public void Resolve_maps_application_roles_and_aliases(string role, string expectedPackId, string expectedPersona)
    {
        var pack = RoleExperiencePackRegistry.Resolve([role]);

        Assert.Equal(expectedPackId, pack.PackId);
        Assert.Equal(expectedPersona, pack.Persona);
        Assert.False(string.IsNullOrWhiteSpace(pack.DefaultHomeRoute));
        Assert.NotEmpty(pack.WidgetAllowList);
        Assert.NotEmpty(pack.NavSectionIds);
    }

    [Fact]
    public void Department_head_and_consultant_default_home_routes()
    {
        Assert.Equal("/app/department-head", RoleExperiencePackRegistry.GetById("department-head").DefaultHomeRoute);
        Assert.Equal("/app/consultant", RoleExperiencePackRegistry.GetById("consultant").DefaultHomeRoute);
    }

    [Fact]
    public void Resolve_prefers_admin_over_manager_when_both_present()
    {
        var pack = RoleExperiencePackRegistry.Resolve(
            [ApplicationRoles.MANAGER, ApplicationRoles.ORG_ADMIN]);

        Assert.Equal("hospital-admin", pack.PackId);
    }

    [Fact]
    public void Resolve_prefers_department_head_alias_over_manager()
    {
        var pack = RoleExperiencePackRegistry.Resolve(
            [ApplicationRoles.MANAGER, "Department Head"]);

        Assert.Equal("department-head", pack.PackId);
        Assert.Equal(nameof(CopilotPersona.DepartmentHeadAssistant), pack.Persona);
    }

    [Fact]
    public void Resolve_defaults_to_quality_manager_when_empty()
    {
        var pack = RoleExperiencePackRegistry.Resolve([]);
        Assert.Equal("quality-manager", pack.PackId);
    }

    [Fact]
    public void TryResolvePersona_returns_true_for_known_role()
    {
        var ok = RoleExperiencePackRegistry.TryResolvePersona(
            [ApplicationRoles.EXTERNAL_AUDITOR], out var persona);

        Assert.True(ok);
        Assert.Equal(CopilotPersona.AuditorAssistant, persona);
    }

    [Fact]
    public void ToPermissionTag_uses_role_pack_prefix()
    {
        Assert.Equal("role-pack:hospital-admin", RoleExperiencePackRegistry.ToPermissionTag("hospital-admin"));
    }

    [Fact]
    public void GetAll_includes_five_primary_packs()
    {
        var ids = RoleExperiencePackRegistry.GetAll().Select(p => p.PackId).ToHashSet();
        Assert.Contains("hospital-admin", ids);
        Assert.Contains("quality-manager", ids);
        Assert.Contains("department-head", ids);
        Assert.Contains("consultant", ids);
        Assert.Contains("auditor", ids);
    }

    [Fact]
    public void HospitalAdmin_pack_has_owner_quick_actions_and_dept_widgets()
    {
        var pack = RoleExperiencePackRegistry.GetById("hospital-admin");

        Assert.Equal("/app/dashboard?persona=hospital-admin", pack.DefaultHomeRoute);
        Assert.Contains(pack.WidgetAllowList, w => w == "home-department-heatmap");
        Assert.Contains(pack.WidgetAllowList, w => w == "home-risk-distribution");
        Assert.Contains(pack.DefaultQuickActions, a => a.Href.Contains("/app/reports", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(pack.DefaultQuickActions, a => a.Href.Contains("/app/analytics/departments", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(pack.DefaultQuickActions, a => a.Href.Contains("/app/audit", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void QualityManager_pack_defaults_to_role_home_with_ops_quick_actions()
    {
        var pack = RoleExperiencePackRegistry.GetById("quality-manager");

        Assert.Equal("/app/role-homes/qm", pack.DefaultHomeRoute);
        Assert.Contains(pack.DefaultQuickActions, a => a.Href.Contains("/app/evidence/review-queue", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(pack.DefaultQuickActions, a => a.Href.Contains("/app/audit/corrective-actions", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(pack.DefaultQuickActions, a => a.Href.Contains("/app/audit/planning", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("DepartmentHead")]
    [InlineData("DeptHead")]
    [InlineData("department head")]
    public void Resolve_department_head_aliases_case_insensitive(string alias)
    {
        var pack = RoleExperiencePackRegistry.Resolve([alias]);
        Assert.Equal("department-head", pack.PackId);
        Assert.Equal(nameof(CopilotPersona.DepartmentHeadAssistant), pack.Persona);
        Assert.Equal("/app/department-head", pack.DefaultHomeRoute);
    }
}

/// <summary>Pure catalog-shape checks for role engagement DTO defaults (Phase 3 Sprint 6).</summary>
public class RoleEngagementDtoTests
{
    [Fact]
    public void RoleEngagementDto_defaults_are_zeroed()
    {
        var dto = new RoleEngagementDto
        {
            PackId = "quality-manager",
            Persona = nameof(CopilotPersona.QualityManagerAssistant),
        };

        Assert.Equal(0, dto.Counters.TasksCompleted);
        Assert.Equal(0, dto.Counters.ReviewsCompleted);
        Assert.Equal(0, dto.Counters.CapaResolutions);
        Assert.Equal(0, dto.Counters.AuditsCompleted);
        Assert.Equal("analytics-operational-best-effort", dto.Source);
    }
}
