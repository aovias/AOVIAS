using ComplianceService.Application.Services;
using ComplianceService.Domain;
using ComplianceService.Domain.Enums;
using Shared.Infrastructure.Auth;
using Shared.Infrastructure.Domain.Enums;
using Xunit;

namespace ComplianceService.Tests;

public sealed class CapaVerifyRoleGateTests
{
    [Fact]
    public void EnsureCanVerify_blocks_non_reviewer_when_roles_present()
    {
        var d = CreateAssigned();
        var ex = Assert.Throws<InvalidOperationException>(() =>
            PatchDeficiencyStatusCommandHandler.EnsureCanVerify(
                d,
                "viewer-1",
                new[] { ApplicationRoles.USER },
                Guid.NewGuid(),
                null));
        Assert.Contains("reviewer role", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EnsureCanVerify_blocks_assignee_self_verify()
    {
        var d = CreateAssigned();
        var verifierId = Guid.NewGuid();
        d.Assign(verifierId.ToString(), DateTime.UtcNow.AddDays(3), "tester");

        var ex = Assert.Throws<InvalidOperationException>(() =>
            PatchDeficiencyStatusCommandHandler.EnsureCanVerify(
                d,
                verifierId.ToString(),
                new[] { ApplicationRoles.MANAGER },
                verifierId,
                null));
        Assert.Contains("self-verify", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EnsureCanVerify_allows_manager_who_is_not_assignee()
    {
        var d = CreateAssigned();
        PatchDeficiencyStatusCommandHandler.EnsureCanVerify(
            d,
            "reviewer-1",
            new[] { ApplicationRoles.MANAGER },
            Guid.NewGuid(),
            null);
    }

    private static ComplianceDeficiency CreateAssigned()
    {
        var d = ComplianceDeficiency.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "DEF-VERIFY-1",
            "Title",
            "Desc",
            DeficiencyType.Assessment,
            DeficiencySeverity.Major,
            ObjectiveElementClassification.Core,
            null,
            "Manual",
            "tester");
        d.Assign("assignee-1", DateTime.UtcNow.AddDays(7), "tester");
        return d;
    }
}
