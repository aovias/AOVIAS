using System.Collections.Generic;
using System.Text.Json;
using Gateway.Application.Handlers;
using Gateway.Application.Queries;
using Gateway.DTOs;
using Gateway.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Shared.Infrastructure.DTO;
using Xunit;

namespace Gateway.UnitTests;

public sealed class OnboardingQuestionnaireProxyHandlerTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public async Task GetOnboardingQuestionnaireQuery_forwards_correct_kb_path()
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
        var handler = new GetOnboardingQuestionnaireQueryHandler(proxy.Object, http.Object);
        var fwId = Guid.NewGuid();
        await handler.Handle(new GetOnboardingQuestionnaireQuery(fwId, "1.0"), CancellationToken.None);

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
    public async Task GetOnboardingQuestionnaireQuery_empty_framework_id_returns_validation_failure()
    {
        var proxy = new Mock<IProxyService>();
        var http = new Mock<IHttpContextAccessor>();
        http.Setup(h => h.HttpContext!.TraceIdentifier).Returns("trace");

        var handler = new GetOnboardingQuestionnaireQueryHandler(proxy.Object, http.Object);
        var result = await handler.Handle(new GetOnboardingQuestionnaireQuery(Guid.Empty, "1.0"), CancellationToken.None);

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

    [Fact]
    public async Task GetOnboardingQuestionnaireQuery_preserves_response_contract()
    {
        var fwId = Guid.NewGuid();
        var chapterId = Guid.NewGuid();
        var oeId = Guid.NewGuid();
        var payload = new ApiResponse<FrameworkQuestionnaireDto>
        {
            Success = true,
            Code = "SUCCESS",
            Message = "Operation completed successfully",
            Data = new FrameworkQuestionnaireDto(
                fwId,
                "NABH",
                new List<OnboardingQuestionnaireSectionDto>
                {
                    new(
                        chapterId,
                        "Access Assessment",
                        new List<OnboardingQuestionnaireQuestionDto>
                        {
                            new(oeId, oeId, "OE title — Ensure compliance", "Maintain records", "CORE", 3.0m)
                        })
                })
        };

        var proxy = new Mock<IProxyService>();
        proxy
            .Setup(p => p.ProxyToKnowledgeBaseServiceAsync(
                It.IsAny<string>(),
                HttpMethod.Get,
                It.IsAny<object?>(),
                It.IsAny<Dictionary<string, string>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ContentResult
            {
                StatusCode = 200,
                Content = JsonSerializer.Serialize(payload, JsonOptions)
            });

        var http = new Mock<IHttpContextAccessor>();
        var handler = new GetOnboardingQuestionnaireQueryHandler(proxy.Object, http.Object);
        var result = await handler.Handle(new GetOnboardingQuestionnaireQuery(fwId, "1.0"), CancellationToken.None);

        var content = Assert.IsType<ContentResult>(result);
        var envelope = JsonSerializer.Deserialize<ApiResponse<FrameworkQuestionnaireDto>>(content.Content!, JsonOptions);
        Assert.NotNull(envelope);
        Assert.True(envelope!.Success);
        Assert.Equal("SUCCESS", envelope.Code);
        Assert.NotNull(envelope.Data);
        Assert.Equal(fwId, envelope.Data!.FrameworkId);
        Assert.Equal("NABH", envelope.Data.FrameworkName);
        Assert.Single(envelope.Data.Sections);
        Assert.Equal(chapterId, envelope.Data.Sections[0].ChapterId);
        Assert.Equal("Access Assessment", envelope.Data.Sections[0].ChapterName);
        Assert.Single(envelope.Data.Sections[0].Questions);
        Assert.Equal(oeId, envelope.Data.Sections[0].Questions[0].ComplianceNodeId);
        Assert.Equal("OE title — Ensure compliance", envelope.Data.Sections[0].Questions[0].QuestionText);
        Assert.Equal("Maintain records", envelope.Data.Sections[0].Questions[0].HelpText);
        Assert.Equal("CORE", envelope.Data.Sections[0].Questions[0].Category);
        Assert.Equal(3.0m, envelope.Data.Sections[0].Questions[0].Weight);
    }
}
