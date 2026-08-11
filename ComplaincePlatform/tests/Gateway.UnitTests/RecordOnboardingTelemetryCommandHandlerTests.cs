using System.Security.Claims;
using Gateway.Application.Commands;
using Gateway.Application.Handlers;
using Gateway.DTOs;
using Gateway.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Gateway.UnitTests;

public sealed class RecordOnboardingTelemetryCommandHandlerTests
{
    [Fact]
    public async Task Handle_proxies_to_organization_internal_events()
    {
        var orgId = Guid.NewGuid();
        var proxy = new Mock<IProxyService>();
        proxy.Setup(p => p.ProxyToOrganizationServiceAsync(
                "/internal/onboarding/events",
                HttpMethod.Post,
                It.IsAny<object?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OkObjectResult(new { ok = true }));

        var http = new DefaultHttpContext();
        http.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("organization_id", orgId.ToString()),
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
        }, "test"));

        var handler = new RecordOnboardingTelemetryCommandHandler(
            proxy.Object,
            new HttpContextAccessor { HttpContext = http },
            NullLogger<RecordOnboardingTelemetryCommandHandler>.Instance);

        var result = await handler.Handle(
            new RecordOnboardingTelemetryCommand(new OnboardingTelemetryEventRequestDto
            {
                EventType = "OnboardingOpened",
                Stage = "Welcome"
            }),
            CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        proxy.Verify(p => p.ProxyToOrganizationServiceAsync(
            "/internal/onboarding/events",
            HttpMethod.Post,
            It.IsAny<object?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_requires_organization_context()
    {
        var proxy = new Mock<IProxyService>();
        var http = new DefaultHttpContext();
        http.User = new ClaimsPrincipal(new ClaimsIdentity());

        var handler = new RecordOnboardingTelemetryCommandHandler(
            proxy.Object,
            new HttpContextAccessor { HttpContext = http },
            NullLogger<RecordOnboardingTelemetryCommandHandler>.Instance);

        var result = await handler.Handle(
            new RecordOnboardingTelemetryCommand(new OnboardingTelemetryEventRequestDto
            {
                EventType = "OnboardingOpened"
            }),
            CancellationToken.None);

        Assert.IsNotType<OkObjectResult>(result);
        proxy.Verify(p => p.ProxyToOrganizationServiceAsync(
            It.IsAny<string>(), It.IsAny<HttpMethod>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
