using ComplianceService.Domain;
using ComplianceService.Domain.Policies;
using Shared.Infrastructure.Domain.Enums;
using Xunit;

namespace ComplianceService.Tests.Journey;

public class OperationalModeClassificationPolicyTests
{
    private readonly OperationalModeClassificationPolicy _policy = new();

    public static TheoryData<OperationalMode, int> WorkspaceCounts => new()
    {
        { OperationalMode.SelfAssessment, 4 },
        { OperationalMode.InternalAudit, 3 },
        { OperationalMode.FinalAudit, 2 },
        { OperationalMode.Surveillance, 3 },
        { OperationalMode.ReAccreditation, 4 }
    };

    [Theory]
    [MemberData(nameof(WorkspaceCounts))]
    public void Workspace_returns_expected_count(OperationalMode mode, int expectedCount)
    {
        var list = _policy.GetClassificationsForWorkspace(mode);
        Assert.Equal(expectedCount, list.Count);
    }

    [Fact]
    public void Decision_rules_Self_Internal_Final_share_Core_and_Commitment()
    {
        var self = _policy.GetClassificationsForDecisionRules(OperationalMode.SelfAssessment);
        var internalAudit = _policy.GetClassificationsForDecisionRules(OperationalMode.InternalAudit);
        var final = _policy.GetClassificationsForDecisionRules(OperationalMode.FinalAudit);
        Assert.Equal(self, final);
        Assert.Equal(internalAudit, final);
        Assert.Equal(2, final.Count);
        Assert.Contains(ObjectiveElementClassification.Core, final);
        Assert.Contains(ObjectiveElementClassification.Commitment, final);
    }

    [Fact]
    public void Decision_rules_Surveillance_and_ReAccreditation_distinct()
    {
        var surv = _policy.GetClassificationsForDecisionRules(OperationalMode.Surveillance);
        var reacc = _policy.GetClassificationsForDecisionRules(OperationalMode.ReAccreditation);
        Assert.Equal(3, surv.Count);
        Assert.Equal(4, reacc.Count);
    }

    [Fact]
    public void Workspace_reuses_same_reference_for_identical_sets()
    {
        var self = _policy.GetClassificationsForWorkspace(OperationalMode.SelfAssessment);
        var reacc = _policy.GetClassificationsForWorkspace(OperationalMode.ReAccreditation);
        Assert.Same(self, reacc);
    }

    [Fact]
    public void Unknown_OperationalMode_throws()
    {
        var bad = (OperationalMode)999;
        Assert.Throws<ArgumentOutOfRangeException>(() => _policy.GetClassificationsForWorkspace(bad));
        Assert.Throws<ArgumentOutOfRangeException>(() => _policy.GetClassificationsForDecisionRules(bad));
    }

    [Fact]
    public void IsClassificationEditable_uses_workspace_scope()
    {
        Assert.True(_policy.IsClassificationEditable(OperationalMode.FinalAudit, ObjectiveElementClassification.Core));
        Assert.False(_policy.IsClassificationEditable(OperationalMode.FinalAudit, ObjectiveElementClassification.Excellence));
    }
}
