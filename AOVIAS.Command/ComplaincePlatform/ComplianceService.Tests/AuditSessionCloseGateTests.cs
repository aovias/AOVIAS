using ComplianceService.Application.Utilities;
using ComplianceService.Domain;
using Shared.Infrastructure.Domain.Enums;
using Xunit;

namespace ComplianceService.Tests;

public class AuditSessionCloseGateTests
{
    [Fact]
    public void IsFindingResolvedForClose_ClosedStatus_Allows()
    {
        var finding = new Finding { Status = FindingStatus.Closed };
        Assert.True(AuditSessionCloseGate.IsFindingResolvedForClose(finding));
    }

    [Fact]
    public void IsFindingResolvedForClose_IsResolvedFlag_Allows()
    {
        var finding = new Finding { Status = FindingStatus.Open, IsResolved = true };
        Assert.True(AuditSessionCloseGate.IsFindingResolvedForClose(finding));
    }

    [Fact]
    public void IsFindingResolvedForClose_PromotedDeficiency_Allows()
    {
        var finding = new Finding
        {
            Status = FindingStatus.Accepted,
            PromotedDeficiencyId = Guid.NewGuid(),
        };
        Assert.True(AuditSessionCloseGate.IsFindingResolvedForClose(finding));
    }

    [Fact]
    public void IsFindingResolvedForClose_AcceptedWithoutCapa_Allows()
    {
        // Accepted findings close the session; AuditSessionClosed workflow auto-promotes CAPA.
        var finding = new Finding
        {
            Status = FindingStatus.Accepted,
            PromotedDeficiencyId = null,
            IsResolved = false,
        };
        Assert.True(AuditSessionCloseGate.IsFindingResolvedForClose(finding));
    }

    [Fact]
    public void IsFindingResolvedForClose_OpenFinding_Blocks()
    {
        var finding = new Finding { Status = FindingStatus.Open, IsResolved = false };
        Assert.False(AuditSessionCloseGate.IsFindingResolvedForClose(finding));
    }

    [Fact]
    public void EnsureCanClose_WithOpenFindings_Throws()
    {
        var findings = new[]
        {
            new Finding { FindingNumber = "FND-1", Status = FindingStatus.Open },
            new Finding { FindingNumber = "FND-2", Status = FindingStatus.Closed },
        };

        var ex = Assert.Throws<InvalidOperationException>(() => AuditSessionCloseGate.EnsureCanClose(findings));
        Assert.Contains("Cannot close audit session", ex.Message);
        Assert.Contains("FND-1", ex.Message);
    }

    [Fact]
    public void EnsureCanClose_AllResolvedClosedOrPromoted_Succeeds()
    {
        var findings = new[]
        {
            new Finding { Status = FindingStatus.Closed },
            new Finding { Status = FindingStatus.Open, IsResolved = true },
            new Finding { Status = FindingStatus.Accepted, PromotedDeficiencyId = Guid.NewGuid() },
        };

        AuditSessionCloseGate.EnsureCanClose(findings);
        Assert.Empty(AuditSessionCloseGate.GetBlockingFindings(findings));
    }
}
