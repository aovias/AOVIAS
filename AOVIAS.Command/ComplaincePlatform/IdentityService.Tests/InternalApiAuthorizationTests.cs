using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Infrastructure.Auth;
using Xunit;

namespace IdentityService.Tests;

public class InternalApiAuthorizationTests
{
    [Fact]
    public void ValidateInternalApiKey_RejectsMissingKey_WhenConfigured()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["InternalApi:ApiKey"] = "secret-key" })
            .Build();
        services.AddSingleton<IConfiguration>(config);

        var context = new DefaultHttpContext();
        context.Request.Headers["X-Internal-Api-Key"] = "wrong";
        context.Request.Host = new HostString("api.internal");

        var valid = InternalApiAuthorizationExtensions.ValidateInternalApiKey(context.Request, services.BuildServiceProvider());
        Assert.False(valid);
    }

    [Fact]
    public void ValidateInternalApiKey_AcceptsMatchingKey()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["InternalApi:ApiKey"] = "secret-key" })
            .Build();
        services.AddSingleton<IConfiguration>(config);

        var context = new DefaultHttpContext();
        context.Request.Headers["X-Internal-Api-Key"] = "secret-key";
        context.Request.Host = new HostString("api.internal");

        var valid = InternalApiAuthorizationExtensions.ValidateInternalApiKey(context.Request, services.BuildServiceProvider());
        Assert.True(valid);
    }

    [Fact]
    public void ValidateInternalApiKey_AllowsLocalhost_WhenKeyNotConfigured()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().Build();
        services.AddSingleton<IConfiguration>(config);

        var context = new DefaultHttpContext();
        context.Request.Host = new HostString("localhost", 7135);

        var valid = InternalApiAuthorizationExtensions.ValidateInternalApiKey(context.Request, services.BuildServiceProvider());
        Assert.True(valid);
    }

    [Fact]
    public void ValidateInternalApiKey_RejectsRemoteHost_WhenKeyNotConfigured()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().Build();
        services.AddSingleton<IConfiguration>(config);

        var context = new DefaultHttpContext();
        context.Request.Host = new HostString("evil.example.com");

        var valid = InternalApiAuthorizationExtensions.ValidateInternalApiKey(context.Request, services.BuildServiceProvider());
        Assert.False(valid);
    }
}
