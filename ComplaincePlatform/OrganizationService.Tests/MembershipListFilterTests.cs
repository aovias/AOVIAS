using OrganizationService.Application.Services;
using OrganizationService.Domain.Enums;
using Shared.Infrastructure.Auth;
using Shared.Infrastructure.Domain;
using Xunit;

namespace OrganizationService.Tests;

public class MembershipListFilterTests
{
    [Fact]
    public void MembershipDto_Map_IncludesProfileFields()
    {
        var member = new OrganizationMember
        {
            Id = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Role = ApplicationRoles.USER,
            MembershipStatus = MembershipStatuses.Active,
            FirstName = "Ada",
            LastName = "Lovelace",
            Email = "ada@test.com",
            PhoneNumber = "123",
            ProfilePicUrl = "https://example.com/p.png",
            CreatedAt = DateTime.UtcNow,
            EffectiveFrom = DateTime.UtcNow,
            IsPrimary = true
        };

        var dto = InvokeMap(member, "Engineering");

        Assert.Equal("Ada", dto.FirstName);
        Assert.Equal("Lovelace", dto.LastName);
        Assert.Equal("ada@test.com", dto.Email);
        Assert.Equal("Engineering", dto.DepartmentName);
        Assert.Equal(member.CreatedAt, dto.CreatedAt);
    }

    private static OrganizationService.Application.Contracts.MembershipDto InvokeMap(OrganizationMember member, string? departmentName)
    {
        var method = typeof(MembershipAppService).GetMethod("Map", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static, null, new[] { typeof(OrganizationMember), typeof(string) }, null);
        Assert.NotNull(method);
        return (OrganizationService.Application.Contracts.MembershipDto)method!.Invoke(null, new object?[] { member, departmentName })!;
    }
}
