using Gateway.Application.Commands;
using Gateway.Configuration;
using Gateway.DTOs;
using Gateway.Services;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Shared.Infrastructure.Email;

namespace Gateway.Application.Handlers;

/// <summary>
/// Handler for SignupCommand that orchestrates organization, hospital, user, and trial license provisioning.
/// </summary>
public class SignupCommandHandler : IRequestHandler<SignupCommand, SignupResponseDto>
{
    private readonly IProxyService _proxyService;
    private readonly ILogger<SignupCommandHandler> _logger;
    private readonly IConfiguration _configuration;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly TrialEnablementOptions _trialOptions;
    private readonly OnboardingExperienceOptions _experienceOptions;
    private readonly IEmailService _emailService;

    public SignupCommandHandler(
        IProxyService proxyService,
        ILogger<SignupCommandHandler> logger,
        IConfiguration configuration,
        IHttpContextAccessor httpContextAccessor,
        IOptions<TrialEnablementOptions> trialOptions,
        IOptions<OnboardingExperienceOptions> experienceOptions,
        IEmailService emailService)
    {
        _proxyService = proxyService;
        _logger = logger;
        _configuration = configuration;
        _httpContextAccessor = httpContextAccessor;
        _trialOptions = trialOptions.Value;
        _experienceOptions = experienceOptions.Value;
        _emailService = emailService;
    }

    public async Task<SignupResponseDto> Handle(SignupCommand request, CancellationToken cancellationToken)
    {
        EnsureEmailVerificationAllowsAnonymousSignup();

        var rollbackActions = new Stack<Func<CancellationToken, Task>>();
        string currentStepName = "Start";
        var correlationId = _httpContextAccessor.HttpContext?.TraceIdentifier ?? Guid.NewGuid().ToString("N");

        try
        {
            currentStepName = "CreateOrganization";
            var organizationId = await CreateOrganizationAsync(request.Request, rollbackActions, cancellationToken);
            EmitProvisioningEvent("OrganizationProvisioned", organizationId, correlationId, currentStepName);

            currentStepName = "CreateHospital";
            var hospitalId = await CreateHospitalAsync(organizationId, request.Request, rollbackActions, cancellationToken);
            EmitProvisioningEvent("HospitalProvisioned", organizationId, correlationId, currentStepName);

            currentStepName = "EnsureDefaults";
            await EnsureOrganizationDefaultsAsync(organizationId, cancellationToken);
            EmitProvisioningEvent("DefaultDataSeeded", organizationId, correlationId, currentStepName);

            currentStepName = "CreateUser";
            var userId = await CreateUserAsync(request.Request, organizationId, hospitalId, rollbackActions, cancellationToken);

            currentStepName = "CreateOrganizationMember";
            await CreateOrganizationMemberAsync(organizationId, userId, request.Request, rollbackActions, cancellationToken);
            EmitProvisioningEvent("MembershipCreated", organizationId, correlationId, currentStepName);

            currentStepName = "ProvisionCommercialTrial";
            var commercial = await ProvisionCommercialTrialAsync(
                organizationId,
                userId,
                request.Request,
                rollbackActions,
                correlationId,
                cancellationToken);

            currentStepName = "GenerateToken";
            var (token, refreshToken, expiresIn) = await GenerateTokenAsync(
                userId, request.Request.AdminUser.Email, organizationId, hospitalId, cancellationToken);

            var signupResponse = new SignupResponseDto
            {
                Token = token,
                RefreshToken = refreshToken,
                ExpiresIn = expiresIn,
                UserId = userId,
                OrganizationId = organizationId,
                HospitalId = hospitalId,
                Email = request.Request.AdminUser.Email,
                Role = "ORG_ADMIN",
                LicenseKeyId = commercial.LicenseKeyId,
                LicenseStatus = commercial.LicenseStatus,
                TrialExpiresAtUtc = commercial.ExpiresAtUtc,
                MaxSeats = commercial.MaxSeats,
                ProvisioningStatus = commercial.ProvisioningStatus
            };

            currentStepName = "WelcomeEmail";
            await TrySendWelcomeEmailAsync(request.Request, commercial, cancellationToken);
            EmitProvisioningEvent("WelcomeEmailSent", organizationId, correlationId, currentStepName);
            EmitProvisioningEvent("ProvisioningCompleted", organizationId, correlationId, "Completed");

            _logger.LogInformation(
                "Signup completed for user {Email}, org {OrganizationId}, hospital {HospitalId}, provisioning={ProvisioningStatus}, license={LicenseKeyId}",
                request.Request.AdminUser.Email,
                organizationId,
                hospitalId,
                commercial.ProvisioningStatus,
                commercial.LicenseKeyId);

            return signupResponse;
        }
        catch (OnboardingStepException ex)
        {
            _logger.LogError(ex,
                "Onboarding failed at step {StepName} with HTTP {StatusCode}, Code={ResponseCode}, Body={ResponseBody}, CorrelationId={CorrelationId}",
                ex.StepName,
                ex.HttpStatusCode,
                ex.ResponseCode,
                ex.ResponseBody,
                correlationId);

            EmitProvisioningEvent("ProvisioningFailed", Guid.Empty, correlationId, ex.StepName);
            await RunRollbackAsync(rollbackActions, request, cancellationToken);
            EmitProvisioningEvent("ProvisioningRolledBack", Guid.Empty, correlationId, ex.StepName);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error during signup orchestration for organization {OrganizationName} at step {StepName}, CorrelationId={CorrelationId}",
                request.Request.Organization.Name,
                currentStepName,
                correlationId);

            EmitProvisioningEvent("ProvisioningFailed", Guid.Empty, correlationId, currentStepName);
            await RunRollbackAsync(rollbackActions, request, cancellationToken);
            EmitProvisioningEvent("ProvisioningRolledBack", Guid.Empty, correlationId, currentStepName);
            throw;
        }
    }

    private void EmitProvisioningEvent(string eventType, Guid organizationId, string correlationId, string step)
    {
        _logger.LogInformation(
            "ProvisioningEvent {EventType} OrganizationId={OrganizationId} CorrelationId={CorrelationId} Step={Step} IdempotencyKey={IdempotencyKey}",
            eventType,
            organizationId,
            correlationId,
            step,
            $"{eventType}:{organizationId:N}:{step}");
    }

    private sealed class CommercialProvisioningResult
    {
        public Guid? LicenseKeyId { get; set; }
        public string? LicenseStatus { get; set; }
        public DateTime? ExpiresAtUtc { get; set; }
        public int? MaxSeats { get; set; }
        public string ProvisioningStatus { get; set; } = "Skipped";
    }

    private async Task EnsureOrganizationDefaultsAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        var result = await _proxyService.ProxyToOrganizationServiceAsync(
            endpoint: $"/internal/organizations/{organizationId}/ensure-defaults",
            httpMethod: HttpMethod.Post,
            requestBody: null,
            cancellationToken: cancellationToken);

        if (result is not ContentResult content || (content.StatusCode ?? 0) >= (int)HttpStatusCode.BadRequest)
        {
            var status = (result as ContentResult)?.StatusCode ?? 0;
            var body = (result as ContentResult)?.Content ?? string.Empty;
            throw new OnboardingStepException(
                "EnsureDefaults",
                status,
                responseCode: null,
                responseBody: body,
                message: $"Failed to ensure organization defaults. Status={status}");
        }
    }

    private async Task<CommercialProvisioningResult> ProvisionCommercialTrialAsync(
        Guid organizationId,
        Guid userId,
        SignupRequestDto requestDto,
        Stack<Func<CancellationToken, Task>> rollbackActions,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var result = new CommercialProvisioningResult();

        if (!_trialOptions.AutoIssueOnSignup)
        {
            _logger.LogInformation(
                "TrialEnablement.AutoIssueOnSignup=false; skipping commercial trial for org {OrganizationId}",
                organizationId);
            result.ProvisioningStatus = "Skipped";
            await TryAssignDefaultTrialSubscriptionAsync(organizationId, cancellationToken);
            return result;
        }

        var issueBody = new
        {
            OrganizationId = organizationId,
            SubscriptionPlanId = _trialOptions.DefaultTrialPlanId,
            MaxSeats = _trialOptions.MaxSeats <= 0 ? 10 : _trialOptions.MaxSeats,
            ValidDays = _trialOptions.ValidDays <= 0 ? 30 : _trialOptions.ValidDays,
            GracePeriodDays = Math.Max(0, _trialOptions.GracePeriodDays),
            Entitlements = _trialOptions.Entitlements,
            CustomerName = requestDto.Organization.Name
        };

        var issueResult = await _proxyService.ProxyToOrganizationServiceAsync(
            endpoint: "/internal/licenses/trial/get-or-issue",
            httpMethod: HttpMethod.Post,
            requestBody: issueBody,
            cancellationToken: cancellationToken);

        if (issueResult is not ContentResult issueContent
            || (issueContent.StatusCode ?? 0) >= (int)HttpStatusCode.BadRequest
            || string.IsNullOrWhiteSpace(issueContent.Content))
        {
            throw new OnboardingStepException(
                "IssueTrialLicense",
                (issueResult as ContentResult)?.StatusCode ?? 0,
                responseCode: null,
                responseBody: (issueResult as ContentResult)?.Content,
                message: "Failed to issue trial license.");
        }

        var issueData = UnwrapData(issueContent.Content!);
        var licenseKeyId = GetGuidProperty(issueData, "id");
        var keyCode = GetStringProperty(issueData, "keyCode")
                      ?? throw new OnboardingStepException(
                          "IssueTrialLicense",
                          issueContent.StatusCode ?? 0,
                          null,
                          issueContent.Content,
                          "Trial issue response missing keyCode.");
        var reused = issueData.TryGetProperty("reused", out var reusedProp) && reusedProp.ValueKind == JsonValueKind.True;
        var status = GetStringProperty(issueData, "status") ?? "Issued";
        DateTime? expires = null;
        if (TryGetPropertyIgnoreCase(issueData, "expiresAtUtc", out var expProp) && expProp.ValueKind != JsonValueKind.Null)
        {
            expires = expProp.GetDateTime();
        }

        var maxSeats = TryGetPropertyIgnoreCase(issueData, "maxSeats", out var seatsProp) && seatsProp.TryGetInt32(out var ms)
            ? ms
            : _trialOptions.MaxSeats;

        result.LicenseKeyId = licenseKeyId;
        result.LicenseStatus = status;
        result.ExpiresAtUtc = expires;
        result.MaxSeats = maxSeats;

        EmitProvisioningEvent("TrialIssued", organizationId, correlationId, reused ? "Reused" : "Issued");

        if (!reused)
        {
            rollbackActions.Push(ct => RevokeLicenseAsync(licenseKeyId, ct));
        }

        if (_trialOptions.AutoActivate)
        {
            var fingerprint = ResolveMachineFingerprint();
            var activated = await TryActivateWithRetryAsync(
                keyCode,
                organizationId,
                fingerprint,
                requestDto.AdminUser.Email,
                cancellationToken);

            if (activated == null)
            {
                throw new OnboardingStepException(
                    "ActivateTrialLicense",
                    0,
                    null,
                    null,
                    "Failed to activate trial license after retry.");
            }

            result.LicenseStatus = activated.Status;
            result.ExpiresAtUtc = activated.ExpiresAtUtc ?? result.ExpiresAtUtc;
            result.MaxSeats = activated.MaxSeats > 0 ? activated.MaxSeats : result.MaxSeats;
            EmitProvisioningEvent("LicenseActivated", organizationId, correlationId, "Activate");
        }
        else if (string.Equals(result.LicenseStatus, "Issued", StringComparison.OrdinalIgnoreCase))
        {
            result.ProvisioningStatus = "LicenseIssuedPendingActivation";
        }

        var seatOk = await TryAssignSeatWithRetryAsync(
            organizationId,
            userId,
            requestDto.AdminUser.Email,
            cancellationToken);
        if (!seatOk)
        {
            throw new OnboardingStepException(
                "AssignSeat",
                0,
                null,
                null,
                "Failed to assign admin seat to trial license.");
        }

        rollbackActions.Push(ct => ReleaseSeatAsync(organizationId, userId, ct));
        EmitProvisioningEvent("SeatAssigned", organizationId, correlationId, "AssignSeat");

        if (result.ProvisioningStatus != "LicenseIssuedPendingActivation")
        {
            result.ProvisioningStatus = "Completed";
        }

        // CRP Phase 6 — catalogue sync from LicenseKey (SSOT). Legacy DualWrite remains as optional alias.
        if (_trialOptions.SyncCatalogFromLicense || _trialOptions.DualWriteOrganizationSubscription)
        {
            await TrySyncCatalogFromLicenseAsync(organizationId, cancellationToken);
        }

        return result;
    }

    private async Task TrySyncCatalogFromLicenseAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        try
        {
            await _proxyService.ProxyToOrganizationServiceAsync(
                endpoint: $"/internal/licenses/catalog/sync?organizationId={organizationId}",
                httpMethod: HttpMethod.Post,
                requestBody: null,
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Catalog sync from license failed for org {Org} (best-effort)", organizationId);
        }
    }

    private string ResolveMachineFingerprint()
    {
        if (string.Equals(_trialOptions.MachineFingerprintMode, "SignupHost", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(_trialOptions.MachineFingerprintMode))
        {
            return Environment.MachineName;
        }

        return _trialOptions.MachineFingerprintMode;
    }

    private async Task<LicenseActivationSnapshot?> TryActivateWithRetryAsync(
        string keyCode,
        Guid organizationId,
        string fingerprint,
        string activatedBy,
        CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= 2; attempt++)
        {
            try
            {
                var activateResult = await _proxyService.ProxyToOrganizationServiceAsync(
                    endpoint: "/internal/licenses/activate",
                    httpMethod: HttpMethod.Post,
                    requestBody: new
                    {
                        KeyCode = keyCode,
                        OrganizationId = organizationId,
                        MachineFingerprint = fingerprint,
                        ActivatedBy = activatedBy,
                        Offline = false
                    },
                    cancellationToken: cancellationToken);

                if (activateResult is ContentResult content
                    && (content.StatusCode ?? 0) < (int)HttpStatusCode.BadRequest
                    && !string.IsNullOrWhiteSpace(content.Content))
                {
                    var data = UnwrapData(content.Content);
                    return new LicenseActivationSnapshot
                    {
                        Status = GetStringProperty(data, "status") ?? "Active",
                        ExpiresAtUtc = TryGetPropertyIgnoreCase(data, "expiresAtUtc", out var e) && e.ValueKind != JsonValueKind.Null
                            ? e.GetDateTime()
                            : null,
                        MaxSeats = TryGetPropertyIgnoreCase(data, "maxSeats", out var m) && m.TryGetInt32(out var seats) ? seats : 0
                    };
                }

                _logger.LogWarning(
                    "License activate attempt {Attempt} failed. Status={Status} Body={Body}",
                    attempt,
                    (activateResult as ContentResult)?.StatusCode,
                    (activateResult as ContentResult)?.Content);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "License activate attempt {Attempt} threw", attempt);
            }
        }

        return null;
    }

    private async Task<bool> TryAssignSeatWithRetryAsync(
        Guid organizationId,
        Guid userId,
        string email,
        CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= 2; attempt++)
        {
            var seatResult = await _proxyService.ProxyToOrganizationServiceAsync(
                endpoint: "/internal/licenses/seats/assign",
                httpMethod: HttpMethod.Post,
                requestBody: new
                {
                    OrganizationId = organizationId,
                    UserId = userId,
                    UserEmail = email
                },
                cancellationToken: cancellationToken);

            if (seatResult is ContentResult content
                && (content.StatusCode ?? 0) < (int)HttpStatusCode.BadRequest)
            {
                return true;
            }

            _logger.LogWarning(
                "Seat assign attempt {Attempt} failed. Status={Status} Body={Body}",
                attempt,
                (seatResult as ContentResult)?.StatusCode,
                (seatResult as ContentResult)?.Content);
        }

        return false;
    }

    private sealed class LicenseActivationSnapshot
    {
        public string Status { get; set; } = "Active";
        public DateTime? ExpiresAtUtc { get; set; }
        public int MaxSeats { get; set; }
    }

    private static JsonElement UnwrapData(string body)
    {
        var root = JsonSerializer.Deserialize<JsonElement>(body, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (root.ValueKind == JsonValueKind.Object
            && root.TryGetProperty("data", out var data)
            && data.ValueKind == JsonValueKind.Object)
        {
            return data;
        }

        return root;
    }

    private static bool TryGetPropertyIgnoreCase(JsonElement element, string name, out JsonElement value)
    {
        if (element.TryGetProperty(name, out value))
        {
            return true;
        }

        foreach (var prop in element.EnumerateObject())
        {
            if (string.Equals(prop.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                value = prop.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    private static Guid GetGuidProperty(JsonElement element, string name)
    {
        if (!TryGetPropertyIgnoreCase(element, name, out var prop))
        {
            throw new InvalidOperationException($"Missing property {name}");
        }

        return prop.ValueKind == JsonValueKind.String ? Guid.Parse(prop.GetString()!) : prop.GetGuid();
    }

    private static string? GetStringProperty(JsonElement element, string name)
    {
        if (!TryGetPropertyIgnoreCase(element, name, out var prop))
        {
            return null;
        }

        return prop.ValueKind == JsonValueKind.String ? prop.GetString() : prop.ToString();
    }

    private async Task RevokeLicenseAsync(Guid licenseKeyId, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogWarning("Rollback: revoking license {LicenseKeyId}", licenseKeyId);
            await _proxyService.ProxyToOrganizationServiceAsync(
                endpoint: "/internal/licenses/revoke",
                httpMethod: HttpMethod.Post,
                requestBody: new { LicenseKeyId = licenseKeyId },
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to revoke license {LicenseKeyId} during rollback", licenseKeyId);
        }
    }

    private async Task ReleaseSeatAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogWarning("Rollback: releasing seat for user {UserId} org {OrganizationId}", userId, organizationId);
            await _proxyService.ProxyToOrganizationServiceAsync(
                endpoint: "/internal/licenses/seats/release",
                httpMethod: HttpMethod.Post,
                requestBody: new { OrganizationId = organizationId, UserId = userId, UserEmail = (string?)null },
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to release seat during rollback for user {UserId}", userId);
        }
    }

    private async Task TrySendWelcomeEmailAsync(
        SignupRequestDto requestDto,
        CommercialProvisioningResult commercial,
        CancellationToken cancellationToken)
    {
        try
        {
            var displayName = $"{requestDto.AdminUser.FirstName} {requestDto.AdminUser.LastName}".Trim();
            var loginUrl = BuildLoginUrl();
            var docsUrl = _experienceOptions.DocsBaseUrl
                          ?? _configuration["OnboardingExperience:DocsBaseUrl"];
            var support = _experienceOptions.SupportEmail
                          ?? _configuration["OnboardingExperience:SupportEmail"];

            var (subject, html) = commercial.LicenseKeyId.HasValue
                ? IdentityEmailTemplates.WelcomeWithTrial(
                    displayName,
                    requestDto.Organization.Name,
                    _trialOptions.ValidDays <= 0 ? 30 : _trialOptions.ValidDays,
                    commercial.MaxSeats ?? _trialOptions.MaxSeats,
                    commercial.ExpiresAtUtc,
                    loginUrl,
                    docsUrl,
                    support)
                : IdentityEmailTemplates.Welcome(displayName);

            await _emailService.SendAsync(requestDto.AdminUser.Email, subject, html);

            if (_experienceOptions.SendGettingStartedEmail)
            {
                var gs = IdentityEmailTemplates.GettingStarted(
                    displayName,
                    requestDto.Organization.Name,
                    loginUrl,
                    docsUrl,
                    support);
                await _emailService.SendAsync(requestDto.AdminUser.Email, gs.Subject, gs.Html);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Welcome email failed for {Email}; continuing without rollback",
                requestDto.AdminUser.Email);
        }
    }

    private string BuildLoginUrl()
    {
        var baseUrl = _experienceOptions.PublicAppBaseUrl
                      ?? _configuration["Identity:PublicBaseUrl"]
                      ?? _configuration["OnboardingExperience:PublicAppBaseUrl"]
                      ?? "https://localhost:4200";
        var path = string.IsNullOrWhiteSpace(_experienceOptions.LoginPath)
            ? "/login"
            : _experienceOptions.LoginPath!;
        return $"{baseUrl.TrimEnd('/')}/{path.TrimStart('/')}";
    }

    private async Task RunRollbackAsync(
        Stack<Func<CancellationToken, Task>> rollbackActions,
        SignupCommand request,
        CancellationToken cancellationToken)
    {
        while (rollbackActions.Any())
        {
            var rollback = rollbackActions.Pop();
            try
            {
                await rollback(cancellationToken);
            }
            catch (Exception rollbackEx)
            {
                _logger.LogError(
                    rollbackEx,
                    "Rollback action failed during onboarding cleanup for organization {OrganizationName}",
                    request.Request.Organization.Name);
            }
        }
    }

    private async Task<Guid> CreateOrganizationAsync(
        SignupRequestDto requestDto,
        Stack<Func<CancellationToken, Task>> rollbackActions,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Step 1: Creating organization {OrganizationName}", requestDto.Organization.Name);

        var orgRequest = new
        {
            Name = requestDto.Organization.Name,
            Type = requestDto.Organization.Type,
            Address = requestDto.Organization.Address
        };

        var orgResult = await _proxyService.ProxyToOrganizationServiceAsync(
            endpoint: "/internal/organizations",
            httpMethod: HttpMethod.Post,
            requestBody: orgRequest,
            cancellationToken: cancellationToken);

        if (orgResult is not ContentResult orgContentResult)
        {
            throw new OnboardingStepException(
                "CreateOrganization",
                statusCode: 0,
                responseCode: null,
                responseBody: null,
                message: "Failed to create organization: Unexpected response type.");
        }

        var statusCode = orgContentResult.StatusCode ?? 0;
        var body = orgContentResult.Content ?? string.Empty;

        if (statusCode == (int)HttpStatusCode.Conflict)
        {
            _logger.LogWarning("Organization already exists: {OrganizationName}", requestDto.Organization.Name);

            throw new OnboardingStepException(
                "CreateOrganization",
                statusCode,
                responseCode: "RESOURCE_CONFLICT",
                responseBody: body,
                message: "Organization with this name already exists.");
        }

        if (string.IsNullOrWhiteSpace(body))
        {
            throw new OnboardingStepException(
                "CreateOrganization",
                statusCode,
                responseCode: null,
                responseBody: body,
                message: $"Failed to create organization: Empty response. Status {statusCode}.");
        }

        JsonElement root;
        try
        {
            root = JsonSerializer.Deserialize<JsonElement>(body, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            throw new OnboardingStepException(
                "CreateOrganization",
                statusCode,
                responseCode: null,
                responseBody: body,
                message: $"Failed to parse organization response JSON. Status {statusCode}. Error: {ex.Message}");
        }

        bool success = true;
        string? code = null;
        JsonElement dataElement = root;

        if (root.ValueKind == JsonValueKind.Object &&
            root.TryGetProperty("success", out var successProp) &&
            (successProp.ValueKind == JsonValueKind.True || successProp.ValueKind == JsonValueKind.False))
        {
            success = successProp.GetBoolean();
            if (root.TryGetProperty("code", out var codeProp) && codeProp.ValueKind == JsonValueKind.String)
            {
                code = codeProp.GetString();
            }

            if (root.TryGetProperty("data", out var d) && d.ValueKind == JsonValueKind.Object)
            {
                dataElement = d;
            }
        }

        if (!success || statusCode >= (int)HttpStatusCode.BadRequest)
        {
            throw new OnboardingStepException(
                "CreateOrganization",
                statusCode,
                responseCode: code,
                responseBody: body,
                message: $"Failed to create organization. Status={statusCode}, Code={code ?? "UNKNOWN"}");
        }

        Guid organizationId;
        try
        {
            organizationId = dataElement.GetProperty("id").GetGuid();
        }
        catch (Exception ex)
        {
            throw new OnboardingStepException(
                "CreateOrganization",
                statusCode,
                responseCode: code,
                responseBody: body,
                message: $"Failed to extract organization id from response. Error={ex.Message}");
        }

        _logger.LogInformation("Organization created with ID: {OrganizationId}", organizationId);

        rollbackActions.Push(ct => DeleteOrganizationAsync(organizationId, ct));

        return organizationId;
    }

    private async Task<Guid> CreateHospitalAsync(
        Guid organizationId,
        SignupRequestDto requestDto,
        Stack<Func<CancellationToken, Task>> rollbackActions,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Step 2: Creating hospital {HospitalName} under organization {OrganizationId}",
            requestDto.Hospital.Name, organizationId);

        var hospitalRequest = new
        {
            OrganizationId = organizationId,
            Name = requestDto.Hospital.Name,
            Code = requestDto.Hospital.Code,
            Address = requestDto.Hospital.Address,
            Type = requestDto.Hospital.Type,
            BedCapacity = requestDto.Hospital.BedCapacity,
            CountryCode = requestDto.Hospital.CountryCode,
            StateCode = requestDto.Hospital.StateCode,
            IsOnboarded = requestDto.Hospital.IsOnboarded,
            NABHLevel = requestDto.Hospital.NABHLevel
        };

        var hospitalResult = await _proxyService.ProxyToOrganizationServiceAsync(
            endpoint: "/internal/hospitals",
            httpMethod: HttpMethod.Post,
            requestBody: hospitalRequest,
            cancellationToken: cancellationToken);

        if (hospitalResult is not ContentResult hospitalContentResult)
        {
            throw new OnboardingStepException(
                "CreateHospital",
                statusCode: 0,
                responseCode: null,
                responseBody: null,
                message: "Failed to create hospital: Unexpected response type.");
        }

        var statusCode = hospitalContentResult.StatusCode ?? 0;
        var body = hospitalContentResult.Content ?? string.Empty;

        if (string.IsNullOrWhiteSpace(body) || statusCode >= (int)HttpStatusCode.BadRequest)
        {
            throw new OnboardingStepException(
                "CreateHospital",
                statusCode,
                responseCode: null,
                responseBody: body,
                message: $"Failed to create hospital: Status {statusCode}");
        }

        JsonElement root;
        try
        {
            root = JsonSerializer.Deserialize<JsonElement>(body, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            throw new OnboardingStepException(
                "CreateHospital",
                statusCode,
                responseCode: null,
                responseBody: body,
                message: $"Failed to parse hospital response JSON. Status {statusCode}. Error: {ex.Message}");
        }

        bool success = true;
        string? code = null;
        JsonElement dataElement = root;

        if (root.ValueKind == JsonValueKind.Object &&
            root.TryGetProperty("success", out var successProp) &&
            (successProp.ValueKind == JsonValueKind.True || successProp.ValueKind == JsonValueKind.False))
        {
            success = successProp.GetBoolean();
            if (root.TryGetProperty("code", out var codeProp) && codeProp.ValueKind == JsonValueKind.String)
            {
                code = codeProp.GetString();
            }

            if (root.TryGetProperty("data", out var d) && d.ValueKind == JsonValueKind.Object)
            {
                dataElement = d;
            }
        }

        if (!success || statusCode >= (int)HttpStatusCode.BadRequest)
        {
            throw new OnboardingStepException(
                "CreateHospital",
                statusCode,
                responseCode: code,
                responseBody: body,
                message: $"Failed to create hospital. Status={statusCode}, Code={code ?? "UNKNOWN"}");
        }

        Guid hospitalId;
        try
        {
            hospitalId = dataElement.GetProperty("id").GetGuid();
        }
        catch (Exception ex)
        {
            throw new OnboardingStepException(
                "CreateHospital",
                statusCode,
                responseCode: code,
                responseBody: body,
                message: $"Failed to extract hospital id from response. Error={ex.Message}");
        }

        _logger.LogInformation("Hospital created with ID: {HospitalId}", hospitalId);

        rollbackActions.Push(ct => DeleteHospitalAsync(hospitalId, ct));

        return hospitalId;
    }

    private async Task<Guid> CreateUserAsync(
        SignupRequestDto requestDto,
        Guid organizationId,
        Guid hospitalId,
        Stack<Func<CancellationToken, Task>> rollbackActions,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Step 3: Creating admin user {Email}", requestDto.AdminUser.Email);

        var userRequest = new
        {
            Email = requestDto.AdminUser.Email,
            Password = requestDto.AdminUser.Password,
            Role = "ORG_ADMIN"
        };

        var userResult = await _proxyService.ProxyRequestAsync(
            clientName: "IdentityClient",
            endpoint: "/internal/users/register",
            httpMethod: HttpMethod.Post,
            requestBody: userRequest,
            forwardAuthHeader: false,
            cancellationToken: cancellationToken);

        if (userResult is not ContentResult userContentResult)
        {
            throw new OnboardingStepException(
                "CreateUser",
                statusCode: 0,
                responseCode: null,
                responseBody: null,
                message: "Failed to create user: Unexpected response type.");
        }

        var statusCode = userContentResult.StatusCode ?? 0;
        var body = userContentResult.Content ?? string.Empty;

        if (statusCode == (int)HttpStatusCode.Conflict)
        {
            _logger.LogWarning("User already exists: {Email}", requestDto.AdminUser.Email);

            throw new OnboardingStepException(
                "CreateUser",
                statusCode,
                responseCode: "RESOURCE_CONFLICT",
                responseBody: body,
                message: "User with this email already exists.");
        }

        if (string.IsNullOrWhiteSpace(body) || statusCode >= (int)HttpStatusCode.BadRequest)
        {
            throw new OnboardingStepException(
                "CreateUser",
                statusCode,
                responseCode: null,
                responseBody: body,
                message: $"Failed to create user. Status={statusCode}");
        }

        JsonElement root;
        try
        {
            root = JsonSerializer.Deserialize<JsonElement>(body, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            throw new OnboardingStepException(
                "CreateUser",
                statusCode,
                responseCode: null,
                responseBody: body,
                message: $"Failed to parse user response JSON. Status {statusCode}. Error: {ex.Message}");
        }

        Guid userId;
        try
        {
            if (root.ValueKind == JsonValueKind.Object &&
                root.TryGetProperty("userId", out var userIdProp) &&
                userIdProp.ValueKind == JsonValueKind.String)
            {
                userId = Guid.Parse(userIdProp.GetString()!);
            }
            else
            {
                userId = root.GetProperty("userId").GetGuid();
            }
        }
        catch (Exception ex)
        {
            throw new OnboardingStepException(
                "CreateUser",
                statusCode,
                responseCode: null,
                responseBody: body,
                message: $"Failed to extract user id from response. Error={ex.Message}");
        }

        _logger.LogInformation("User created with ID: {UserId}", userId);

        rollbackActions.Push(ct => DeleteUserAsync(userId, ct));

        return userId;
    }

    private async Task CreateOrganizationMemberAsync(
        Guid organizationId,
        Guid userId,
        SignupRequestDto requestDto,
        Stack<Func<CancellationToken, Task>> rollbackActions,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Step 4: Associating user {UserId} with organization {OrganizationId}",
            userId, organizationId);

        var memberRequest = new
        {
            OrganizationId = organizationId,
            UserId = userId,
            Role = "ORG_ADMIN",
            FirstName = requestDto.AdminUser.FirstName,
            LastName = requestDto.AdminUser.LastName,
            Email = requestDto.AdminUser.Email,
            PhoneNumber = requestDto.AdminUser.PhoneNumber,
            AlternatePhoneNumber = requestDto.AdminUser.AlternatePhoneNumber,
            ProfilePicUrl = requestDto.AdminUser.ProfilePicUrl,
            Preferences = requestDto.AdminUser.Preferences
        };

        var memberResult = await _proxyService.ProxyToOrganizationServiceAsync(
            endpoint: "/internal/organization-members",
            httpMethod: HttpMethod.Post,
            requestBody: memberRequest,
            cancellationToken: cancellationToken);

        if (memberResult is not ContentResult memberContentResult ||
            memberContentResult.StatusCode != (int)HttpStatusCode.OK)
        {
            var status = memberResult is ContentResult cr ? (cr.StatusCode ?? 0) : 0;
            var body = memberResult is ContentResult cr2 ? (cr2.Content ?? string.Empty) : string.Empty;

            throw new OnboardingStepException(
                "CreateOrganizationMember",
                status,
                responseCode: null,
                responseBody: body,
                message: "Failed to associate user with organization.");
        }

        _logger.LogInformation("User associated with organization successfully");
    }

    private async Task<(string token, string refreshToken, int expiresIn)> GenerateTokenAsync(
        Guid userId,
        string email,
        Guid organizationId,
        Guid hospitalId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Step 5: Generating JWT token for user {UserId}", userId);

        var tokenRequest = new
        {
            UserId = userId.ToString(),
            Email = email,
            Role = "ORG_ADMIN",
            OrganizationId = organizationId,
            HospitalId = hospitalId
        };

        var tokenResult = await _proxyService.ProxyRequestAsync(
            clientName: "IdentityClient",
            endpoint: "/internal/token/generate",
            httpMethod: HttpMethod.Post,
            requestBody: tokenRequest,
            forwardAuthHeader: false,
            cancellationToken: cancellationToken);

        if (tokenResult is not ContentResult tokenContentResult ||
            tokenContentResult.StatusCode != (int)HttpStatusCode.OK ||
            string.IsNullOrEmpty(tokenContentResult.Content))
        {
            var status = (tokenResult as ContentResult)?.StatusCode ?? 0;
            var body = (tokenResult as ContentResult)?.Content ?? string.Empty;

            throw new OnboardingStepException(
                "GenerateToken",
                status,
                responseCode: null,
                responseBody: body,
                message: "Failed to generate authentication token.");
        }

        JsonElement root;
        try
        {
            root = JsonSerializer.Deserialize<JsonElement>(tokenContentResult.Content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            throw new OnboardingStepException(
                "GenerateToken",
                tokenContentResult.StatusCode ?? 0,
                responseCode: null,
                responseBody: tokenContentResult.Content,
                message: $"Failed to parse token response JSON. Error={ex.Message}");
        }

        try
        {
            var token = root.GetProperty("token").GetString()!;
            var refreshToken = root.GetProperty("refreshToken").GetString()!;
            var expiresIn = root.GetProperty("expiresIn").GetInt32();

            return (token, refreshToken, expiresIn);
        }
        catch (Exception ex)
        {
            throw new OnboardingStepException(
                "GenerateToken",
                tokenContentResult.StatusCode ?? 0,
                responseCode: null,
                responseBody: tokenContentResult.Content,
                message: $"Failed to extract token fields from response. Error={ex.Message}");
        }
    }

    private async Task DeleteOrganizationAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogWarning("Attempting to rollback organization {OrganizationId}", organizationId);
            await _proxyService.ProxyToOrganizationServiceAsync(
                endpoint: $"/internal/organizations/{organizationId}",
                httpMethod: HttpMethod.Delete,
                requestBody: null,
                cancellationToken: cancellationToken);
            _logger.LogInformation("Organization {OrganizationId} rolled back successfully", organizationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to rollback organization {OrganizationId}", organizationId);
        }
    }

    private async Task DeleteHospitalAsync(Guid hospitalId, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogWarning("Attempting to rollback hospital {HospitalId}", hospitalId);
            await _proxyService.ProxyToOrganizationServiceAsync(
                endpoint: $"/internal/hospitals/{hospitalId}",
                httpMethod: HttpMethod.Delete,
                requestBody: null,
                cancellationToken: cancellationToken);
            _logger.LogInformation("Hospital {HospitalId} rolled back successfully", hospitalId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to rollback hospital {HospitalId}", hospitalId);
        }
    }

    private async Task DeleteUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogWarning("Attempting to rollback user {UserId}", userId);
            await _proxyService.ProxyRequestAsync(
                clientName: "IdentityClient",
                endpoint: $"/internal/users/{userId}",
                httpMethod: HttpMethod.Delete,
                requestBody: null,
                forwardAuthHeader: false,
                cancellationToken: cancellationToken);
            _logger.LogInformation("User {UserId} rolled back successfully", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to rollback user {UserId}", userId);
        }
    }

    private void EnsureEmailVerificationAllowsAnonymousSignup()
    {
        var modeText = _configuration["Identity:EmailVerificationMode"];
        EmailVerificationMode mode = EmailVerificationMode.Async;
        if (!string.IsNullOrWhiteSpace(modeText)
            && Enum.TryParse(modeText, ignoreCase: true, out EmailVerificationMode parsed))
        {
            mode = parsed;
        }
        else if (_configuration.GetValue<bool?>("Identity:RequireEmailVerification") == true)
        {
            mode = EmailVerificationMode.RequiredBeforeLogin;
        }

        if (mode != EmailVerificationMode.RequiredBeforeOrganizationProvisioning)
            return;

        var http = _httpContextAccessor.HttpContext;
        var authenticated = http?.User?.Identity?.IsAuthenticated == true;
        var emailConfirmedClaim = http?.User?.FindFirst("email_verified")?.Value
            ?? http?.User?.FindFirst("EmailConfirmed")?.Value;
        var confirmed = string.Equals(emailConfirmedClaim, "true", StringComparison.OrdinalIgnoreCase);

        if (authenticated && confirmed)
            return;

        throw new InvalidOperationException(
            "Email verification is required before organization provisioning. Register and verify your email, then sign in to continue.");
    }

    private async Task TryAssignDefaultTrialSubscriptionAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        var planIdValue = _trialOptions.DefaultTrialPlanId?.ToString()
                          ?? _configuration["Subscriptions:DefaultTrialPlanId"];
        if (string.IsNullOrWhiteSpace(planIdValue) || !Guid.TryParse(planIdValue, out var planId))
        {
            _logger.LogDebug("No default trial subscription plan configured. Skipping automatic subscription assignment.");
            return;
        }

        try
        {
            _logger.LogInformation("Assigning default trial subscription plan {PlanId} to organization {OrganizationId}", planId, organizationId);

            var result = await _proxyService.ProxyToOrganizationServiceAsync(
                endpoint: "/internal/subscriptions",
                httpMethod: HttpMethod.Post,
                requestBody: new
                {
                    OrganizationId = organizationId,
                    SubscriptionPlanId = planId,
                    IsTrial = true
                },
                cancellationToken: cancellationToken);

            if (result is not ContentResult contentResult || contentResult.StatusCode is null || contentResult.StatusCode >= (int)HttpStatusCode.BadRequest)
            {
                _logger.LogWarning("Failed to assign default trial subscription to organization {OrganizationId}. Status: {Status}, Response: {Response}",
                    organizationId,
                    (result as ContentResult)?.StatusCode,
                    (result as ContentResult)?.Content);
            }
            else
            {
                _logger.LogInformation("Default trial subscription assigned to organization {OrganizationId}", organizationId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning default trial subscription to organization {OrganizationId}", organizationId);
        }
    }
}
