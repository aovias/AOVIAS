using Gateway.Application.Handlers;
using Gateway.Application.Queries;
using Gateway.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Gateway.UnitTests;

/// <summary>
/// Verifies that <see cref="GetComplianceMaturityDashboardQueryHandler"/> proxies
/// the request to the correct ComplianceService endpoint.
/// </summary>
public sealed class ComplianceMaturityDashboardQueryHandlerTests
{
    private readonly Mock<IProxyService> _proxy = new();

    [Fact]
    public async Task Handle_ProxiesToCorrectComplianceServiceEndpoint()
    {
        // Arrange
        _proxy
            .Setup(p => p.ProxyToComplianceServiceAsync(
                It.IsAny<string>(),
                HttpMethod.Get,
                It.IsAny<object?>(),
                It.IsAny<Dictionary<string, string>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ContentResult { StatusCode = 200, Content = "{}" });

        var handler = new GetComplianceMaturityDashboardQueryHandler(_proxy.Object);
        var query   = new GetComplianceMaturityDashboardQuery("1");

        // Act
        await handler.Handle(query, CancellationToken.None);

        // Assert: exact endpoint path
        _proxy.Verify(
            p => p.ProxyToComplianceServiceAsync(
                "/api/v1/compliance/maturity-dashboard",
                HttpMethod.Get,
                null,
                null,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithDifferentApiVersion_UsesVersionInEndpoint()
    {
        _proxy
            .Setup(p => p.ProxyToComplianceServiceAsync(
                It.IsAny<string>(),
                It.IsAny<HttpMethod>(),
                It.IsAny<object?>(),
                It.IsAny<Dictionary<string, string>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ContentResult { StatusCode = 200 });

        var handler = new GetComplianceMaturityDashboardQueryHandler(_proxy.Object);

        await handler.Handle(new GetComplianceMaturityDashboardQuery("1.1"), CancellationToken.None);

        _proxy.Verify(
            p => p.ProxyToComplianceServiceAsync(
                "/api/v1.1/compliance/maturity-dashboard",
                HttpMethod.Get,
                null,
                null,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_PassesThroughProxyResult()
    {
        var expected = new ContentResult { StatusCode = 200, Content = "{\"success\":true}" };
        _proxy
            .Setup(p => p.ProxyToComplianceServiceAsync(
                It.IsAny<string>(),
                It.IsAny<HttpMethod>(),
                It.IsAny<object?>(),
                It.IsAny<Dictionary<string, string>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var handler = new GetComplianceMaturityDashboardQueryHandler(_proxy.Object);
        var result  = await handler.Handle(
            new GetComplianceMaturityDashboardQuery("1"),
            CancellationToken.None);

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task Handle_AlwaysUsesHttpGetMethod_NeverPost()
    {
        _proxy
            .Setup(p => p.ProxyToComplianceServiceAsync(
                It.IsAny<string>(),
                It.IsAny<HttpMethod>(),
                It.IsAny<object?>(),
                It.IsAny<Dictionary<string, string>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ContentResult { StatusCode = 200 });

        var handler = new GetComplianceMaturityDashboardQueryHandler(_proxy.Object);
        await handler.Handle(new GetComplianceMaturityDashboardQuery("1"), CancellationToken.None);

        _proxy.Verify(
            p => p.ProxyToComplianceServiceAsync(
                It.IsAny<string>(),
                HttpMethod.Get,
                null,
                null,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
