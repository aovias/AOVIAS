using Shared.Infrastructure.Auth;
using Xunit;

namespace Shared.Infrastructure.Tests;

public class RoleLegacyMapTests
{
    [Theory]
    [InlineData("GroupAdmin", "ORG_ADMIN")]
    [InlineData("OrgAdmin", "ORG_ADMIN")]
    [InlineData("User", "USER")]
    [InlineData("USER", "USER")]
    [InlineData("", "USER")]
    public void ToCanonical_maps_known_and_empty(string? input, string expected) =>
        Assert.Equal(expected, RoleLegacyMap.ToCanonical(input));

    [Fact]
    public void ToCanonical_null_returns_USER() =>
        Assert.Equal(ApplicationRoles.USER, RoleLegacyMap.ToCanonical(null));

    [Fact]
    public void NormalizeRoleNames_deduplicates_after_canonicalization()
    {
        var list = RoleLegacyMap.NormalizeRoleNames(new[] { "GroupAdmin", "ORG_ADMIN", "User" });
        Assert.Equal(2, list.Count);
        Assert.Contains(ApplicationRoles.ORG_ADMIN, list);
        Assert.Contains(ApplicationRoles.USER, list);
    }

    [Fact]
    public void NormalizeRoleNames_empty_collection_defaults_to_USER()
    {
        var list = RoleLegacyMap.NormalizeRoleNames(Array.Empty<string>());
        Assert.Single(list);
        Assert.Equal(ApplicationRoles.USER, list[0]);
    }
}
