using IdentityService.Configuration;
using Microsoft.Extensions.Configuration;
using Shared.Infrastructure.Email;
using Xunit;

namespace IdentityService.Tests;

public class EmailVerificationModeResolverTests
{
    [Theory]
    [InlineData("Async", EmailVerificationMode.Async)]
    [InlineData("None", EmailVerificationMode.None)]
    [InlineData("RequiredBeforeLogin", EmailVerificationMode.RequiredBeforeLogin)]
    [InlineData("RequiredBeforeOrganizationProvisioning", EmailVerificationMode.RequiredBeforeOrganizationProvisioning)]
    public void Resolve_parses_mode_string(string value, EmailVerificationMode expected)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Identity:EmailVerificationMode"] = value
            })
            .Build();

        Assert.Equal(expected, EmailVerificationModeResolver.Resolve(config));
    }

    [Fact]
    public void Resolve_maps_legacy_true_to_RequiredBeforeLogin()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Identity:RequireEmailVerification"] = "true"
            })
            .Build();

        Assert.Equal(EmailVerificationMode.RequiredBeforeLogin, EmailVerificationModeResolver.Resolve(config));
    }

    [Fact]
    public void Resolve_defaults_to_Async()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
        Assert.Equal(EmailVerificationMode.Async, EmailVerificationModeResolver.Resolve(config));
    }

    [Fact]
    public void Policy_helpers_match_phase0_rules()
    {
        Assert.True(EmailVerificationModeResolver.ShouldSendVerificationEmail(EmailVerificationMode.Async));
        Assert.False(EmailVerificationModeResolver.ShouldConfirmEmailOnCreate(EmailVerificationMode.Async));
        Assert.True(EmailVerificationModeResolver.IsActiveOnCreate(EmailVerificationMode.Async));
        Assert.False(EmailVerificationModeResolver.BlocksPasswordLoginWhenUnconfirmed(EmailVerificationMode.Async));

        Assert.True(EmailVerificationModeResolver.BlocksPasswordLoginWhenUnconfirmed(EmailVerificationMode.RequiredBeforeLogin));
        Assert.True(EmailVerificationModeResolver.BlocksAnonymousOrgSignup(EmailVerificationMode.RequiredBeforeOrganizationProvisioning));
        Assert.True(EmailVerificationModeResolver.ShouldConfirmEmailOnCreate(EmailVerificationMode.None));
    }
}
