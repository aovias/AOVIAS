using Microsoft.AspNetCore.Authorization;
using Shared.Infrastructure.Auth;
using System.Security.Claims;

namespace Shared.Authorization;

public static class AuthorizationContextBuilder
{
    public static AuthorizationRequest FromClaimsPrincipal(ClaimsPrincipal user, string resource, string action, IReadOnlyDictionary<string, string>? context = null)
    {
        var subjectId = ParseGuid(user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub"));
        return new AuthorizationRequest
        {
            SubjectId = subjectId,
            PersonId = ParseNullableGuid(user.FindFirstValue(AuthConstants.Claims.PersonId)),
            EmploymentId = ParseNullableGuid(user.FindFirstValue(AuthConstants.Claims.EmploymentId)),
            ProfessionalProfileId = ParseNullableGuid(user.FindFirstValue(AuthConstants.Claims.ProfessionalProfileId)),
            OrganizationId = ParseNullableGuid(user.FindFirstValue("organization_id"))
                ?? ParseNullableGuid(user.FindFirstValue(AuthConstants.Claims.OrganizationId)),
            HospitalId = ParseNullableGuid(user.FindFirstValue("hospital_id")),
            DepartmentId = ParseNullableGuid(user.FindFirstValue(AuthConstants.Claims.ActiveDepartmentId)),
            Resource = resource,
            Action = action,
            Context = context ?? new Dictionary<string, string>()
        };
    }

    public static AuthorizationRequest FromPermissionCode(ClaimsPrincipal user, string permissionCode, IReadOnlyDictionary<string, string>? context = null)
    {
        var parts = permissionCode.Split('.', 2, StringSplitOptions.RemoveEmptyEntries);
        var resource = parts.Length > 0 ? parts[0] : permissionCode;
        var action = parts.Length > 1 ? parts[1] : permissionCode;
        if (permissionCode.Count(c => c == '.') >= 2)
        {
            var lastDot = permissionCode.LastIndexOf('.');
            resource = permissionCode[..lastDot];
            action = permissionCode[(lastDot + 1)..];
        }
        return FromClaimsPrincipal(user, resource, action, context);
    }

    private static Guid ParseGuid(string? value) =>
        Guid.TryParse(value, out var id) ? id : Guid.Empty;

    private static Guid? ParseNullableGuid(string? value) =>
        Guid.TryParse(value, out var id) && id != Guid.Empty ? id : null;
}
