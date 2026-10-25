using ComplianceService.Domain.Auditor;
using ComplianceService.Domain.Enums;
using Xunit;

namespace ComplianceService.Tests;

public sealed class AuditSessionFindingGateTests
{
    [Theory]
    [InlineData(AuditSessionStatus.Closed)]
    [InlineData(AuditSessionStatus.Submitted)]
    public void EnsureAcceptsFindings_rejects_terminal_sessions(AuditSessionStatus status)
    {
        var session = new AuditSession
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            SessionNumber = "AUD-TEST-1",
            Status = status,
        };

        var ex = Assert.Throws<InvalidOperationException>(() => session.EnsureAcceptsFindings());
        Assert.Contains(status.ToString(), ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(AuditSessionStatus.Planned)]
    [InlineData(AuditSessionStatus.InProgress)]
    [InlineData(AuditSessionStatus.Review)]
    [InlineData(AuditSessionStatus.DraftReport)]
    public void EnsureAcceptsFindings_allows_open_sessions(AuditSessionStatus status)
    {
        var session = new AuditSession
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            SessionNumber = "AUD-TEST-2",
            Status = status,
        };

        session.EnsureAcceptsFindings();
    }
}
