using Shared.Infrastructure.Auth;
using Xunit;

namespace Gateway.UnitTests;

public sealed class CommercialPermissionsTests
{
    [Fact]
    public void SuperAdmin_has_permanent_delete()
    {
        Assert.True(CommercialPermissions.RoleHasPermission(ApplicationRoles.SUPER_ADMIN, CommercialPermissions.OrgsPermanentDelete));
    }

    [Fact]
    public void CommercialAdmin_cannot_permanent_delete()
    {
        Assert.False(CommercialPermissions.RoleHasPermission(ApplicationRoles.COMMERCIAL_ADMIN, CommercialPermissions.OrgsPermanentDelete));
        Assert.True(CommercialPermissions.RoleHasPermission(ApplicationRoles.COMMERCIAL_ADMIN, CommercialPermissions.OrgsWrite));
    }

    [Fact]
    public void Auditor_is_read_only()
    {
        Assert.True(CommercialPermissions.RoleHasPermission(ApplicationRoles.COMMERCIAL_AUDITOR, CommercialPermissions.AuditRead));
        Assert.False(CommercialPermissions.RoleHasPermission(ApplicationRoles.COMMERCIAL_AUDITOR, CommercialPermissions.OrgsWrite));
        Assert.False(CommercialPermissions.RoleHasPermission(ApplicationRoles.COMMERCIAL_AUDITOR, CommercialPermissions.TrialsWrite));
    }

    [Fact]
    public void OrgAdmin_is_not_commercial_admin_role()
    {
        Assert.False(ApplicationRoles.CommercialAdminRoles.Contains(ApplicationRoles.ORG_ADMIN));
    }
}
