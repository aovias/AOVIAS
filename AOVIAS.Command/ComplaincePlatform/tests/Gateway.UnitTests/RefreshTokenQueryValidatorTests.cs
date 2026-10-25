using Gateway.Application.Queries;
using Gateway.Application.Validators;
using System.Text.Json;
using Xunit;

namespace Gateway.UnitTests;

public class RefreshTokenQueryValidatorTests
{
    private readonly RefreshTokenQueryValidator _validator = new();

    [Fact]
    public void Valid_refreshToken_passes()
    {
        var json = JsonSerializer.SerializeToElement(new { refreshToken = "opaque-token-value" });
        var query = new RefreshTokenQuery(json);
        var result = _validator.Validate(query);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Missing_refreshToken_fails()
    {
        var json = JsonSerializer.SerializeToElement(new { });
        var query = new RefreshTokenQuery(json);
        var result = _validator.Validate(query);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Empty_refreshToken_string_fails()
    {
        var json = JsonSerializer.SerializeToElement(new { refreshToken = "" });
        var query = new RefreshTokenQuery(json);
        var result = _validator.Validate(query);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Null_body_fails()
    {
        var query = new RefreshTokenQuery(null!);
        var result = _validator.Validate(query);
        Assert.False(result.IsValid);
    }
}
