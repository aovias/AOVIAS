using ComplianceService.Application.Contracts;
using Xunit;

namespace ComplianceService.Tests;

public sealed class EvidenceRegistrationContextTests
{
    [Fact]
    public void ValidateAnchors_throws_when_no_anchor()
    {
        var ctx = new EvidenceRegistrationContext { TenantId = Guid.NewGuid(), UserId = Guid.NewGuid() };
        Assert.Throws<ArgumentException>(() => ctx.ValidateAnchors());
    }

    [Fact]
    public void ValidateAnchors_passes_with_task_id()
    {
        var ctx = EvidenceRegistrationContext.ForTask(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        ctx.ValidateAnchors();
    }
}
