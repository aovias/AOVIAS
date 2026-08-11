using Shared.Infrastructure.Api;
using Xunit;

namespace Shared.Infrastructure.Tests;

public class ApiResponseCodesRegistryTests
{
    [Fact]
    public void ONBOARDING_WIZARD_COMPLETED_is_registered_with_metadata()
    {
        Assert.True(ApiResponseCodes.Registry.TryGetValue(ApiResponseCode.ONBOARDING_WIZARD_COMPLETED, out var meta));
        Assert.Equal(ApiResponseCode.ONBOARDING_WIZARD_COMPLETED, meta.Code);
        Assert.True(meta.HttpStatus >= 200 && meta.HttpStatus < 300);
    }

    [Fact]
    public void ONBOARDING_WIZARD_IDEMPOTENCY_REPLAY_is_registered_with_metadata()
    {
        Assert.True(ApiResponseCodes.Registry.TryGetValue(ApiResponseCode.ONBOARDING_WIZARD_IDEMPOTENCY_REPLAY, out var meta));
        Assert.Equal(ApiResponseCode.ONBOARDING_WIZARD_IDEMPOTENCY_REPLAY, meta.Code);
        Assert.Equal(200, meta.HttpStatus);
    }

    [Fact]
    public void ONBOARDING_WIZARD_PARTIAL_FAILURE_is_registered_with_metadata()
    {
        Assert.True(ApiResponseCodes.Registry.TryGetValue(ApiResponseCode.ONBOARDING_WIZARD_PARTIAL_FAILURE, out var meta));
        Assert.Equal(ApiResponseCode.ONBOARDING_WIZARD_PARTIAL_FAILURE, meta.Code);
        Assert.Equal(422, meta.HttpStatus);
    }
}
