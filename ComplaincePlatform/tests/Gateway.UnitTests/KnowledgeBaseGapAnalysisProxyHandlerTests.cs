using Gateway.Application.Commands;
using Gateway.Application.Handlers;
using Gateway.Application.Queries;
using Gateway.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Gateway.UnitTests;

public sealed class KnowledgeBaseGapAnalysisProxyHandlerTests
{
    [Fact]
    public async Task PostKnowledgeBaseGapAnalysisCommand_forwards_correct_kb_path()
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

        var http = new Mock<IHttpContextAccessor>();
        var assessmentId = Guid.NewGuid();
        var handler = new PostKnowledgeBaseGapAnalysisCommandHandler(proxy.Object, http.Object);
        await handler.Handle(new PostKnowledgeBaseGapAnalysisCommand(assessmentId, "1.0"), CancellationToken.None);

        proxy.Verify(
            p => p.ProxyToKnowledgeBaseServiceAsync(
                $"/api/v1.0/assessments/{assessmentId}/gap-analysis",
                HttpMethod.Post,
                null,
                null,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetKnowledgeBaseGapAnalysisByAssessmentQuery_forwards_correct_kb_path()
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
        var assessmentId = Guid.NewGuid();
        var handler = new GetKnowledgeBaseGapAnalysisByAssessmentQueryHandler(proxy.Object, http.Object);
        await handler.Handle(new GetKnowledgeBaseGapAnalysisByAssessmentQuery(assessmentId, "1.0"), CancellationToken.None);

        proxy.Verify(
            p => p.ProxyToKnowledgeBaseServiceAsync(
                $"/api/v1.0/assessments/{assessmentId}/gap-analysis",
                HttpMethod.Get,
                null,
                null,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
