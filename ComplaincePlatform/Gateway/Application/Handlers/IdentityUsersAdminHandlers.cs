using Gateway.Application.Commands;
using Gateway.Application.Queries;
using Gateway.DTOs;
using Gateway.Services;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Api;
using Shared.Infrastructure.DTO;

namespace Gateway.Application.Handlers;

public class IdentityUsersAdminHandlers
{
    public sealed class ListHandler(IdentityUsersAdminOrchestrator orchestrator, IHttpContextAccessor http) : IRequestHandler<ListIdentityUsersAdminQuery, IActionResult>
    {
        public async Task<IActionResult> Handle(ListIdentityUsersAdminQuery request, CancellationToken ct)
        {
            var data = await orchestrator.ListAsync(request.ApiVersion, request.OrganizationId, request.Page, request.PageSize, request.Search, request.Role, request.DepartmentId, request.MembershipStatus, request.IncludeInactive, ct);
            return IdentityUsersAdminProxyHelper.Success(data, http.HttpContext?.TraceIdentifier ?? string.Empty);
        }
    }

    public sealed class GetHandler(IdentityUsersAdminOrchestrator orchestrator, IHttpContextAccessor http) : IRequestHandler<GetIdentityUserAdminQuery, IActionResult>
    {
        public async Task<IActionResult> Handle(GetIdentityUserAdminQuery request, CancellationToken ct)
        {
            var data = await orchestrator.GetAsync(request.ApiVersion, request.OrganizationId, request.UserId, ct);
            if (data == null)
                return IdentityUsersAdminProxyHelper.Failure("User not found.", http.HttpContext?.TraceIdentifier ?? string.Empty, ApiResponseCode.RESOURCE_NOT_FOUND);
            return IdentityUsersAdminProxyHelper.Success(data, http.HttpContext?.TraceIdentifier ?? string.Empty);
        }
    }

    public sealed class CreateHandler(IProxyService proxy, IdentityUsersAdminOrchestrator orchestrator, IHttpContextAccessor http) : IRequestHandler<CreateIdentityUserAdminCommand, IActionResult>
    {
        public async Task<IActionResult> Handle(CreateIdentityUserAdminCommand request, CancellationToken ct)
        {
            var traceId = http.HttpContext?.TraceIdentifier ?? string.Empty;
            var body = request.Request;
            var identityResult = await proxy.ProxyRequestAsync("IdentityClient", $"/api/v{request.ApiVersion}/users", HttpMethod.Post, new
            {
                actorUserId = request.ActorUserId,
                email = body.Email,
                userName = body.UserName,
                temporaryPassword = body.TemporaryPassword,
                phoneNumber = body.PhoneNumber,
                role = body.Role,
                personId = body.PersonId
            }, cancellationToken: ct);

            var (ok, user, error) = await IdentityUsersAdminProxyHelper.ReadEnvelopeAsync<IdentityUserBatchItemDto>(identityResult);
            if (!ok || user == null)
                return IdentityUsersAdminProxyHelper.Failure(error ?? "Failed to create identity user.", traceId);

            var membershipResult = await proxy.ProxyToOrganizationServiceAsync($"/api/v{request.ApiVersion}/membership", HttpMethod.Post, new
            {
                organizationId = request.OrganizationId,
                userId = user.UserId,
                personId = body.PersonId,
                hospitalId = body.HospitalId,
                employmentId = body.EmploymentId,
                role = body.Role,
                departmentId = body.DepartmentId,
                designation = body.Designation,
                email = body.Email,
                membershipStatus = "Active",
                isPrimary = true
            }, cancellationToken: ct);

            var (mOk, membership, mError) = await IdentityUsersAdminProxyHelper.ReadEnvelopeAsync<MembershipListItemDto>(membershipResult);
            if (!mOk || membership == null)
            {
                await proxy.ProxyRequestAsync("IdentityClient", $"/api/v{request.ApiVersion}/users/{user.UserId}?actorUserId={request.ActorUserId}", HttpMethod.Delete, cancellationToken: ct);
                return IdentityUsersAdminProxyHelper.Failure(mError ?? "User created but membership failed.", traceId);
            }

            if (!string.IsNullOrWhiteSpace(body.FirstName) || !string.IsNullOrWhiteSpace(body.LastName))
            {
                await proxy.ProxyToOrganizationServiceAsync($"/api/v{request.ApiVersion}/membership/{membership.Id}", HttpMethod.Put, new
                {
                    firstName = body.FirstName,
                    lastName = body.LastName,
                    phoneNumber = body.PhoneNumber
                }, cancellationToken: ct);
            }

            var detail = await orchestrator.GetAsync(request.ApiVersion, request.OrganizationId, user.UserId, ct);
            return IdentityUsersAdminProxyHelper.Success(detail!, traceId);
        }
    }

    public sealed class UpdateHandler(IProxyService proxy, IdentityUsersAdminOrchestrator orchestrator, IHttpContextAccessor http) : IRequestHandler<UpdateIdentityUserAdminCommand, IActionResult>
    {
        public async Task<IActionResult> Handle(UpdateIdentityUserAdminCommand request, CancellationToken ct)
        {
            var traceId = http.HttpContext?.TraceIdentifier ?? string.Empty;
            var body = request.Request;

            var identityResult = await proxy.ProxyRequestAsync("IdentityClient", $"/api/v{request.ApiVersion}/users/{request.UserId}", HttpMethod.Put, new
            {
                actorUserId = request.ActorUserId,
                email = body.Email,
                userName = body.UserName,
                phoneNumber = body.PhoneNumber,
                role = body.Role,
                status = body.Status
            }, cancellationToken: ct);

            var (ok, _, error) = await IdentityUsersAdminProxyHelper.ReadEnvelopeAsync<IdentityUserBatchItemDto>(identityResult);
            if (!ok)
                return IdentityUsersAdminProxyHelper.Failure(error ?? "Identity update failed.", traceId);

            var membershipQs = $"/api/v{request.ApiVersion}/membership?organizationId={request.OrganizationId}&userId={request.UserId}&page=1&pageSize=1&includeInactive=true";
            var membershipList = await proxy.ProxyToOrganizationServiceAsync(membershipQs, HttpMethod.Get, cancellationToken: ct);
            var (mListOk, mPage, _) = await IdentityUsersAdminProxyHelper.ReadEnvelopeAsync<PagedMembershipListDto>(membershipList);
            var membership = mListOk ? mPage?.Items.FirstOrDefault() : null;
            if (membership != null)
            {
                await proxy.ProxyToOrganizationServiceAsync($"/api/v{request.ApiVersion}/membership/{membership.Id}", HttpMethod.Put, new
                {
                    role = body.Role,
                    departmentId = body.DepartmentId,
                    designation = body.Designation,
                    firstName = body.FirstName,
                    lastName = body.LastName,
                    email = body.Email,
                    phoneNumber = body.PhoneNumber,
                    membershipStatus = body.MembershipStatus
                }, cancellationToken: ct);
            }

            var detail = await orchestrator.GetAsync(request.ApiVersion, request.OrganizationId, request.UserId, ct);
            return IdentityUsersAdminProxyHelper.Success(detail!, traceId);
        }
    }

    public sealed class SuspendHandler(IProxyService proxy, IHttpContextAccessor http) : IRequestHandler<SuspendIdentityUserAdminCommand, IActionResult>
    {
        public Task<IActionResult> Handle(SuspendIdentityUserAdminCommand request, CancellationToken ct)
            => IdentityActionHelper.PostAsync(proxy, http, request.ApiVersion, $"/api/v{request.ApiVersion}/users/{request.UserId}/suspend", new { actorUserId = request.ActorUserId }, ct);
    }

    public sealed class ActivateHandler(IProxyService proxy, IHttpContextAccessor http) : IRequestHandler<ActivateIdentityUserAdminCommand, IActionResult>
    {
        public Task<IActionResult> Handle(ActivateIdentityUserAdminCommand request, CancellationToken ct)
            => IdentityActionHelper.PostAsync(proxy, http, request.ApiVersion, $"/api/v{request.ApiVersion}/users/{request.UserId}/activate", new { actorUserId = request.ActorUserId }, ct);
    }

    public sealed class DisableHandler(IProxyService proxy, IHttpContextAccessor http) : IRequestHandler<DisableIdentityUserAdminCommand, IActionResult>
    {
        public Task<IActionResult> Handle(DisableIdentityUserAdminCommand request, CancellationToken ct)
            => IdentityActionHelper.PostAsync(proxy, http, request.ApiVersion, $"/api/v{request.ApiVersion}/users/{request.UserId}/disable", new { actorUserId = request.ActorUserId }, ct);
    }

    public sealed class UnlockHandler(IProxyService proxy, IHttpContextAccessor http) : IRequestHandler<UnlockIdentityUserAdminCommand, IActionResult>
    {
        public Task<IActionResult> Handle(UnlockIdentityUserAdminCommand request, CancellationToken ct)
            => IdentityActionHelper.PostAsync(proxy, http, request.ApiVersion, $"/api/v{request.ApiVersion}/users/{request.UserId}/unlock", new { actorUserId = request.ActorUserId }, ct);
    }

    public sealed class DeleteHandler(IProxyService proxy, IHttpContextAccessor http) : IRequestHandler<DeleteIdentityUserAdminCommand, IActionResult>
    {
        public async Task<IActionResult> Handle(DeleteIdentityUserAdminCommand request, CancellationToken ct)
        {
            var traceId = http.HttpContext?.TraceIdentifier ?? string.Empty;
            var identityResult = await proxy.ProxyRequestAsync("IdentityClient", $"/api/v{request.ApiVersion}/users/{request.UserId}?actorUserId={request.ActorUserId}", HttpMethod.Delete, cancellationToken: ct);
            var (ok, _, error) = await IdentityUsersAdminProxyHelper.ReadEnvelopeAsync<object>(identityResult);
            if (!ok) return IdentityUsersAdminProxyHelper.Failure(error ?? "Delete failed.", traceId);

            var membershipQs = $"/api/v{request.ApiVersion}/membership?organizationId={request.OrganizationId}&userId={request.UserId}&page=1&pageSize=1&includeInactive=true";
            var membershipList = await proxy.ProxyToOrganizationServiceAsync(membershipQs, HttpMethod.Get, cancellationToken: ct);
            var (mListOk, mPage, _) = await IdentityUsersAdminProxyHelper.ReadEnvelopeAsync<PagedMembershipListDto>(membershipList);
            var membership = mListOk ? mPage?.Items.FirstOrDefault() : null;
            if (membership != null)
            {
                await proxy.ProxyToOrganizationServiceAsync($"/api/v{request.ApiVersion}/membership/{membership.Id}", HttpMethod.Put, new { membershipStatus = "Archived" }, cancellationToken: ct);
            }

            return IdentityUsersAdminProxyHelper.Success(new { }, traceId);
        }
    }

    public sealed class AdminResetPasswordHandler(IProxyService proxy, IHttpContextAccessor http) : IRequestHandler<AdminResetPasswordCommand, IActionResult>
    {
        public async Task<IActionResult> Handle(AdminResetPasswordCommand request, CancellationToken ct)
        {
            var traceId = http.HttpContext?.TraceIdentifier ?? string.Empty;
            var tempPassword = IdentityUsersAdminProxyHelper.GenerateTemporaryPassword();
            var result = await proxy.ProxyRequestAsync("IdentityClient", $"/api/v{request.ApiVersion}/users/{request.UserId}/reset-password", HttpMethod.Post, new
            {
                actorUserId = request.ActorUserId,
                newPassword = tempPassword
            }, cancellationToken: ct);
            var (ok, _, error) = await IdentityUsersAdminProxyHelper.ReadEnvelopeAsync<object>(result);
            if (!ok) return IdentityUsersAdminProxyHelper.Failure(error ?? "Password reset failed.", traceId);
            return IdentityUsersAdminProxyHelper.Success(new { message = "Password reset successfully." }, traceId);
        }
    }

    public sealed class ForcePasswordChangeHandler(IProxyService proxy, IHttpContextAccessor http) : IRequestHandler<ForcePasswordChangeIdentityCommand, IActionResult>
    {
        public Task<IActionResult> Handle(ForcePasswordChangeIdentityCommand request, CancellationToken ct)
            => IdentityActionHelper.PostAsync(proxy, http, request.ApiVersion, $"/api/v{request.ApiVersion}/users/{request.UserId}/force-password-change", new { actorUserId = request.ActorUserId }, ct);
    }

    public sealed class PlaceholderHandler(IHttpContextAccessor http) : IRequestHandler<PlaceholderPasswordManagementCommand, IActionResult>
    {
        public Task<IActionResult> Handle(PlaceholderPasswordManagementCommand request, CancellationToken ct)
        {
            var traceId = http.HttpContext?.TraceIdentifier ?? string.Empty;
            var envelope = ApiResponseFactory.Failure<object>(ApiResponseCode.VALIDATION_FAILED, new List<ApiError> { new() { Message = $"{request.Feature} is coming soon." } }, traceId);
            return Task.FromResult<IActionResult>(new ObjectResult(envelope) { StatusCode = 501 });
        }
    }
}

internal static class IdentityActionHelper
{
    public static async Task<IActionResult> PostAsync(IProxyService proxy, IHttpContextAccessor http, string apiVersion, string endpoint, object body, CancellationToken ct)
    {
        var traceId = http.HttpContext?.TraceIdentifier ?? string.Empty;
        var result = await proxy.ProxyRequestAsync("IdentityClient", endpoint, HttpMethod.Post, body, cancellationToken: ct);
        var (ok, _, error) = await IdentityUsersAdminProxyHelper.ReadEnvelopeAsync<object>(result);
        if (!ok) return IdentityUsersAdminProxyHelper.Failure(error ?? "Action failed.", traceId);
        return IdentityUsersAdminProxyHelper.Success(new { }, traceId);
    }
}
