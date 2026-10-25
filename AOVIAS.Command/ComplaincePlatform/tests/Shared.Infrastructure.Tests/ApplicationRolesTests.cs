using Shared.Infrastructure.Auth;
using Xunit;

namespace Shared.Infrastructure.Tests;

public class ApplicationRolesTests
{
    [Fact]
    public void AllSeededRoles_has_no_duplicates()
    {
        var groups = ApplicationRoles.AllSeededRoles.GroupBy(r => r).Where(g => g.Count() > 1).ToList();
        Assert.Empty(groups);
    }

    [Theory]
    [InlineData(ApplicationRoles.USER)]
    [InlineData(ApplicationRoles.ORG_ADMIN)]
    public void IsAssignableRole_accepts_canonical(string role) =>
        Assert.True(ApplicationRoles.IsAssignableRole(role));

    [Fact]
    public void IsAssignableRole_rejects_unknown() =>
        Assert.False(ApplicationRoles.IsAssignableRole("NotARealRole"));
}
