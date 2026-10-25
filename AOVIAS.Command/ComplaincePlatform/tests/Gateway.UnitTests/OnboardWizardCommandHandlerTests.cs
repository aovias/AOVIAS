using System.Collections.Generic;
using System.Security.Claims;
using System.Text.Json;
using Gateway.Application.Commands;
using Gateway.Application.Handlers;
using Gateway.Configuration;
using Gateway.DTOs;
using Gateway.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Shared.Infrastructure.DTO;
using Xunit;

namespace Gateway.UnitTests;

public sealed class OnboardWizardCommandHandlerTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public async Task Handle_dual_writes_compliance_audit_and_kb_from_wizard_assessment()
    {
        var orgId = Guid.NewGuid();
        var hospitalId = Guid.NewGuid();
        var kbFrameworkId = Guid.NewGuid();
        var complianceFrameworkId = Guid.NewGuid();
        var assessmentId = Guid.NewGuid();
        var oeId = Guid.NewGuid();

        var req = new OnboardWizardRequestDto
        {
            OrganizationId = orgId,
            HospitalId = hospitalId,
            HospitalProfile = new HospitalOnboardingProfileDto
            {
                HospitalId = hospitalId,
                DepartmentIds = new List<Guid> { Guid.NewGuid() }
            },
            FrameworkCodes = new List<string> { "NABH_HOSP_6TH" },
            AssessmentAnswers = new List<WizardAssessmentAnswerClientDto>
            {
                new()
                {
                    QuestionId = oeId.ToString(),
                    Question = "OE question",
                    Answer = "yes"
                }
            },
            TeamInvites = new List<TeamInviteClientDto>(),
            WizardVersion = 3
        };

        var complianceAssessmentPosted = false;
        var kbFromWizardPosted = false;

        var proxy = new Mock<IProxyService>();

        proxy
            .Setup(p => p.ProxyToOrganizationServiceAsync(
                It.IsAny<string>(),
                It.IsAny<HttpMethod>(),
                It.IsAny<object?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ContentResult { StatusCode = 200, Content = "{}" });

        proxy
            .Setup(p => p.ProxyToComplianceServiceAsync(
                "/api/v1/onboarding/wizard/assessments",
                HttpMethod.Post,
                It.IsAny<object?>(),
                null,
                It.IsAny<CancellationToken>()))
            .Callback(() => complianceAssessmentPosted = true)
            .ReturnsAsync(new ContentResult { StatusCode = 201, Content = "{\"id\":\"" + Guid.NewGuid() + "\"}" });

        proxy
            .Setup(p => p.ProxyToKnowledgeBaseServiceAsync(
                "/api/v1.0/frameworks",
                HttpMethod.Get,
                null,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ContentResult
            {
                StatusCode = 200,
                Content = JsonSerializer.Serialize(new ApiResponse<List<KbFrameworkStub>>
                {
                    Success = true,
                    Data = new List<KbFrameworkStub>
                    {
                        new() { Id = kbFrameworkId, Code = "NABH_HOSP_6TH" }
                    }
                }, JsonOptions)
            });

        proxy
            .Setup(p => p.ProxyToKnowledgeBaseServiceAsync(
                "/api/v1.0/assessments/from-wizard",
                HttpMethod.Post,
                It.IsAny<object?>(),
                null,
                It.IsAny<CancellationToken>()))
            .Callback(() => kbFromWizardPosted = true)
            .ReturnsAsync(new ContentResult
            {
                StatusCode = 200,
                Content = JsonSerializer.Serialize(new ApiResponse<KbSubmitWizardStub>
                {
                    Success = true,
                    Data = new KbSubmitWizardStub
                    {
                        AssessmentId = assessmentId,
                        Summary = new OnboardingAssessmentSummaryDto { OverallPercent = 100m }
                    }
                }, JsonOptions)
            });

        proxy
            .Setup(p => p.ProxyToComplianceServiceAsync(
                "/api/v1/frameworks",
                HttpMethod.Get,
                null,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ContentResult
            {
                StatusCode = 200,
                Content = JsonSerializer.Serialize(new ApiResponse<List<FrameworkDto>>
                {
                    Success = true,
                    Data = new List<FrameworkDto>
                    {
                        new()
                        {
                            FrameworkId = complianceFrameworkId,
                            Code = "NABH_HOSP_6TH",
                            Name = "NABH",
                            IsActive = true
                        }
                    }
                }, JsonOptions)
            });

        proxy
            .Setup(p => p.ProxyToComplianceServiceAsync(
                It.Is<string>(s => s.Contains("/frameworks/") && s.Contains("/enroll")),
                HttpMethod.Post,
                null,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ContentResult { StatusCode = 201, Content = "{}" });

        proxy
            .Setup(p => p.ProxyToComplianceServiceAsync(
                It.Is<string>(s => s.Contains("/frameworks/") && s.Contains("/journeys")),
                HttpMethod.Post,
                null,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ContentResult { StatusCode = 201, Content = "{}" });

        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("organization_id", orgId.ToString()),
            new Claim("hospital_id", hospitalId.ToString())
        ], "test"));

        var http = new Mock<IHttpContextAccessor>();
        http.Setup(h => h.HttpContext).Returns(httpContext);

        var handler = new OnboardWizardCommandHandler(
            proxy.Object,
            http.Object,
            Options.Create(new OnboardingWizardOptions()),
            NullLogger<OnboardWizardCommandHandler>.Instance);

        var response = await handler.Handle(new OnboardWizardCommand(req), CancellationToken.None);

        Assert.True(complianceAssessmentPosted);
        Assert.True(kbFromWizardPosted);
        Assert.Equal(assessmentId, response.AssessmentId);
        Assert.NotNull(response.AssessmentSummary);
        Assert.Equal(100m, response.AssessmentSummary!.OverallPercent);
        Assert.Single(response.AssessmentIds!);

        proxy.Verify(
            p => p.ProxyToComplianceServiceAsync(
                "/api/v1/onboarding/wizard/assessments",
                HttpMethod.Post,
                It.IsAny<object?>(),
                null,
                It.IsAny<CancellationToken>()),
            Times.Once);

        proxy.Verify(
            p => p.ProxyToKnowledgeBaseServiceAsync(
                "/api/v1.0/assessments/from-wizard",
                HttpMethod.Post,
                It.IsAny<object?>(),
                null,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private sealed class KbFrameworkStub
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
    }

    private sealed class KbSubmitWizardStub
    {
        public Guid AssessmentId { get; set; }
        public OnboardingAssessmentSummaryDto? Summary { get; set; }
    }
}
