using Gateway.Application.Handlers;
using Gateway.Application.Queries;
using Gateway.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Platform.Dashboard.Models;
using Xunit;

namespace Gateway.UnitTests;

/// <summary>
/// Ensures Gateway can deserialize ComplianceService experience payloads that use
/// camelCase string enums for <see cref="DashboardLoadingPhase"/>.
/// </summary>
public sealed class GetDashboardExperienceQueryHandlerTests
{
    private readonly Mock<IProxyService> _proxy = new();

    [Fact]
    public async Task Handle_DeserializesCamelCaseWidgetPhases()
    {
        var hospitalId = Guid.Parse("d9f5554a-bf83-4262-b386-a31587b0ba06");
        const string content = """
            {
              "success": true,
              "data": {
                "summary": null,
                "widgets": [
                  {
                    "id": "home-executive-kpi-strip",
                    "application": "Home",
                    "title": "Executive KPI Strip",
                    "icon": "bar-chart-2",
                    "priority": 10,
                    "phase": "criticalWidgets",
                    "permissions": [],
                    "lazy": false,
                    "critical": true,
                    "visibility": "Visible"
                  }
                ],
                "sections": [
                  {
                    "id": "home-summary",
                    "application": "Home",
                    "title": "Home Summary",
                    "icon": "layout-dashboard",
                    "priority": 10,
                    "phase": "summary",
                    "widgetIds": ["home-executive-kpi-strip"],
                    "critical": true,
                    "visibility": "Visible"
                  }
                ]
              }
            }
            """;

        _proxy
            .Setup(p => p.ProxyToComplianceServiceAsync(
                It.IsAny<string>(),
                HttpMethod.Get,
                It.IsAny<object?>(),
                It.IsAny<Dictionary<string, string>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ContentResult
            {
                StatusCode = 200,
                Content = content,
                ContentType = "application/json",
            });

        var handler = new GetDashboardExperienceQueryHandler(
            _proxy.Object,
            NullLogger<GetDashboardExperienceQueryHandler>.Instance);

        var result = await handler.Handle(new GetDashboardExperienceQuery(hospitalId), CancellationToken.None);

        Assert.Single(result.Widgets);
        Assert.Equal(DashboardLoadingPhase.CriticalWidgets, result.Widgets[0].Phase);
        Assert.Single(result.Sections);
        Assert.Equal(DashboardLoadingPhase.Summary, result.Sections[0].Phase);

        _proxy.Verify(
            p => p.ProxyToComplianceServiceAsync(
                $"/api/v1/platform/dashboard/experience?hospitalId={hospitalId}",
                HttpMethod.Get,
                null,
                null,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
