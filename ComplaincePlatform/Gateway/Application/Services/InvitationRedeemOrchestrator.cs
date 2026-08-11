using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Gateway.Configuration;
using Gateway.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Shared.Infrastructure.Api;

namespace Gateway.Application.Services;

/// <summary>CRP Phase 7 â€” orchestrates invitation redeem across Identity, Person, Org, Workforce, Licensing.</summary>
public sealed class InvitationRedeemOrchestrator
{
    private readonly IProxyService _proxy;
    private readonly IOptions<WorkforceOnboardingOptions> _options;
    private readonly ILogger<InvitationRedeemOrchestrator> _logger;

    public InvitationRedeemOrchestrator(
        IProxyService proxy,
        IOptions<WorkforceOnboardingOptions> options,
        ILogger<InvitationRedeemOrchestrator> logger)
    {
        _proxy = proxy;
        _options = options;
        _logger = logger;
    }

    public async Task<IActionResult> PreviewAsync(string token, CancellationToken cancellationToken)
    {
        return await _proxy.ProxyToOrganizationServiceAsync(
            $"/internal/invitations/preview?token={Uri.EscapeDataString(token)}",
            HttpMethod.Get,
            cancellationToken: cancellationToken);
    }

    public async Task<object> RedeemAsync(
        string token,
        string? password,
        string? firstName,
        string? lastName,
        ClaimsPrincipal? user,
        CancellationToken cancellationToken)
    {
        var inviteResult = await _proxy.ProxyToOrganizationServiceAsync(
            $"/internal/invitations/by-token?token={Uri.EscapeDataString(token)}",
            HttpMethod.Get,
            cancellationToken: cancellationToken);
        var invite = await ReadDataObjectAsync(inviteResult, cancellationToken)
                     ?? throw new InvalidOperationException("Invitation not found or invalid.");

        var status = GetString(invite, "status") ?? GetString(invite, "Status");
        if (!string.Equals(status, "Pending", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Invitation is {status}.");
        }

        var invitationId = GetGuid(invite, "id") ?? GetGuid(invite, "Id")
                           ?? throw new InvalidOperationException("Invitation id missing.");
        var email = GetString(invite, "email") ?? GetString(invite, "Email")
                    ?? throw new InvalidOperationException("Invitation email missing.");
        var organizationId = GetGuid(invite, "organizationId") ?? GetGuid(invite, "OrganizationId") ?? Guid.Empty;
        var hospitalId = GetGuid(invite, "hospitalId") ?? GetGuid(invite, "HospitalId") ?? Guid.Empty;
        var departmentId = GetGuid(invite, "departmentId") ?? GetGuid(invite, "DepartmentId");
        var designation = GetString(invite, "designation") ?? GetString(invite, "Designation");
        var requireSeat = GetBool(invite, "requireSeat") ?? GetBool(invite, "RequireSeat") ?? true;
        var positionId = GetGuid(invite, "positionId") ?? GetGuid(invite, "PositionId");
        var managerEmploymentId = GetGuid(invite, "managerEmploymentId") ?? GetGuid(invite, "ManagerEmploymentId");
        var existingPersonId = GetGuid(invite, "personId") ?? GetGuid(invite, "PersonId");
        var existingMembershipId = GetGuid(invite, "membershipId") ?? GetGuid(invite, "MembershipId");
        var existingEmploymentId = GetGuid(invite, "employmentId") ?? GetGuid(invite, "EmploymentId");
        var seatAlready = GetBool(invite, "seatAssigned") ?? GetBool(invite, "SeatAssigned") ?? false;

        await PatchPipelineAsync(invitationId, new { pipelineStep = "IdentityEnsure" }, cancellationToken);

        var userId = await ResolveUserIdAsync(email, password, firstName, lastName, user, cancellationToken);
        await PatchPipelineAsync(invitationId, new { pipelineStep = "IdentityLinked", acceptedUserId = userId }, cancellationToken: cancellationToken);

        var personId = existingPersonId;
        if (personId is null || personId == Guid.Empty)
        {
            personId = await EnsurePersonAsync(email, firstName, lastName, cancellationToken);
            await PatchPipelineAsync(invitationId, new { pipelineStep = "PersonEnsure", personId }, cancellationToken: cancellationToken);
        }

        Guid membershipId;
        if (existingMembershipId is { } mid && mid != Guid.Empty)
        {
            membershipId = mid;
        }
        else
        {
            var memberResult = await _proxy.ProxyToOrganizationServiceAsync(
                $"/internal/invitations/{invitationId}/accept-for-user",
                HttpMethod.Post,
                new { userId },
                cancellationToken: cancellationToken);
            var member = await ReadDataObjectAsync(memberResult, cancellationToken)
                         ?? throw new InvalidOperationException("Membership creation failed.");
            membershipId = GetGuid(member, "id") ?? GetGuid(member, "Id")
                           ?? throw new InvalidOperationException("Membership id missing.");
        }

        Guid employmentId;
        if (existingEmploymentId is { } eid && eid != Guid.Empty)
        {
            employmentId = eid;
        }
        else
        {
            await PatchPipelineAsync(invitationId, new { pipelineStep = "EmploymentCreate", membershipId, personId }, cancellationToken: cancellationToken);
            var empResult = await _proxy.ProxyToWorkforceServiceAsync(
                "/internal/employment",
                HttpMethod.Post,
                new
                {
                    personId,
                    organizationId,
                    hospitalId,
                    userId,
                    organizationMemberId = membershipId,
                    joiningDate = DateTime.UtcNow,
                    createdByUserId = userId
                },
                cancellationToken: cancellationToken);
            var emp = await ReadDataObjectAsync(empResult, cancellationToken)
                      ?? throw new InvalidOperationException("Employment creation failed.");
            employmentId = GetGuid(emp, "id") ?? GetGuid(emp, "Id")
                           ?? throw new InvalidOperationException("Employment id missing.");

            await _proxy.ProxyToOrganizationServiceAsync(
                "/internal/invitations/link-employment",
                HttpMethod.Post,
                new { membershipId, employmentId, personId },
                cancellationToken: cancellationToken);

            await PatchPipelineAsync(invitationId, new
            {
                pipelineStep = "EmploymentCreated",
                membershipId,
                employmentId,
                personId
            }, cancellationToken: cancellationToken);
        }

        if (departmentId is { } dept && dept != Guid.Empty)
        {
            try
            {
                await _proxy.ProxyToWorkforceServiceAsync(
                    $"/internal/employment/{employmentId}/assign-department",
                    HttpMethod.Post,
                    new
                    {
                        masterDepartmentId = dept,
                        effectiveFrom = DateTime.UtcNow,
                        isPrimary = true,
                        allocationPercentage = 100,
                        actorUserId = userId
                    },
                    cancellationToken: cancellationToken);
                await PatchPipelineAsync(invitationId, new { pipelineStep = "DepartmentAssigned" }, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Department assign skipped for employment {Id}", employmentId);
            }
        }

        if (positionId is { } pos && pos != Guid.Empty)
        {
            try
            {
                await _proxy.ProxyToWorkforceServiceAsync(
                    $"/internal/employment/{employmentId}/assign-position",
                    HttpMethod.Post,
                    new
                    {
                        positionId = pos,
                        effectiveFrom = DateTime.UtcNow,
                        isPrimary = true,
                        actorUserId = userId
                    },
                    cancellationToken: cancellationToken);
                await PatchPipelineAsync(invitationId, new { pipelineStep = "DesignationAssigned" }, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Position assign skipped for employment {Id}", employmentId);
            }
        }
        else if (!string.IsNullOrWhiteSpace(designation))
        {
            await PatchPipelineAsync(invitationId, new { pipelineStep = "DesignationAssigned" }, cancellationToken);
        }

        if (managerEmploymentId is { } mgr && mgr != Guid.Empty)
        {
            try
            {
                await _proxy.ProxyToWorkforceServiceAsync(
                    $"/internal/employment/{employmentId}/assign-manager",
                    HttpMethod.Post,
                    new
                    {
                        managerEmploymentId = mgr,
                        effectiveFrom = DateTime.UtcNow,
                        actorUserId = userId
                    },
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Manager assign skipped for employment {Id}", employmentId);
            }
        }

        var seatAssigned = seatAlready;
        if (requireSeat && !seatAlready)
        {
            await PatchPipelineAsync(invitationId, new { pipelineStep = "SeatAssign" }, cancellationToken);
            var seatResult = await _proxy.ProxyToOrganizationServiceAsync(
                "/internal/licenses/seats/assign",
                HttpMethod.Post,
                new { organizationId, userId, userEmail = email },
                cancellationToken: cancellationToken);
            var seatOk = GetStatusCode(seatResult) is >= 200 and < 300;
            if (!seatOk && _options.Value.FailSeatIfUnavailable)
            {
                await PatchPipelineAsync(invitationId, new
                {
                    pipelineStep = "SeatAssign",
                    pipelineError = "License seat unavailable"
                }, cancellationToken: cancellationToken);
                throw new InvalidOperationException("License seat unavailable for this invitation.");
            }

            seatAssigned = seatOk;
            if (seatOk)
            {
                await PatchPipelineAsync(invitationId, new { pipelineStep = "SeatAssigned", seatAssigned = true }, cancellationToken: cancellationToken);
            }
        }

        await PatchPipelineAsync(invitationId, new { pipelineStep = "PermissionsResolved" }, cancellationToken);
        await PatchPipelineAsync(invitationId, new
        {
            markAccepted = true,
            acceptedUserId = userId,
            membershipId,
            employmentId,
            personId,
            seatAssigned
        }, cancellationToken: cancellationToken);

        return new
        {
            invitationId,
            userId,
            personId,
            membershipId,
            employmentId,
            seatAssigned,
            organizationId,
            designation,
            status = "Accepted",
            message = "Workforce onboarding complete."
        };
    }

    private async Task<Guid> ResolveUserIdAsync(
        string email,
        string? password,
        string? firstName,
        string? lastName,
        ClaimsPrincipal? user,
        CancellationToken cancellationToken)
    {
        if (user?.Identity?.IsAuthenticated == true)
        {
            var claimEmail = user.FindFirst(ClaimTypes.Email)?.Value
                             ?? user.FindFirst("email")?.Value;
            if (!string.IsNullOrWhiteSpace(claimEmail)
                && !string.Equals(claimEmail.Trim(), email.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Authenticated user email does not match the invitation.");
            }

            var idClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? user.FindFirst("sub")?.Value;
            if (Guid.TryParse(idClaim, out var existingUserId) && existingUserId != Guid.Empty)
            {
                return existingUserId;
            }
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException("Password is required to create an account for this invitation.");
        }

        var registerBody = new
        {
            email = email.Trim(),
            password,
            firstName = firstName ?? email.Split('@')[0],
            lastName = lastName ?? "User",
            role = "ORG_USER"
        };

        var regResult = await _proxy.ProxyToIdentityServiceAsync(
            "/internal/users/register",
            HttpMethod.Post,
            registerBody,
            cancellationToken: cancellationToken);
        var status = GetStatusCode(regResult);
        if (status == (int)HttpStatusCode.Conflict)
        {
            throw new InvalidOperationException(
                "An account already exists for this email. Sign in, then accept the invitation again.");
        }

        if (status < 200 || status >= 300)
        {
            throw new InvalidOperationException("Failed to create identity for invitation.");
        }

        var data = await ReadDataObjectAsync(regResult, cancellationToken) ?? await ReadRootObjectAsync(regResult, cancellationToken);
        var userId = GetGuid(data, "userId") ?? GetGuid(data, "UserId") ?? GetGuid(data, "id") ?? GetGuid(data, "Id");
        if (userId is null || userId == Guid.Empty)
        {
            throw new InvalidOperationException("Identity register did not return userId.");
        }

        return userId.Value;
    }

    private async Task<Guid> EnsurePersonAsync(string email, string? firstName, string? lastName, CancellationToken cancellationToken)
    {
        var create = await _proxy.ProxyToPersonServiceAsync(
            "/internal/persons",
            HttpMethod.Post,
            new
            {
                legalFirstName = string.IsNullOrWhiteSpace(firstName) ? email.Split('@')[0] : firstName,
                legalLastName = string.IsNullOrWhiteSpace(lastName) ? "Member" : lastName,
                email = email.Trim(),
                displayName = $"{firstName ?? email.Split('@')[0]} {lastName ?? "Member"}".Trim()
            },
            cancellationToken: cancellationToken);
        var status = GetStatusCode(create);
        var data = await ReadDataObjectAsync(create, cancellationToken) ?? await ReadRootObjectAsync(create, cancellationToken);
        var id = GetGuid(data, "personId") ?? GetGuid(data, "PersonId") ?? GetGuid(data, "id") ?? GetGuid(data, "Id");
        if (id is { } pid && pid != Guid.Empty)
        {
            return pid;
        }

        if (status == (int)HttpStatusCode.Conflict)
        {
            // Best-effort: upsert
            var upsert = await _proxy.ProxyToPersonServiceAsync(
                "/internal/persons/upsert",
                HttpMethod.Put,
                new
                {
                    legalFirstName = firstName ?? email.Split('@')[0],
                    legalLastName = lastName ?? "Member",
                    email = email.Trim()
                },
                cancellationToken: cancellationToken);
            var udata = await ReadDataObjectAsync(upsert, cancellationToken) ?? await ReadRootObjectAsync(upsert, cancellationToken);
            id = GetGuid(udata, "personId") ?? GetGuid(udata, "PersonId") ?? GetGuid(udata, "id") ?? GetGuid(udata, "Id");
            if (id is { } up && up != Guid.Empty)
            {
                return up;
            }
        }

        throw new InvalidOperationException("Failed to ensure Person for invitation.");
    }

    private Task<IActionResult> PatchPipelineAsync(Guid invitationId, object body, CancellationToken cancellationToken)
        => _proxy.ProxyToOrganizationServiceAsync(
            $"/internal/invitations/{invitationId}/pipeline",
            HttpMethod.Post,
            body,
            cancellationToken: cancellationToken);

    private static int GetStatusCode(IActionResult result) => result switch
    {
        ObjectResult o => o.StatusCode ?? 200,
        ContentResult c => c.StatusCode ?? 200,
        StatusCodeResult s => s.StatusCode,
        _ => 200
    };

    private static async Task<JsonElement?> ReadDataObjectAsync(IActionResult result, CancellationToken cancellationToken)
    {
        var json = await ReadJsonAsync(result, cancellationToken);
        if (json == null)
        {
            return null;
        }

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement.Clone();
        if (root.TryGetProperty("data", out var d) || root.TryGetProperty("Data", out d))
        {
            return d.Clone();
        }

        return root.ValueKind == JsonValueKind.Object ? root : null;
    }

    private static async Task<JsonElement?> ReadRootObjectAsync(IActionResult result, CancellationToken cancellationToken)
    {
        var json = await ReadJsonAsync(result, cancellationToken);
        if (json == null)
        {
            return null;
        }

        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }

    private static async Task<string?> ReadJsonAsync(IActionResult result, CancellationToken cancellationToken)
    {
        if (result is ContentResult content)
        {
            return content.Content;
        }

        if (result is ObjectResult { Value: not null } obj)
        {
            return JsonSerializer.Serialize(obj.Value);
        }

        await Task.CompletedTask;
        return null;
    }

    private static string? GetString(JsonElement? el, string name)
    {
        if (el is null || el.Value.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        if (el.Value.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.String)
        {
            return p.GetString();
        }

        return null;
    }

    private static Guid? GetGuid(JsonElement? el, string name)
    {
        if (el is null || el.Value.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        if (!el.Value.TryGetProperty(name, out var p))
        {
            return null;
        }

        if (p.ValueKind == JsonValueKind.String && Guid.TryParse(p.GetString(), out var g))
        {
            return g;
        }

        if (p.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        try
        {
            return p.GetGuid();
        }
        catch
        {
            return null;
        }
    }

    private static bool? GetBool(JsonElement? el, string name)
    {
        if (el is null || el.Value.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        if (!el.Value.TryGetProperty(name, out var p))
        {
            return null;
        }

        return p.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => null
        };
    }
}



