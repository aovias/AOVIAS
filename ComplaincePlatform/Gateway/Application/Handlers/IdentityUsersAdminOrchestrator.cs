using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using Gateway.DTOs;
using Gateway.Services;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Api;
using Shared.Infrastructure.DTO;

namespace Gateway.Application.Handlers;

internal static class IdentityUsersAdminProxyHelper
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public static async Task<(bool ok, T? data, string? error)> ReadEnvelopeAsync<T>(IActionResult result)
    {
        if (result is ContentResult content && !string.IsNullOrEmpty(content.Content))
        {
            if ((content.StatusCode ?? 0) >= 400)
                return (false, default, content.Content);
            var envelope = JsonSerializer.Deserialize<ApiResponse<T>>(content.Content, JsonOptions);
            if (envelope?.Success == true && envelope.Data != null)
                return (true, envelope.Data, null);
            return (false, default, envelope?.Errors?.FirstOrDefault()?.Message ?? "Request failed.");
        }

        if (result is ObjectResult obj && obj.Value != null)
            return (false, default, JsonSerializer.Serialize(obj.Value));

        return (false, default, "Unexpected proxy response.");
    }

    public static IActionResult Success<T>(T data, string traceId)
    {
        var envelope = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, traceId);
        return new ObjectResult(envelope) { StatusCode = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS].HttpStatus };
    }

    public static IActionResult Failure(string message, string traceId, ApiResponseCode code = ApiResponseCode.VALIDATION_FAILED)
    {
        var envelope = ApiResponseFactory.Failure<object>(code, new List<ApiError> { new() { Message = message } }, traceId);
        return new ObjectResult(envelope) { StatusCode = ApiResponseCodes.Registry[code].HttpStatus };
    }

    public static string GenerateTemporaryPassword()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789!@#$%";
        var bytes = RandomNumberGenerator.GetBytes(16);
        return new string(bytes.Select(b => chars[b % chars.Length]).ToArray());
    }
}

internal sealed class MembershipListItemDto
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid UserId { get; set; }
    public Guid? PersonId { get; set; }
    public Guid? EmploymentId { get; set; }
    public string Role { get; set; } = string.Empty;
    public string MembershipStatus { get; set; } = string.Empty;
    public Guid? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public string? Designation { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? ProfilePicUrl { get; set; }
    public DateTime CreatedAt { get; set; }
}

internal sealed class PagedMembershipListDto
{
    public List<MembershipListItemDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

internal sealed class IdentityUserBatchItemDto
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? UserName { get; set; }
    public string? PhoneNumber { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }
    public bool ForcePasswordChange { get; set; }
    public DateTimeOffset? LockoutEnd { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public List<string> Roles { get; set; } = new();
    public Guid? PersonId { get; set; }
    public string PlatformId { get; set; } = string.Empty;
    public bool PasswordResetRequired { get; set; }
}

public sealed class IdentityUsersAdminOrchestrator(IProxyService proxy)
{
    public async Task<PagedUserAdminListDto> ListAsync(
        string apiVersion,
        Guid organizationId,
        int page,
        int pageSize,
        string? search,
        string? role,
        Guid? departmentId,
        string? membershipStatus,
        bool includeInactive,
        CancellationToken ct)
    {
        var qs = $"/api/v{apiVersion}/membership?page={page}&pageSize={pageSize}&organizationId={organizationId}&includeInactive={includeInactive.ToString().ToLowerInvariant()}";
        if (!string.IsNullOrWhiteSpace(search)) qs += $"&search={Uri.EscapeDataString(search)}";
        if (!string.IsNullOrWhiteSpace(role)) qs += $"&role={Uri.EscapeDataString(role)}";
        if (departmentId.HasValue) qs += $"&departmentId={departmentId}";
        if (!string.IsNullOrWhiteSpace(membershipStatus)) qs += $"&membershipStatus={Uri.EscapeDataString(membershipStatus)}";

        var membershipResult = await proxy.ProxyToOrganizationServiceAsync(qs, HttpMethod.Get, cancellationToken: ct);
        var (ok, pageData, _) = await IdentityUsersAdminProxyHelper.ReadEnvelopeAsync<PagedMembershipListDto>(membershipResult);
        if (!ok || pageData == null)
            return new PagedUserAdminListDto { Page = page, PageSize = pageSize };

        var userIds = pageData.Items.Select(i => i.UserId).Distinct().ToList();
        var identityLookup = await BatchIdentityUsersAsync(apiVersion, userIds, ct);

        var items = pageData.Items.Select(m =>
        {
            identityLookup.TryGetValue(m.UserId, out var identity);
            var fullName = string.Join(' ', new[] { m.FirstName, m.LastName }.Where(s => !string.IsNullOrWhiteSpace(s)));
            if (string.IsNullOrWhiteSpace(fullName)) fullName = identity?.Email ?? m.Email ?? "—";
            return new UserAdminListItemDto
            {
                MembershipId = m.Id,
                UserId = m.UserId,
                OrganizationId = m.OrganizationId,
                FirstName = m.FirstName,
                LastName = m.LastName,
                FullName = fullName,
                Email = identity?.Email ?? m.Email,
                UserName = identity?.UserName,
                PhoneNumber = identity?.PhoneNumber ?? m.PhoneNumber,
                ProfilePicUrl = m.ProfilePicUrl,
                DepartmentId = m.DepartmentId,
                DepartmentName = m.DepartmentName,
                Designation = m.Designation,
                Role = m.Role,
                Roles = identity?.Roles ?? new List<string> { m.Role },
                MembershipStatus = m.MembershipStatus,
                AccountStatus = identity?.Status ?? "Unknown",
                IsDeleted = identity?.IsDeleted ?? false,
                ForcePasswordChange = identity?.ForcePasswordChange ?? false,
                IsLockedOut = identity?.LockoutEnd.HasValue == true && identity.LockoutEnd > DateTimeOffset.UtcNow,
                LastLoginAt = identity?.LastLoginAt,
                CreatedAt = m.CreatedAt,
                EmploymentId = m.EmploymentId,
                PersonId = m.PersonId ?? identity?.PersonId,
                EmployeeNumber = null
            };
        }).ToList();

        return new PagedUserAdminListDto { Items = items, TotalCount = pageData.TotalCount, Page = pageData.Page, PageSize = pageData.PageSize };
    }

    public async Task<UserAdminDetailResponseDto?> GetAsync(string apiVersion, Guid organizationId, Guid userId, CancellationToken ct)
    {
        var membershipQs = $"/api/v{apiVersion}/membership?organizationId={organizationId}&userId={userId}&page=1&pageSize=1&includeInactive=true";
        var membershipResult = await proxy.ProxyToOrganizationServiceAsync(membershipQs, HttpMethod.Get, cancellationToken: ct);
        var (ok, pageData, _) = await IdentityUsersAdminProxyHelper.ReadEnvelopeAsync<PagedMembershipListDto>(membershipResult);
        var membership = ok ? pageData?.Items.FirstOrDefault() : null;

        var identityResult = await proxy.ProxyRequestAsync("IdentityClient", $"/api/v{apiVersion}/users/{userId}", HttpMethod.Get, cancellationToken: ct);
        var (iOk, identity, _) = await IdentityUsersAdminProxyHelper.ReadEnvelopeAsync<IdentityUserBatchItemDto>(identityResult);
        if (!iOk || identity == null) return null;

        var fullName = membership != null
            ? string.Join(' ', new[] { membership.FirstName, membership.LastName }.Where(s => !string.IsNullOrWhiteSpace(s)))
            : string.Empty;
        if (string.IsNullOrWhiteSpace(fullName)) fullName = identity.Email;

        return new UserAdminDetailResponseDto
        {
            MembershipId = membership?.Id ?? Guid.Empty,
            UserId = identity.UserId,
            OrganizationId = organizationId,
            FirstName = membership?.FirstName,
            LastName = membership?.LastName,
            FullName = fullName,
            Email = identity.Email,
            UserName = identity.UserName,
            PhoneNumber = identity.PhoneNumber ?? membership?.PhoneNumber,
            ProfilePicUrl = membership?.ProfilePicUrl,
            DepartmentId = membership?.DepartmentId,
            DepartmentName = membership?.DepartmentName,
            Designation = membership?.Designation,
            Role = membership?.Role ?? identity.Roles.FirstOrDefault() ?? string.Empty,
            Roles = identity.Roles,
            MembershipStatus = membership?.MembershipStatus ?? "Unknown",
            AccountStatus = identity.Status,
            IsDeleted = identity.IsDeleted,
            ForcePasswordChange = identity.ForcePasswordChange,
            IsLockedOut = identity.LockoutEnd.HasValue && identity.LockoutEnd > DateTimeOffset.UtcNow,
            LastLoginAt = identity.LastLoginAt,
            CreatedAt = membership?.CreatedAt ?? DateTime.UtcNow,
            EmploymentId = membership?.EmploymentId,
            PersonId = membership?.PersonId ?? identity.PersonId,
            PlatformId = identity.PlatformId,
            PasswordResetRequired = identity.PasswordResetRequired,
            LockoutEnd = identity.LockoutEnd
        };
    }

    private async Task<Dictionary<Guid, IdentityUserBatchItemDto>> BatchIdentityUsersAsync(string apiVersion, IReadOnlyList<Guid> userIds, CancellationToken ct)
    {
        if (userIds.Count == 0) return new Dictionary<Guid, IdentityUserBatchItemDto>();
        var result = await proxy.ProxyRequestAsync("IdentityClient", $"/api/v{apiVersion}/users/batch", HttpMethod.Post, new { userIds }, cancellationToken: ct);
        var (ok, users, _) = await IdentityUsersAdminProxyHelper.ReadEnvelopeAsync<List<IdentityUserBatchItemDto>>(result);
        if (!ok || users == null) return new Dictionary<Guid, IdentityUserBatchItemDto>();
        return users.ToDictionary(u => u.UserId);
    }
}
