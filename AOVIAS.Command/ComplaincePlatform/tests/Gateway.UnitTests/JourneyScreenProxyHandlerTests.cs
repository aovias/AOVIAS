using System.Collections.Generic;
using Gateway.Application.Handlers;
using Gateway.Application.Queries;
using Gateway.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Gateway.UnitTests;

public class JourneyScreenProxyHandlerTests
{
    [Fact]
    public async Task GetJourneySessionQuery_forwards_expected_path_and_query()
    {
        var proxy = new Mock<IProxyService>();
        proxy
            .Setup(p => p.ProxyToComplianceServiceAsync(
                It.IsAny<string>(),
                HttpMethod.Get,
                It.IsAny<object?>(),
                It.IsAny<Dictionary<string, string>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ContentResult { StatusCode = 200, Content = "{}" });

        var http = new DefaultHttpContext();
        http.TraceIdentifier = "trace";
        var accessor = new HttpContextAccessor { HttpContext = http };

        var handler = new GetJourneySessionQueryHandler(proxy.Object, accessor);
        var journeyId = Guid.NewGuid();
        await handler.Handle(new GetJourneySessionQuery(journeyId, "1.0", "?operationalMode=2"), CancellationToken.None);

        proxy.Verify(
            p => p.ProxyToComplianceServiceAsync(
                $"/api/v1.0/journeys/{journeyId}/session?operationalMode=2",
                HttpMethod.Get,
                It.IsAny<object?>(),
                It.IsAny<Dictionary<string, string>?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetJourneyChaptersSplitQuery_forwards_chapters_route()
    {
        var proxy = new Mock<IProxyService>();
        proxy
            .Setup(p => p.ProxyToComplianceServiceAsync(
                It.IsAny<string>(),
                HttpMethod.Get,
                It.IsAny<object?>(),
                It.IsAny<Dictionary<string, string>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ContentResult { StatusCode = 200, Content = "{}" });

        var http = new DefaultHttpContext();
        http.TraceIdentifier = "trace";
        var accessor = new HttpContextAccessor { HttpContext = http };

        var handler = new GetJourneyChaptersSplitQueryHandler(proxy.Object, accessor);
        var journeyId = Guid.NewGuid();
        await handler.Handle(new GetJourneyChaptersSplitQuery(journeyId, "1.0", null), CancellationToken.None);

        proxy.Verify(
            p => p.ProxyToComplianceServiceAsync(
                $"/api/v1.0/journeys/{journeyId}/chapters",
                HttpMethod.Get,
                It.IsAny<object?>(),
                It.IsAny<Dictionary<string, string>?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
