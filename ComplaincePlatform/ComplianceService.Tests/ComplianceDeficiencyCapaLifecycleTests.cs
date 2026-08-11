using ComplianceService.Application.Services;
using ComplianceService.Domain;
using ComplianceService.Domain.Enums;
using Shared.Infrastructure.Domain.Enums;
using Xunit;

namespace ComplianceService.Tests;

public sealed class ComplianceDeficiencyCapaLifecycleTests
{
    [Fact]
    public void AcceptOwnership_sets_acceptance_and_moves_Assigned_to_InProgress()
    {
        var d = CreateAssigned();

        d.AcceptOwnership("owner-1");

        Assert.Equal(DeficiencyStatus.InProgress, d.Status);
        Assert.NotNull(d.OwnerAcceptedAt);
        Assert.Equal("owner-1", d.OwnerAcceptedBy);
        Assert.False(d.OwnerAcceptanceRejected);
    }

    [Fact]
    public void RejectOwnership_clears_acceptance_and_keeps_assignment()
    {
        var d = CreateAssigned();
        d.AcceptOwnership("owner-1");
        d.RejectOwnership("owner-1", "Workload");

        Assert.Null(d.OwnerAcceptedAt);
        Assert.Null(d.OwnerAcceptedBy);
        Assert.True(d.OwnerAcceptanceRejected);
        Assert.Equal("owner-1", d.OwnerRejectedBy);
        Assert.Equal("Workload", d.OwnershipRejectionReason);
        Assert.Equal("assignee-1", d.AssignedTo);
    }

    [Fact]
    public void AssignCapaNumber_sets_once()
    {
        var d = CreateAssigned();
        d.AssignCapaNumber("CAPA-2026-00001");
        Assert.Equal("CAPA-2026-00001", d.CapaNumber);
        Assert.Throws<InvalidOperationException>(() => d.AssignCapaNumber("CAPA-2026-00002"));
    }

    [Fact]
    public void EnsureCloseable_requires_Verified()
    {
        var d = CreateAssigned();
        var ex = Assert.Throws<InvalidOperationException>(() => d.EnsureCloseable());
        Assert.Contains("Verified", ex.Message);
    }

    [Fact]
    public void EnsureCloseable_requires_corrective_completed_when_capa_started()
    {
        var d = CreateAssigned();
        d.TransitionStatus(DeficiencyStatus.InProgress, "u");
        d.TransitionStatus(DeficiencyStatus.EvidenceSubmitted, "u");
        d.TransitionStatus(DeficiencyStatus.ReviewPending, "u");
        d.TransitionStatus(DeficiencyStatus.Verified, "u");
        d.UpdateCorrectiveAction("Fix it", "owner", DateTime.UtcNow, null, null, CapaActionStatus.InProgress, "u");

        var ex = Assert.Throws<InvalidOperationException>(() => d.EnsureCloseable());
        Assert.Contains("corrective action", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EnsureCloseable_requires_preventive_completed_when_started()
    {
        var d = CreateCloseableBase();
        d.UpdateCorrectiveAction("Done", "owner", DateTime.UtcNow.AddDays(-2), null, DateTime.UtcNow, CapaActionStatus.Completed, "u");
        d.UpdatePreventiveAction("Prevent", "owner", DateTime.UtcNow.AddDays(7), null, null, CapaActionStatus.InProgress, "u");

        var ex = Assert.Throws<InvalidOperationException>(() => d.EnsureCloseable());
        Assert.Contains("preventive action", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EnsureCloseable_requires_terminal_effectiveness_status()
    {
        var d = CreateCloseableBase();
        d.UpdateCorrectiveAction("Done", "owner", DateTime.UtcNow.AddDays(-2), null, DateTime.UtcNow, CapaActionStatus.Completed, "u");
        d.SetEffectivenessReviewStatus(EffectivenessReviewStatus.Pending, "u");

        var ex = Assert.Throws<InvalidOperationException>(() => d.EnsureCloseable());
        Assert.Contains("effectiveness review", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Close_succeeds_when_gates_pass_without_capa_activity()
    {
        var d = CreateCloseableBase();
        d.Close("closer");
        Assert.Equal(DeficiencyStatus.Closed, d.Status);
        Assert.NotNull(d.ClosedDate);
    }

    [Fact]
    public void Close_succeeds_when_capa_gates_pass()
    {
        var d = CreateCloseableBase();
        d.AssignCapaNumber("CAPA-2026-00009");
        d.UpdateCorrectiveAction("Done", "owner", DateTime.UtcNow.AddDays(-3), null, DateTime.UtcNow, CapaActionStatus.Completed, "u");
        d.SetEffectivenessReviewStatus(EffectivenessReviewStatus.Effective, "u");

        d.Close("closer");
        Assert.Equal(DeficiencyStatus.Closed, d.Status);
    }

    [Fact]
    public void Closed_capa_rejects_corrective_action_patch()
    {
        var d = CreateCloseableBase();
        d.Close("closer");

        var ex = Assert.Throws<InvalidOperationException>(() =>
            d.UpdateCorrectiveAction("mutate", "owner", null, null, null, CapaActionStatus.InProgress, "u"));
        Assert.Contains("closed or cancelled", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Closed_capa_rejects_rca_patch()
    {
        var d = CreateCloseableBase();
        d.Close("closer");

        var ex = Assert.Throws<InvalidOperationException>(() =>
            d.UpdateRca(null, "p", "r", null, null, null, null, null, null, null, null, null, "u"));
        Assert.Contains("closed or cancelled", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MarkPromoted_allows_additional_promotions()
    {
        var finding = new Finding
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Title = "t",
            Status = FindingStatus.Accepted,
        };
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();

        finding.MarkPromoted(first);
        finding.LinkPromotedDeficiency(second);

        Assert.Equal(second, finding.PromotedDeficiencyId);
    }

    [Theory]
    [InlineData(FindingClassification.AuditFinding, true)]
    [InlineData(FindingClassification.GoodPractice, false)]
    public void Multi_promote_eligibility_classification_unchanged(FindingClassification classification, bool expected)
    {
        Assert.Equal(
            expected,
            FindingPromotionEligibilityService.IsClassificationEligible(classification));
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
            "DEF-TEST-1",
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

    private static ComplianceDeficiency CreateCloseableBase()
    {
        var d = CreateAssigned();
        d.TransitionStatus(DeficiencyStatus.InProgress, "u");
        d.TransitionStatus(DeficiencyStatus.EvidenceSubmitted, "u");
        d.TransitionStatus(DeficiencyStatus.ReviewPending, "u");
        d.TransitionStatus(DeficiencyStatus.Verified, "u");
        return d;
    }
}
