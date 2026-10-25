using Gateway.Configuration;
using Gateway.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text;
using Xunit;

namespace Gateway.UnitTests;

public sealed class TrialAccessMiddlewareTests
{
    [Fact]
    public async Task Allows_get_requests()
    {
        var called = false;
        var mw = new TrialAccessMiddleware(_ =>
        {
            called = true;
            return Task.CompletedTask;
        }, NullLogger<TrialAccessMiddleware>.Instance);

        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = "/api/v1/documents";

        await mw.InvokeAsync(
            context,
            Mock.Of<IHttpClientFactory>(),
            Options.Create(new TrialLifecycleGatewayOptions { EnforceReadOnly = true }));

        Assert.True(called);
    }

    [Fact]
    public async Task Blocks_post_when_expired()
    {
        var called = false;
        var mw = new TrialAccessMiddleware(_ =>
        {
            called = true;
            return Task.CompletedTask;
        }, NullLogger<TrialAccessMiddleware>.Instance);

        var orgId = Guid.NewGuid();
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    $"{{\"data\":{{\"status\":\"Expired\",\"organizationId\":\"{orgId}\"}}}}",
                    Encoding.UTF8,
                    "application/json")
            });

        var client = new HttpClient(handler.Object) { BaseAddress = new Uri("http://localhost") };
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient("OrganizationClient")).Returns(client);

        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Post;
        context.Request.Path = "/api/v1/documents";
        context.User = new System.Security.Claims.ClaimsPrincipal(
            new System.Security.Claims.ClaimsIdentity(
                [new System.Security.Claims.Claim("organization_id", orgId.ToString())],
                "test"));
        context.Response.Body = new MemoryStream();

        await mw.InvokeAsync(
            context,
            factory.Object,
            Options.Create(new TrialLifecycleGatewayOptions { EnforceReadOnly = true }));

        Assert.False(called);
        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
    }
}
