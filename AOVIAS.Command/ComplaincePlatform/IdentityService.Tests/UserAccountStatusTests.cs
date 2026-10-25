using IdentityService.Domain.Enums;
using Xunit;

namespace IdentityService.Tests;

public class UserAccountStatusTests
{
    [Fact]
    public void Suspended_IsDistinctFromLockoutConcept()
    {
        Assert.NotEqual((int)UserAccountStatus.Suspended, (int)UserAccountStatus.Active);
        Assert.Equal(2, (int)UserAccountStatus.Suspended);
    }

    [Fact]
    public void PendingVerification_IsZero()
    {
        Assert.Equal(0, (int)UserAccountStatus.PendingVerification);
    }
}
