using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using Gateway.Application.Commands;
using Gateway.Configuration;
using Gateway.DTOs;
using Gateway.Services;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Shared.Infrastructure.DTO;

namespace Gateway.Application.Handlers;

/// <summary>
/// Orchestrates second-step onboarding: Organization wizard profile, Compliance assessment + frameworks, Identity invites.
/// </summary>
public sealed class OnboardWizardCommandHandler : IRequestHandler<OnboardWizardCommand, OnboardWizardResponseDto>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IProxyService _proxyService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IOptions<OnboardingWizardOptions> _wizardOptions;
    private readonly ILogger<OnboardWizardCommandHandler> _logger;

    public OnboardWizardCommandHandler(
        IProxyService proxyService,
        IHttpContextAccessor httpContextAccessor,
        IOptions<OnboardingWizardOptions> wizardOptions,
        ILogger<OnboardWizardCommandHandler> logger)
    {
        _proxyService = proxyService;
        _httpContextAccessor = httpContextAccessor;
        _wizardOptions = wizardOptions;
        _logger = logger;
    }

    public async Task<OnboardWizardResponseDto> Handle(OnboardWizardCommand command, CancellationToken cancellationToken)
    {
        var req = command.Request;

        EnsureClaimsMatchBody(req);

        _logger.LogInformation(
            "OnboardingWizard:Start organization={OrganizationId} hospital={HospitalId} frameworks={FrameworkCount} invites={InviteCount}",
            req.OrganizationId,
            req.HospitalId,
            req.FrameworkCodes.Count,
            req.TeamInvites.Count);

        await ApplyHospitalWizardProfileAsync(req, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation(
            "OnboardingWizard:Profile organization={OrganizationId} hospital={HospitalId}",
            req.OrganizationId,
            req.HospitalId);

        await SaveComplianceWizardAssessmentAsync(req, cancellationToken).ConfigureAwait(false);
        var kbAssessment = await SaveKbAssessmentsAsync(req, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation(
            "OnboardingWizard:Assessment organization={OrganizationId} hospital={HospitalId} assessmentId={AssessmentId}",
            req.OrganizationId,
            req.HospitalId,
            kbAssessment?.PrimaryAssessmentId);

        var codesDone = await EnrollFrameworksAndCreateJourneysAsync(req, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation(
            "OnboardingWizard:Frameworks organization={OrganizationId} hospital={HospitalId} codes={Codes}",
            req.OrganizationId,
            req.HospitalId,
            string.Join(',', codesDone));

        var invitedIds = await InviteTeamMembersAsync(req, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation(
            "OnboardingWizard:Invites organization={OrganizationId} hospital={HospitalId} count={Count}",
            req.OrganizationId,
            req.HospitalId,
            invitedIds.Count);

        return new OnboardWizardResponseDto
        {
            OrganizationId = req.OrganizationId,
            HospitalId = req.HospitalId,
            FrameworkCodesCompleted = codesDone,
            AssessmentId = kbAssessment?.PrimaryAssessmentId,
            AssessmentIds = kbAssessment?.AssessmentIds,
            AssessmentSummary = kbAssessment?.PrimarySummary,
            InvitedUserIds = invitedIds
        };
    }

    private sealed class KbAssessmentSaveResult
    {
        public Guid? PrimaryAssessmentId { get; init; }
        public OnboardingAssessmentSummaryDto? PrimarySummary { get; init; }
        public IReadOnlyList<Guid> AssessmentIds { get; init; } = Array.Empty<Guid>();
    }

    private sealed class KbFrameworkListItem
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
    }

    private sealed class KbSubmitWizardAssessmentResponse
    {
        public Guid AssessmentId { get; set; }
        public OnboardingAssessmentSummaryDto? Summary { get; set; }
    }

    private sealed class KbSubmitCoreWizardAssessmentResponse
    {
        public Guid AssessmentId { get; set; }
        public decimal CoreReadinessScore { get; set; }
        public string Status { get; set; } = string.Empty;
        public int CriticalFailureCount { get; set; }
        public bool CanUnlockCommitment { get; set; }
        public List<string>? HighRiskAreas { get; set; }
    }

    private void EnsureClaimsMatchBody(OnboardWizardRequestDto req)
    {
        var http = _httpContextAccessor.HttpContext;
        if (http?.User?.Identity?.IsAuthenticated != true)
        {
            throw new UnauthorizedAccessException("Authentication required.");
        }

        var orgClaim = http.User.FindFirst("organization_id")?.Value;
        var hospClaim = http.User.FindFirst("hospital_id")?.Value;

        if (string.IsNullOrEmpty(orgClaim) || !Guid.TryParse(orgClaim, out var tokenOrg) || tokenOrg != req.OrganizationId)
        {
            throw new InvalidOperationException("HOSPITAL_CLAIM_INVALID: organization_id claim does not match request.");
        }

        if (string.IsNullOrEmpty(hospClaim) || !Guid.TryParse(hospClaim, out var tokenHosp) || tokenHosp != req.HospitalId)
        {
            throw new InvalidOperationException("HOSPITAL_CLAIM_INVALID: hospital_id claim does not match request.");
        }

        if (req.HospitalProfile.HospitalId != Guid.Empty && req.HospitalProfile.HospitalId != req.HospitalId)
        {
            throw new InvalidOperationException("HospitalProfile.hospitalId must match hospitalId.");
        }
    }

    private async Task ApplyHospitalWizardProfileAsync(OnboardWizardRequestDto req, CancellationToken cancellationToken)
    {
        var body = new
        {
            organizationId = req.OrganizationId,
            hospitalType = req.HospitalProfile.HospitalType,
            bedCapacityRange = req.HospitalProfile.BedCapacity,
            countryCode = req.HospitalProfile.CountryCode,
            stateCode = req.HospitalProfile.StateCode,
            markOnboarded = true,
            wizardVersion = req.WizardVersion,
            departmentMasterIds = req.HospitalProfile.DepartmentIds
        };

        var result = await _proxyService.ProxyToOrganizationServiceAsync(
            $"/internal/hospitals/{req.HospitalId}/wizard-profile",
            HttpMethod.Put,
            body,
            cancellationToken).ConfigureAwait(false);

        ThrowIfProxyFailure(result, "ApplyHospitalWizardProfile");
    }

    private async Task SaveComplianceWizardAssessmentAsync(OnboardWizardRequestDto req, CancellationToken cancellationToken)
    {
        if (req.AssessmentAnswers.Count == 0)
        {
            return;
        }

        var body = new
        {
            organizationId = req.OrganizationId,
            hospitalId = req.HospitalId,
            wizardVersion = req.WizardVersion,
            assessmentAnswers = req.AssessmentAnswers.Select(a => new
            {
                questionId = a.QuestionId,
                question = a.Question,
                answer = a.Answer,
                objectiveElementCode = a.ObjectiveElementCode
            }).ToList()
        };

        var result = await _proxyService.ProxyToComplianceServiceAsync(
            "/api/v1/onboarding/wizard/assessments",
            HttpMethod.Post,
            body,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        ThrowIfProxyFailure(result, "SaveComplianceWizardAssessment");
    }

    private async Task<KbAssessmentSaveResult?> SaveKbAssessmentsAsync(
        OnboardWizardRequestDto req,
        CancellationToken cancellationToken)
    {
        if (req.AssessmentAnswers.Count == 0)
        {
            return null;
        }

        var codes = req.FrameworkCodes
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Select(c => c.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (codes.Count == 0)
        {
            return null;
        }

        var frameworksResult = await _proxyService.ProxyToKnowledgeBaseServiceAsync(
            "/api/v1.0/frameworks",
            HttpMethod.Get,
            null,
            null,
            cancellationToken).ConfigureAwait(false);

        ThrowIfProxyFailure(frameworksResult, "GetKbFrameworks");

        if (frameworksResult is not ContentResult fcr || string.IsNullOrEmpty(fcr.Content))
        {
            throw new InvalidOperationException("Failed to load knowledge base frameworks.");
        }

        var fwEnvelope = JsonSerializer.Deserialize<ApiResponse<List<KbFrameworkListItem>>>(fcr.Content, JsonOptions);
        var list = fwEnvelope?.Data ?? new List<KbFrameworkListItem>();
        var codeSet = new HashSet<string>(codes, StringComparer.OrdinalIgnoreCase);
        var matched = list
            .Where(f => !string.IsNullOrWhiteSpace(f.Code) && codeSet.Contains(f.Code.Trim()))
            .ToList();

        if (matched.Count == 0)
        {
            _logger.LogWarning(
                "OnboardingWizard:KbAssessment no KB frameworks matched codes={Codes}",
                string.Join(',', codes));
            return null;
        }

        // Map client answers (questionId + answer) to Core wizard format.
        // The client now sends compliant/partially_compliant/non_compliant/not_applicable.
        // ObjectiveElementId is parsed from QuestionId; Weight/IsMandatory/IsCritical are
        // defaulted — the KB already has the authoritative values but the submission still
        // carries them so the scoring formula is self-contained.
        var coreAnswers = req.AssessmentAnswers
            .Where(a => Guid.TryParse(a.QuestionId, out _))
            .Select(a => new
            {
                wizardQuestionId    = Guid.Empty,          // not needed by KB submission path
                objectiveElementId  = Guid.Parse(a.QuestionId),
                answer              = a.Answer,
                weight              = 1m,                  // KB uses its own stored weight
                isMandatory         = false,               // KB uses its own stored flag
                isCritical          = false
            }).ToList();

        var assessmentIds = new List<Guid>();
        OnboardingAssessmentSummaryDto? primarySummary = null;
        Guid? primaryId = null;

        foreach (var framework in matched)
        {
            var body = new
            {
                facilityId  = req.HospitalId,
                frameworkId = framework.Id,
                assessedBy  = req.HospitalId.ToString(),
                answers     = coreAnswers
            };

            var result = await _proxyService.ProxyToKnowledgeBaseServiceAsync(
                "/api/v1.0/assessments/from-core-wizard",
                HttpMethod.Post,
                body,
                null,
                cancellationToken).ConfigureAwait(false);

            ThrowIfProxyFailure(result, "SaveKbCoreWizardAssessment");

            if (result is not ContentResult cr || string.IsNullOrEmpty(cr.Content))
            {
                continue;
            }

            var envelope = JsonSerializer.Deserialize<ApiResponse<KbSubmitCoreWizardAssessmentResponse>>(cr.Content, JsonOptions);
            if (envelope?.Data == null)
            {
                continue;
            }

            assessmentIds.Add(envelope.Data.AssessmentId);
            if (primaryId is null)
            {
                primaryId = envelope.Data.AssessmentId;
                primarySummary = new OnboardingAssessmentSummaryDto
                {
                    CoreReadinessScore   = envelope.Data.CoreReadinessScore,
                    CoreStatus           = envelope.Data.Status,
                    CriticalFailures     = envelope.Data.CriticalFailureCount,
                    CanUnlockCommitment  = envelope.Data.CanUnlockCommitment
                };
            }
        }

        return new KbAssessmentSaveResult
        {
            PrimaryAssessmentId = primaryId,
            PrimarySummary = primarySummary,
            AssessmentIds = assessmentIds
        };
    }

    private async Task<List<string>> EnrollFrameworksAndCreateJourneysAsync(OnboardWizardRequestDto req, CancellationToken cancellationToken)
    {
        var codes = req.FrameworkCodes.Where(c => !string.IsNullOrWhiteSpace(c)).Select(c => c.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (codes.Count == 0)
        {
            return new List<string>();
        }

        var frameworksResult = await _proxyService.ProxyToComplianceServiceAsync(
            "/api/v1/frameworks",
            HttpMethod.Get,
            null,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        ThrowIfProxyFailure(frameworksResult, "GetFrameworks");

        if (frameworksResult is not ContentResult fcr || string.IsNullOrEmpty(fcr.Content))
        {
            throw new InvalidOperationException("Failed to load compliance frameworks.");
        }

        var fwEnvelope = JsonSerializer.Deserialize<ApiResponse<List<Shared.Infrastructure.DTO.FrameworkDto>>>(fcr.Content, JsonOptions);
        var list = fwEnvelope?.Data;
        if (list == null || list.Count == 0)
        {
            throw new InvalidOperationException("No compliance frameworks available.");
        }

        var byCode = list
            .Where(f => !string.IsNullOrEmpty(f.Code))
            .GroupBy(f => f.Code.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var completed = new List<string>();
        foreach (var code in codes)
        {
            if (!byCode.TryGetValue(code, out var fw))
            {
                throw new InvalidOperationException($"Unknown framework code: {code}");
            }

            var enrollResult = await _proxyService.ProxyToComplianceServiceAsync(
                $"/api/v1/hospitals/{req.HospitalId}/frameworks/{fw.FrameworkId}/enroll",
                HttpMethod.Post,
                null,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            var enrollStatus = GetStatusCode(enrollResult);
            if (enrollStatus is not (201 or 200 or 409))
            {
                ThrowFromResult(enrollResult, "EnrollFramework");
            }

            if (req.HospitalId == Guid.Empty || fw.FrameworkId == Guid.Empty)
            {
                _logger.LogWarning(
                    "OnboardingWizard:CreateJourney skipped empty id hospitalId={HospitalId} frameworkId={FrameworkId} frameworkCode={FrameworkCode}",
                    req.HospitalId,
                    fw.FrameworkId,
                    code);
                throw new InvalidOperationException(
                    "Cannot create compliance journey: HospitalId or FrameworkId is empty. Check wizard request and framework list mapping.");
            }

            _logger.LogInformation(
                "OnboardingWizard:CreateJourney POST compliance hospitalId={HospitalId} frameworkId={FrameworkId} frameworkCode={FrameworkCode}",
                req.HospitalId,
                fw.FrameworkId,
                code);

            var journeyResult = await _proxyService.ProxyToComplianceServiceAsync(
                $"/api/v1/hospitals/{req.HospitalId}/frameworks/{fw.FrameworkId}/journeys",
                HttpMethod.Post,
                null,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            var jStatus = GetStatusCode(journeyResult);
            if (jStatus < 200 || jStatus >= 300)
            {
                ThrowFromResult(journeyResult, "CreateJourney");
            }

            await ApplyOnboardingAssessmentToObligationsAsync(req, fw.FrameworkId, cancellationToken)
                .ConfigureAwait(false);

            completed.Add(code);
        }

        return completed;
    }

    private async Task ApplyOnboardingAssessmentToObligationsAsync(
        OnboardWizardRequestDto req,
        Guid frameworkId,
        CancellationToken cancellationToken)
    {
        if (req.AssessmentAnswers.Count == 0 || frameworkId == Guid.Empty)
        {
            return;
        }

        var body = new
        {
            organizationId = req.OrganizationId,
            hospitalId = req.HospitalId,
            frameworkId,
            assessmentAnswers = req.AssessmentAnswers.Select(a => new
            {
                questionId = a.QuestionId,
                question = a.Question,
                answer = a.Answer,
                objectiveElementCode = a.ObjectiveElementCode,
            }).ToList(),
        };

        var result = await _proxyService.ProxyToComplianceServiceAsync(
            "/internal/v1/obligations/apply-onboarding-assessment",
            HttpMethod.Post,
            body,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        var status = GetStatusCode(result);
        if (status >= 400)
        {
            _logger.LogWarning(
                "OnboardingWizard:ApplyObligationAssessment failed for HospitalId={HospitalId} FrameworkId={FrameworkId} HTTP {Status}",
                req.HospitalId, frameworkId, status);
        }
    }

    private async Task<List<Guid>> InviteTeamMembersAsync(
        OnboardWizardRequestDto req,
        CancellationToken cancellationToken)
    {
        if (_wizardOptions.Value.UseEmailInvites)
        {
            return await InviteTeamMembersViaEmailAsync(req, cancellationToken).ConfigureAwait(false);
        }

        var ids = new List<Guid>();
        foreach (var invite in req.TeamInvites)
        {
            if (string.IsNullOrWhiteSpace(invite.Email))
            {
                continue;
            }

            var password = GenerateInvitePassword();
            SplitName(invite.Name, out var firstName, out var lastName);

            var registerBody = new
            {
                email = invite.Email.Trim(),
                password,
                role = invite.Role
            };

            var regResult = await _proxyService.ProxyToIdentityServiceAsync(
                "/internal/users/register",
                HttpMethod.Post,
                registerBody,
                cancellationToken).ConfigureAwait(false);

            var regStatus = GetStatusCode(regResult);
            if (regStatus == (int)HttpStatusCode.Conflict)
            {
                throw new InvalidOperationException($"User already exists for email {invite.Email}.");
            }

            if (regStatus < 200 || regStatus >= 300)
            {
                ThrowFromResult(regResult, "RegisterInviteUser");
            }

            if (regResult is not ContentResult rcr || string.IsNullOrEmpty(rcr.Content))
            {
                throw new InvalidOperationException("Identity register returned empty body.");
            }

            Guid userId;
            using (var doc = JsonDocument.Parse(rcr.Content))
            {
                var root = doc.RootElement;
                if (!root.TryGetProperty("userId", out var uid))
                {
                    throw new InvalidOperationException("Identity register response missing userId.");
                }

                userId = uid.ValueKind == JsonValueKind.String
                    ? Guid.Parse(uid.GetString()!)
                    : Guid.Parse(uid.GetRawText().Trim('"'));
            }

            var memberBody = new
            {
                organizationId = req.OrganizationId,
                userId,
                departmentId = invite.DepartmentId,
                role = invite.Role,
                firstName,
                lastName,
                email = invite.Email.Trim()
            };

            var memResult = await _proxyService.ProxyToOrganizationServiceAsync(
                "/internal/organization-members",
                HttpMethod.Post,
                memberBody,
                cancellationToken).ConfigureAwait(false);

            try
            {
                ThrowIfProxyFailure(memResult, "CreateOrganizationMember");
            }
            catch
            {
                await DeleteUserAsync(userId, cancellationToken).ConfigureAwait(false);
                throw;
            }

            ids.Add(userId);
        }

        return ids;
    }

    /// <summary>CRP Phase 7 — email invitation path (no eager Identity register).</summary>
    private async Task<List<Guid>> InviteTeamMembersViaEmailAsync(
        OnboardWizardRequestDto req,
        CancellationToken cancellationToken)
    {
        var invitationIds = new List<Guid>();
        foreach (var invite in req.TeamInvites)
        {
            if (string.IsNullOrWhiteSpace(invite.Email))
            {
                continue;
            }

            SplitName(invite.Name, out _, out _);
            var body = new
            {
                organizationId = req.OrganizationId,
                hospitalId = req.HospitalId,
                departmentId = invite.DepartmentId,
                role = invite.Role,
                email = invite.Email.Trim(),
                designation = string.IsNullOrWhiteSpace(invite.Name) ? null : invite.Name.Trim(),
                employmentType = "Permanent",
                requireSeat = true
            };

            var result = await _proxyService.ProxyToOrganizationServiceAsync(
                "/internal/invitations",
                HttpMethod.Post,
                body,
                cancellationToken).ConfigureAwait(false);

            var status = GetStatusCode(result);
            if (status < 200 || status >= 300)
            {
                ThrowFromResult(result, "CreateTeamInvitation");
            }

            if (result is ContentResult cr && !string.IsNullOrEmpty(cr.Content))
            {
                try
                {
                    using var doc = JsonDocument.Parse(cr.Content);
                    var root = doc.RootElement;
                    var data = root.TryGetProperty("data", out var d) ? d
                        : root.TryGetProperty("Data", out var d2) ? d2
                        : root;
                    if (data.TryGetProperty("id", out var idProp)
                        || data.TryGetProperty("Id", out idProp))
                    {
                        var id = idProp.ValueKind == JsonValueKind.String
                            ? Guid.Parse(idProp.GetString()!)
                            : idProp.GetGuid();
                        invitationIds.Add(id);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not parse invitation id for {Email}", invite.Email);
                }
            }
        }

        _logger.LogInformation(
            "OnboardingWizard: created {Count} email invitations for org {Org}",
            invitationIds.Count,
            req.OrganizationId);
        return invitationIds;
    }

    private static string GenerateInvitePassword()
    {
        var bytes = new byte[24];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes) + "aA1!";
    }

    private static void SplitName(string? name, out string firstName, out string lastName)
    {
        firstName = (name ?? string.Empty).Trim();
        lastName = string.Empty;
        var i = firstName.IndexOf(' ', StringComparison.Ordinal);
        if (i > 0)
        {
            lastName = firstName[(i + 1)..].Trim();
            firstName = firstName[..i].Trim();
        }
    }

    private async Task DeleteUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        try
        {
            await _proxyService.ProxyToIdentityServiceAsync(
                $"/internal/users/{userId}",
                HttpMethod.Delete,
                null,
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Rollback: failed to delete user {UserId}", userId);
        }
    }

    private static int GetStatusCode(IActionResult result)
    {
        return result switch
        {
            ContentResult cr => cr.StatusCode ?? 200,
            ObjectResult or => or.StatusCode ?? 200,
            StatusCodeResult sr => sr.StatusCode,
            _ => 200
        };
    }

    private void ThrowIfProxyFailure(IActionResult result, string step)
    {
        var code = GetStatusCode(result);
        if (code >= 400)
        {
            ThrowFromResult(result, step);
        }
    }

    private void ThrowFromResult(IActionResult result, string step)
    {
        var code = GetStatusCode(result);
        var body = result is ContentResult cr ? cr.Content : string.Empty;
        _logger.LogWarning("Proxy step {Step} failed with HTTP {Status}: {Body}", step, code, body);
        throw new InvalidOperationException($"{step} failed with HTTP {code}.");
    }

}
