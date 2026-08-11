using System.Text.Json;
using Gateway.Application.Commands;
using Gateway.Application.Handlers;
using Gateway.Configuration;
using Gateway.DTOs;
using Gateway.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Shared.Infrastructure.Email;
using Xunit;

namespace Gateway.UnitTests;

public sealed class SignupCommandHandlerTrialProvisioningTests
{
    private static SignupRequestDto SampleRequest() => new()
    {
        Organization = new OrganizationDto
        {
            Name = "Trial Hospital Org",
            Type = "Private Hospital",
            Address = "1 Main St"
        },
        Hospital = new HospitalDto
        {
            Name = "Trial Hospital",
            Code = "TH-01",
            Address = "1 Main St",
            NABHLevel = "Entry Level",
            CountryCode = "IN",
            StateCode = "KA"
        },
        AdminUser = new AdminUserDto
        {
            FirstName = "Ada",
            LastName = "Admin",
            Email = "ada@example.com",
            Password = "Password1!"
        }
    };

    private static IOptions<TrialEnablementOptions> TrialOptions(bool autoIssue = true, bool autoActivate = true)
        => Options.Create(new TrialEnablementOptions
        {
            AutoIssueOnSignup = autoIssue,
            AutoActivate = autoActivate,
            ValidDays = 30,
            MaxSeats = 10,
            GracePeriodDays = 7,
            MachineFingerprintMode = "SignupHost",
            DualWriteOrganizationSubscription = false
        });

    private static IConfiguration Config()
        => new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Identity:EmailVerificationMode"] = "Async"
            })
            .Build();

    private static Mock<IEmailService> EmailMock()
    {
        var email = new Mock<IEmailService>();
        email.Setup(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        return email;
    }

    private static ContentResult OkJson(object payload)
        => new()
        {
            StatusCode = 200,
            Content = JsonSerializer.Serialize(new { success = true, data = payload })
        };

    [Fact]
    public async Task Handle_issues_activates_and_assigns_seat_before_token()
    {
        var orgId = Guid.NewGuid();
        var hospitalId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var licenseId = Guid.NewGuid();
        var keyCode = "AOVI-TEST-TRIAL-0001";
        var callOrder = new List<string>();

        var proxy = new Mock<IProxyService>();
        proxy.Setup(p => p.ProxyToOrganizationServiceAsync(
                It.IsAny<string>(), It.IsAny<HttpMethod>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string endpoint, HttpMethod method, object? body, CancellationToken _) =>
            {
                callOrder.Add($"{method.Method} {endpoint}");
                if (endpoint == "/internal/organizations" && method == HttpMethod.Post)
                    return OkJson(new { id = orgId });
                if (endpoint == "/internal/hospitals" && method == HttpMethod.Post)
                    return OkJson(new { id = hospitalId });
                if (endpoint.Contains("/ensure-defaults", StringComparison.Ordinal))
                    return OkJson(new { organizationId = orgId, settingsCreated = false });
                if (endpoint == "/internal/organization-members")
                    return new ContentResult { StatusCode = 200, Content = "{}" };
                if (endpoint == "/internal/licenses/trial/get-or-issue")
                    return OkJson(new
                    {
                        id = licenseId,
                        keyCode,
                        status = "Issued",
                        expiresAtUtc = DateTime.UtcNow.AddDays(30),
                        maxSeats = 10,
                        reused = false
                    });
                if (endpoint == "/internal/licenses/activate")
                    return OkJson(new
                    {
                        licenseKeyId = licenseId,
                        status = "Active",
                        expiresAtUtc = DateTime.UtcNow.AddDays(30),
                        maxSeats = 10
                    });
                if (endpoint == "/internal/licenses/seats/assign")
                    return OkJson(new { id = Guid.NewGuid(), isActive = true });
                return new ContentResult { StatusCode = 200, Content = "{}" };
            });

        proxy.Setup(p => p.ProxyRequestAsync(
                "IdentityClient",
                It.IsAny<string>(),
                It.IsAny<HttpMethod>(),
                It.IsAny<object?>(),
                It.IsAny<bool>(),
                It.IsAny<Dictionary<string, string>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((string _, string endpoint, HttpMethod method, object? __, bool ___, Dictionary<string, string>? ____, CancellationToken _____) =>
            {
                callOrder.Add($"{method.Method} {endpoint}");
                if (endpoint == "/internal/users/register")
                    return new ContentResult
                    {
                        StatusCode = 200,
                        Content = JsonSerializer.Serialize(new { userId = userId.ToString() })
                    };
                if (endpoint == "/internal/token/generate")
                    return new ContentResult
                    {
                        StatusCode = 200,
                        Content = JsonSerializer.Serialize(new
                        {
                            token = "tok",
                            refreshToken = "ref",
                            expiresIn = 3600
                        })
                    };
                return new ContentResult { StatusCode = 200, Content = "{}" };
            });

        var handler = new SignupCommandHandler(
            proxy.Object,
            NullLogger<SignupCommandHandler>.Instance,
            Config(),
            new HttpContextAccessor { HttpContext = new DefaultHttpContext() },
            TrialOptions(),
            Options.Create(new OnboardingExperienceOptions()),
            EmailMock().Object);

        var response = await handler.Handle(new SignupCommand(SampleRequest()), CancellationToken.None);

        Assert.Equal("Completed", response.ProvisioningStatus);
        Assert.Equal(licenseId, response.LicenseKeyId);
        Assert.Equal("Active", response.LicenseStatus);
        Assert.Equal(10, response.MaxSeats);
        Assert.Equal(orgId, response.OrganizationId);
        Assert.Contains(callOrder, c => c.Contains("/internal/licenses/trial/get-or-issue"));
        Assert.Contains(callOrder, c => c.Contains("/internal/licenses/activate"));
        Assert.Contains(callOrder, c => c.Contains("/internal/licenses/seats/assign"));
        Assert.True(callOrder.FindIndex(c => c.Contains("get-or-issue"))
                    < callOrder.FindIndex(c => c.Contains("/internal/token/generate")));
    }

    [Fact]
    public async Task Handle_fails_and_rolls_back_when_trial_issue_fails()
    {
        var orgId = Guid.NewGuid();
        var hospitalId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var deletedOrg = false;

        var proxy = new Mock<IProxyService>();
        proxy.Setup(p => p.ProxyToOrganizationServiceAsync(
                It.IsAny<string>(), It.IsAny<HttpMethod>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string endpoint, HttpMethod method, object? _, CancellationToken __) =>
            {
                if (endpoint == "/internal/organizations" && method == HttpMethod.Post)
                    return OkJson(new { id = orgId });
                if (endpoint.StartsWith("/internal/organizations/", StringComparison.Ordinal) && method == HttpMethod.Delete)
                {
                    deletedOrg = true;
                    return new ContentResult { StatusCode = 204, Content = "" };
                }
                if (endpoint == "/internal/hospitals" && method == HttpMethod.Post)
                    return OkJson(new { id = hospitalId });
                if (endpoint.Contains("/ensure-defaults", StringComparison.Ordinal))
                    return OkJson(new { organizationId = orgId });
                if (endpoint == "/internal/organization-members")
                    return new ContentResult { StatusCode = 200, Content = "{}" };
                if (endpoint == "/internal/licenses/trial/get-or-issue")
                    return new ContentResult { StatusCode = 400, Content = "{\"success\":false}" };
                if (endpoint.StartsWith("/internal/hospitals/", StringComparison.Ordinal) && method == HttpMethod.Delete)
                    return new ContentResult { StatusCode = 204, Content = "" };
                return new ContentResult { StatusCode = 200, Content = "{}" };
            });

        proxy.Setup(p => p.ProxyRequestAsync(
                "IdentityClient",
                It.IsAny<string>(),
                It.IsAny<HttpMethod>(),
                It.IsAny<object?>(),
                It.IsAny<bool>(),
                It.IsAny<Dictionary<string, string>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((string _, string endpoint, HttpMethod __, object? ___, bool ____, Dictionary<string, string>? _____, CancellationToken ______) =>
            {
                if (endpoint == "/internal/users/register")
                    return new ContentResult
                    {
                        StatusCode = 200,
                        Content = JsonSerializer.Serialize(new { userId = userId.ToString() })
                    };
                if (endpoint.StartsWith("/internal/users/", StringComparison.Ordinal) && endpoint != "/internal/users/register")
                    return new ContentResult { StatusCode = 204, Content = "" };
                return new ContentResult { StatusCode = 200, Content = "{}" };
            });

        var handler = new SignupCommandHandler(
            proxy.Object,
            NullLogger<SignupCommandHandler>.Instance,
            Config(),
            new HttpContextAccessor { HttpContext = new DefaultHttpContext() },
            TrialOptions(),
            Options.Create(new OnboardingExperienceOptions()),
            EmailMock().Object);

        var ex = await Assert.ThrowsAsync<OnboardingStepException>(() =>
            handler.Handle(new SignupCommand(SampleRequest()), CancellationToken.None));

        Assert.Equal("IssueTrialLicense", ex.StepName);
        Assert.True(deletedOrg);
    }

    [Fact]
    public async Task Handle_skips_license_when_auto_issue_disabled()
    {
        var orgId = Guid.NewGuid();
        var hospitalId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var licenseCalled = false;

        var proxy = new Mock<IProxyService>();
        proxy.Setup(p => p.ProxyToOrganizationServiceAsync(
                It.IsAny<string>(), It.IsAny<HttpMethod>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string endpoint, HttpMethod method, object? _, CancellationToken __) =>
            {
                if (endpoint.Contains("licenses", StringComparison.OrdinalIgnoreCase))
                {
                    licenseCalled = true;
                }
                if (endpoint == "/internal/organizations" && method == HttpMethod.Post)
                    return OkJson(new { id = orgId });
                if (endpoint == "/internal/hospitals" && method == HttpMethod.Post)
                    return OkJson(new { id = hospitalId });
                if (endpoint.Contains("/ensure-defaults", StringComparison.Ordinal))
                    return OkJson(new { organizationId = orgId });
                if (endpoint == "/internal/organization-members")
                    return new ContentResult { StatusCode = 200, Content = "{}" };
                return new ContentResult { StatusCode = 200, Content = "{}" };
            });

        proxy.Setup(p => p.ProxyRequestAsync(
                "IdentityClient",
                It.IsAny<string>(),
                It.IsAny<HttpMethod>(),
                It.IsAny<object?>(),
                It.IsAny<bool>(),
                It.IsAny<Dictionary<string, string>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((string _, string endpoint, HttpMethod __, object? ___, bool ____, Dictionary<string, string>? _____, CancellationToken ______) =>
            {
                if (endpoint == "/internal/users/register")
                    return new ContentResult
                    {
                        StatusCode = 200,
                        Content = JsonSerializer.Serialize(new { userId = userId.ToString() })
                    };
                if (endpoint == "/internal/token/generate")
                    return new ContentResult
                    {
                        StatusCode = 200,
                        Content = JsonSerializer.Serialize(new
                        {
                            token = "tok",
                            refreshToken = "ref",
                            expiresIn = 3600
                        })
                    };
                return new ContentResult { StatusCode = 200, Content = "{}" };
            });

        var handler = new SignupCommandHandler(
            proxy.Object,
            NullLogger<SignupCommandHandler>.Instance,
            Config(),
            new HttpContextAccessor { HttpContext = new DefaultHttpContext() },
            TrialOptions(autoIssue: false),
            Options.Create(new OnboardingExperienceOptions()),
            EmailMock().Object);

        var response = await handler.Handle(new SignupCommand(SampleRequest()), CancellationToken.None);

        Assert.Equal("Skipped", response.ProvisioningStatus);
        Assert.Null(response.LicenseKeyId);
        Assert.False(licenseCalled);
    }
}
