using System.Security.Claims;
using System.Text.Json;
using Gateway.Application.Queries;
using Gateway.Application.Services;
using Gateway.DTOs;
using Gateway.Services;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Application.Handlers;

public sealed class GetOnboardingJourneyQueryHandler : IRequestHandler<GetOnboardingJourneyQuery, OnboardingJourneyDto>
{
    private readonly IProxyService _proxy;
    private readonly IMediator _mediator;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<GetOnboardingJourneyQueryHandler> _logger;

    public GetOnboardingJourneyQueryHandler(
        IProxyService proxy,
        IMediator mediator,
        IHttpContextAccessor httpContextAccessor,
        ILogger<GetOnboardingJourneyQueryHandler> logger)
    {
        _proxy = proxy;
        _mediator = mediator;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<OnboardingJourneyDto> Handle(GetOnboardingJourneyQuery request, CancellationToken cancellationToken)
    {
        var http = _httpContextAccessor.HttpContext
                   ?? throw new UnauthorizedAccessException("No HTTP context.");
        var user = http.User;

        var orgId = ParseGuidClaim(user, "organization_id")
                    ?? ParseGuidClaim(user, "organizationId")
                    ?? throw new UnauthorizedAccessException("Missing organization_id claim.");
        var hospitalId = ParseGuidClaim(user, "hospital_id")
                         ?? ParseGuidClaim(user, "hospitalId");
        var role = user.FindFirst(ClaimTypes.Role)?.Value
                   ?? user.FindFirst("role")?.Value
                   ?? "ORG_ADMIN";
        var emailVerified = string.Equals(
                                user.FindFirst("email_verified")?.Value,
                                "true",
                                StringComparison.OrdinalIgnoreCase)
                            || string.Equals(
                                user.FindFirst("EmailConfirmed")?.Value,
                                "true",
                                StringComparison.OrdinalIgnoreCase);

        OrganizationDetailDto? orgDetail = null;
        try
        {
            orgDetail = await _mediator.Send(new GetOrganizationDetailQuery(orgId), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Organization detail unavailable for onboarding journey");
        }

        var hospital = hospitalId.HasValue
            ? orgDetail?.Hospitals?.FirstOrDefault(h => h.Id == hospitalId.Value)
            : orgDetail?.Hospitals?.FirstOrDefault();
        if (hospital == null && orgDetail?.Hospitals?.Count > 0)
        {
            hospital = orgDetail.Hospitals[0];
            hospitalId = hospital.Id;
        }

        var isOnboarded = hospital?.IsOnboarded == true;
        var hasFrameworks = hospital?.Journey?.Count > 0;
        var admin = orgDetail?.Members?
            .FirstOrDefault(m => string.Equals(m.Role, "ORG_ADMIN", StringComparison.OrdinalIgnoreCase))
            ?? orgDetail?.Members?.FirstOrDefault();

        var trial = await LoadTrialAsync(orgId, cancellationToken);

        var snap = new OnboardingJourneyComposer.Snapshot(
            OrganizationExists: orgDetail != null,
            EmailVerified: emailVerified,
            LicenseOperational: trial.IsOperational,
            HasFrameworks: hasFrameworks,
            HasAssessment: isOnboarded || hasFrameworks,
            HasTeamInvites: (orgDetail?.Members?.Count ?? 0) > 1,
            IsOnboarded: isOnboarded,
            Role: role);

        return OnboardingJourneyComposer.Compose(
            snap,
            orgDetail?.OrganizationName ?? "Organization",
            orgId,
            hospitalId,
            hospital?.Name,
            FormatName(admin?.FirstName, admin?.LastName),
            admin?.Email,
            trial);
    }

    private async Task<OnboardingTrialSummaryDto> LoadTrialAsync(Guid orgId, CancellationToken ct)
    {
        var trial = new OnboardingTrialSummaryDto();
        try
        {
            var result = await _proxy.ProxyToOrganizationServiceAsync(
                $"/internal/licenses/status?organizationId={orgId}",
                HttpMethod.Get,
                null,
                ct);

            if (result is not ContentResult content || (content.StatusCode ?? 0) >= 400 || string.IsNullOrWhiteSpace(content.Content))
            {
                return trial;
            }

            using var doc = JsonDocument.Parse(content.Content);
            var root = doc.RootElement;
            var data = root.TryGetProperty("data", out var d) ? d : root;

            if (TryGetPropertyIgnoreCase(data, "licenseKeyId", out var idProp) && idProp.ValueKind != JsonValueKind.Null)
            {
                trial.LicenseKeyId = idProp.ValueKind == JsonValueKind.String
                    ? Guid.Parse(idProp.GetString()!)
                    : idProp.GetGuid();
            }
            trial.LicenseType = GetString(data, "licenseType");
            trial.LicenseStatus = GetString(data, "status");
            trial.MaxSeats = GetInt(data, "maxSeats");
            trial.UsedSeats = GetInt(data, "usedSeats");
            trial.IsOperational = TryGetPropertyIgnoreCase(data, "isOperational", out var op) && op.ValueKind == JsonValueKind.True;

            if (TryGetPropertyIgnoreCase(data, "expiresAtUtc", out var exp) && exp.ValueKind != JsonValueKind.Null)
            {
                trial.ExpiresAtUtc = exp.GetDateTime();
                var days = (int)Math.Ceiling((trial.ExpiresAtUtc.Value - DateTime.UtcNow).TotalDays);
                trial.DaysRemaining = Math.Max(0, days);
            }

            if (TryGetPropertyIgnoreCase(data, "entitlements", out var ents) && ents.ValueKind == JsonValueKind.Array)
            {
                trial.Entitlements = ents.EnumerateArray()
                    .Select(e => e.GetString() ?? string.Empty)
                    .Where(s => s.Length > 0)
                    .ToList();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed loading license status for journey");
        }

        return trial;
    }

    private static Guid? ParseGuidClaim(ClaimsPrincipal user, string type)
    {
        var v = user.FindFirst(type)?.Value;
        return Guid.TryParse(v, out var g) ? g : null;
    }

    private static string? FormatName(string? first, string? last)
    {
        var n = $"{first} {last}".Trim();
        return string.IsNullOrWhiteSpace(n) ? null : n;
    }

    private static bool TryGetPropertyIgnoreCase(JsonElement element, string name, out JsonElement value)
    {
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

    private static string? GetString(JsonElement el, string name)
    {
        if (!TryGetPropertyIgnoreCase(el, name, out var p))
        {
            return null;
        }

        return p.ValueKind == JsonValueKind.String ? p.GetString() : p.ToString();
    }

    private static int GetInt(JsonElement el, string name)
    {
        if (TryGetPropertyIgnoreCase(el, name, out var p) && p.TryGetInt32(out var i))
        {
            return i;
        }

        return 0;
    }
}
