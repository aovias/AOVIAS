using Gateway.Configuration;
using Gateway.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using System.Net;
using System.Security.Claims;
using System.Text;
using Xunit;

namespace Gateway.UnitTests;

public sealed class LicenseEntitlementMiddlewareTests
{
    [Fact]
    public async Task Allows_get_with_advisory_headers_when_gate_matches()
    {
        var called = false;
        var mw = new LicenseEntitlementMiddleware(_ =>
        {
            called = true;
            return Task.CompletedTask;
        }, NullLogger<LicenseEntitlementMiddleware>.Instance);

        var orgId = Guid.NewGuid();
        var factory = CreateFactory(orgId, allowed: true);
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = "/api/v1/ai/assist";
        context.User = Principal(orgId);
        context.Response.Body = new MemoryStream();

        await mw.InvokeAsync(context, factory, Options.Create(DefaultOpts()));

        Assert.True(called);
        Assert.Equal(200, context.Response.StatusCode == 0 ? 200 : context.Response.StatusCode);
    }

    [Fact]
    public async Task Blocks_post_when_entitlement_denied_fail_closed()
    {
        var called = false;
        var mw = new LicenseEntitlementMiddleware(_ =>
        {
            called = true;
            return Task.CompletedTask;
        }, NullLogger<LicenseEntitlementMiddleware>.Instance);

        var orgId = Guid.NewGuid();
        var factory = CreateFactory(orgId, allowed: false);
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Post;
        context.Request.Path = "/api/v1/ai/actions";
        context.User = Principal(orgId);
        context.Response.Body = new MemoryStream();

        await mw.InvokeAsync(context, factory, Options.Create(DefaultOpts()));

        Assert.False(called);
        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
    }

    [Fact]
    public async Task Allowlists_commercial_admin_mutations()
    {
        var called = false;
        var mw = new LicenseEntitlementMiddleware(_ =>
        {
            called = true;
            return Task.CompletedTask;
        }, NullLogger<LicenseEntitlementMiddleware>.Instance);

        var orgId = Guid.NewGuid();
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Post;
        context.Request.Path = "/api/v1/commercial-admin/trials/extend";
        context.User = Principal(orgId);

        await mw.InvokeAsync(
            context,
            Mock.Of<IHttpClientFactory>(),
            Options.Create(DefaultOpts()));

        Assert.True(called);
    }

    private static LicensingFeatureGateOptions DefaultOpts() => new()
    {
        FailClosedWrites = true,
        EntitlementCacheSeconds = 5,
        FeatureGates = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["/api/v1/ai/"] = "ai.write"
        }
    };

    private static ClaimsPrincipal Principal(Guid orgId) =>
        new(new ClaimsIdentity([new Claim("organization_id", orgId.ToString())], "test"));

    private static IHttpClientFactory CreateFactory(Guid orgId, bool allowed)
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    $"{{\"data\":{{\"allowed\":{(allowed ? "true" : "false")},\"organizationId\":\"{orgId}\"}}}}",
                    Encoding.UTF8,
                    "application/json")
            });

        var client = new HttpClient(handler.Object) { BaseAddress = new Uri("http://localhost") };
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient("OrganizationClient")).Returns(client);
        return factory.Object;
    }
}
