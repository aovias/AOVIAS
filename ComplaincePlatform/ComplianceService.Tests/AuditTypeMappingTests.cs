using ComplianceService.Application.Utilities;
using ComplianceService.Domain.Enums;
using Xunit;

namespace ComplianceService.Tests;

public class AuditTypeMappingTests
{
    [Theory]
    [InlineData("Internal", AuditType.InternalAudit)]
    [InlineData("Department", AuditType.DepartmentAudit)]
    [InlineData("Follow-up", AuditType.FollowUpAudit)]
    [InlineData("Follow up", AuditType.FollowUpAudit)]
    [InlineData("Mock NABH", AuditType.MockAudit)]
    [InlineData("MockAudit", AuditType.MockAudit)]
    [InlineData("Focused", AuditType.FocusedAudit)]
    public void TryParseCommercial_CoreLabels_MapCorrectly(string label, AuditType expected)
    {
        Assert.True(AuditTypeMapping.TryParseCommercial(label, out var type));
        Assert.Equal(expected, type);
    }

    [Fact]
    public void ToCommercialLabel_CoreTypes_MatchSpec()
    {
        Assert.Equal("Internal", AuditTypeMapping.ToCommercialLabel(AuditType.InternalAudit));
        Assert.Equal("Department", AuditTypeMapping.ToCommercialLabel(AuditType.DepartmentAudit));
        Assert.Equal("Follow-up", AuditTypeMapping.ToCommercialLabel(AuditType.FollowUpAudit));
        Assert.Equal("Mock NABH", AuditTypeMapping.ToCommercialLabel(AuditType.MockAudit));
        Assert.Equal("Focused", AuditTypeMapping.ToCommercialLabel(AuditType.FocusedAudit));
    }

    [Fact]
    public void CommercialCoreTypes_IncludeRequiredSprint2Set()
    {
        var core = AuditTypeMapping.CommercialCoreTypes;
        Assert.Contains(AuditType.InternalAudit, core);
        Assert.Contains(AuditType.DepartmentAudit, core);
        Assert.Contains(AuditType.FollowUpAudit, core);
        Assert.Contains(AuditType.MockAudit, core);
        Assert.Contains(AuditType.FocusedAudit, core);
    }

    [Fact]
    public void AuditType_Enum_ContainsDepartmentAndFollowUp()
    {
        var values = Enum.GetValues<AuditType>();
        Assert.Contains(AuditType.DepartmentAudit, values);
        Assert.Contains(AuditType.FollowUpAudit, values);
        Assert.Contains(AuditType.FocusedAudit, values);
        Assert.Contains(AuditType.MockAudit, values);
        Assert.Contains(AuditType.InternalAudit, values);
    }
}
