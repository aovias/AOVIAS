using System.Collections.Generic;
using System.Text.Json;
using Gateway.Application.Commands;
using Gateway.Application.Handlers;
using Gateway.Application.Queries;
using Gateway.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Gateway.UnitTests;

public class KnowledgeBaseProxyHandlerTests
{
    [Fact]
    public async Task ListKnowledgeBaseFrameworksQuery_forwards_frameworks_list()
    {
        var proxy = new Mock<IProxyService>();
        proxy
            .Setup(p => p.ProxyToKnowledgeBaseServiceAsync(
                It.IsAny<string>(),
                HttpMethod.Get,
                It.IsAny<object?>(),
                It.IsAny<Dictionary<string, string>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ContentResult { StatusCode = 200, Content = "{}" });

        var handler = new ListKnowledgeBaseFrameworksQueryHandler(proxy.Object);
        await handler.Handle(new ListKnowledgeBaseFrameworksQuery("1.0"), CancellationToken.None);

        proxy.Verify(
            p => p.ProxyToKnowledgeBaseServiceAsync(
                "/api/v1.0/frameworks",
                HttpMethod.Get,
                null,
                null,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SearchKnowledgeBaseKnowledgeQuery_forwards_query_string()
    {
        var proxy = new Mock<IProxyService>();
        proxy
            .Setup(p => p.ProxyToKnowledgeBaseServiceAsync(
                It.IsAny<string>(),
                HttpMethod.Get,
                It.IsAny<object?>(),
                It.IsAny<Dictionary<string, string>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ContentResult { StatusCode = 200, Content = "{}" });

        var handler = new SearchKnowledgeBaseKnowledgeQueryHandler(proxy.Object);
        var fw = Guid.NewGuid();
        await handler.Handle(new SearchKnowledgeBaseKnowledgeQuery("nabh", fw, "1.0"), CancellationToken.None);

        proxy.Verify(
            p => p.ProxyToKnowledgeBaseServiceAsync(
                $"/api/v1.0/knowledge/search?q=nabh&frameworkId={fw}",
                HttpMethod.Get,
                null,
                null,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetKnowledgeBaseKnowledgeByNodeCodeQuery_forwards_encoded_path()
    {
        var proxy = new Mock<IProxyService>();
        proxy
            .Setup(p => p.ProxyToKnowledgeBaseServiceAsync(
                It.IsAny<string>(),
                HttpMethod.Get,
                It.IsAny<object?>(),
                It.IsAny<Dictionary<string, string>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ContentResult { StatusCode = 200, Content = "{}" });

        var http = new Mock<IHttpContextAccessor>();
        http.Setup(h => h.HttpContext!.TraceIdentifier).Returns("trace");

        var handler = new GetKnowledgeBaseKnowledgeByNodeCodeQueryHandler(proxy.Object, http.Object);
        await handler.Handle(new GetKnowledgeBaseKnowledgeByNodeCodeQuery("CH1.2", "1.0"), CancellationToken.None);

        proxy.Verify(
            p => p.ProxyToKnowledgeBaseServiceAsync(
                $"/api/v1.0/knowledge/node/code/{Uri.EscapeDataString("CH1.2")}",
                HttpMethod.Get,
                null,
                null,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetKnowledgeBaseNodeDetailsByCodeQuery_forwards_encoded_path()
    {
        var proxy = new Mock<IProxyService>();
        proxy
            .Setup(p => p.ProxyToKnowledgeBaseServiceAsync(
                It.IsAny<string>(),
                HttpMethod.Get,
                It.IsAny<object?>(),
                It.IsAny<Dictionary<string, string>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ContentResult { StatusCode = 200, Content = "{}" });

        var http = new Mock<IHttpContextAccessor>();
        http.Setup(h => h.HttpContext!.TraceIdentifier).Returns("trace");

        var handler = new GetKnowledgeBaseNodeDetailsByCodeQueryHandler(proxy.Object, http.Object);
        await handler.Handle(new GetKnowledgeBaseNodeDetailsByCodeQuery("CH1", "1.0"), CancellationToken.None);

        proxy.Verify(
            p => p.ProxyToKnowledgeBaseServiceAsync(
                $"/api/v1.0/nodes/code/{Uri.EscapeDataString("CH1")}/details",
                HttpMethod.Get,
                null,
                null,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task PostKnowledgeBaseAiAskCommand_forwards_post_body()
    {
        var proxy = new Mock<IProxyService>();
        proxy
            .Setup(p => p.ProxyToKnowledgeBaseServiceAsync(
                It.IsAny<string>(),
                HttpMethod.Post,
                It.IsAny<object?>(),
                It.IsAny<Dictionary<string, string>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ContentResult { StatusCode = 200, Content = "{}" });

        var body = JsonSerializer.SerializeToElement(new { frameworkId = Guid.NewGuid(), query = "test" });
        var httpContext = new Mock<IHttpContextAccessor>();
        var handler = new PostKnowledgeBaseAiAskCommandHandler(proxy.Object, httpContext.Object);
        await handler.Handle(new PostKnowledgeBaseAiAskCommand(body, "1.0"), CancellationToken.None);

        proxy.Verify(
            p => p.ProxyToKnowledgeBaseServiceAsync(
                "/api/v1.0/ai/ask",
                HttpMethod.Post,
                It.Is<object?>(o => o is JsonElement),
                null,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetFrameworkQuestionnaireQuery_forwards_correct_path()
    {
        var proxy = new Mock<IProxyService>();
        proxy
            .Setup(p => p.ProxyToKnowledgeBaseServiceAsync(
                It.IsAny<string>(),
                HttpMethod.Get,
                It.IsAny<object?>(),
                It.IsAny<Dictionary<string, string>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ContentResult { StatusCode = 200, Content = "{}" });

        var http = new Mock<IHttpContextAccessor>();
        var handler = new GetFrameworkQuestionnaireQueryHandler(proxy.Object, http.Object);
        var fwId = Guid.NewGuid();
        await handler.Handle(new GetFrameworkQuestionnaireQuery(fwId, "1.0"), CancellationToken.None);

        proxy.Verify(
            p => p.ProxyToKnowledgeBaseServiceAsync(
                $"/api/v1.0/frameworks/{fwId}/questionnaire",
                HttpMethod.Get,
                null,
                null,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetFrameworkQuestionnaireQuery_empty_framework_id_returns_validation_failure()
    {
        var proxy = new Mock<IProxyService>();
        var http = new Mock<IHttpContextAccessor>();
        http.Setup(h => h.HttpContext!.TraceIdentifier).Returns("trace");

        var handler = new GetFrameworkQuestionnaireQueryHandler(proxy.Object, http.Object);
        var result = await handler.Handle(new GetFrameworkQuestionnaireQuery(Guid.Empty, "1.0"), CancellationToken.None);

        var status = Assert.IsType<ObjectResult>(result);
        Assert.Equal(400, status.StatusCode);
        proxy.Verify(
            p => p.ProxyToKnowledgeBaseServiceAsync(
                It.IsAny<string>(),
                It.IsAny<HttpMethod>(),
                It.IsAny<object?>(),
                It.IsAny<Dictionary<string, string>?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
