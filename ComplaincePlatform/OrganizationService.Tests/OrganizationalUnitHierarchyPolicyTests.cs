using OrganizationService.Domain;
using Xunit;

namespace OrganizationService.Tests;

public sealed class OrganizationalUnitHierarchyPolicyTests
{
    [Theory]
    [InlineData(null, "Campus")]
    [InlineData(null, "Facility")]
    [InlineData(null, "Building")]
    [InlineData(null, "Team")]
    [InlineData("Campus", "Facility")]
    [InlineData("Campus", "Building")]
    [InlineData("Facility", "Building")]
    [InlineData("Building", "Floor")]
    [InlineData("Floor", "Wing")]
    [InlineData("Wing", "Unit")]
    [InlineData("Unit", "Location")]
    [InlineData("Location", "WorkArea")]
    [InlineData("WorkArea", "Team")]
    [InlineData("Team", "Team")]
    public void EnsureAllowedParentChild_AcceptsValidEdges(string? parent, string child)
    {
        OrganizationalUnitHierarchyPolicy.EnsureAllowedParentChild(parent, child);
    }

    [Theory]
    [InlineData(null, "Floor")]
    [InlineData("Campus", "Floor")]
    [InlineData("Floor", "Campus")]
    [InlineData("Team", "Building")]
    [InlineData("WorkArea", "Wing")]
    public void EnsureAllowedParentChild_RejectsInvalidEdges(string? parent, string child)
    {
        Assert.Throws<InvalidOperationException>(() =>
            OrganizationalUnitHierarchyPolicy.EnsureAllowedParentChild(parent, child));
    }

    [Fact]
    public void EnsureAllowedParentChild_RejectsEmptyChildType()
    {
        Assert.Throws<InvalidOperationException>(() =>
            OrganizationalUnitHierarchyPolicy.EnsureAllowedParentChild("Campus", " "));
    }

    [Theory]
    [InlineData("Campus", true)]
    [InlineData("WorkArea", true)]
    [InlineData("Hospital", false)]
    [InlineData("Department", false)]
    public void IsKnownType_MatchesCatalog(string code, bool expected)
    {
        Assert.Equal(expected, OrganizationalUnitHierarchyPolicy.IsKnownType(code));
    }
}
