using System.Security.Claims;
using ComplianceService.Application.Authorization;
using Microsoft.AspNetCore.Http;
using Shared.Infrastructure.Execution;

namespace ComplianceService.Infrastructure.Authorization;

public sealed class HospitalAccessCallerResolver : IHospitalAccessCallerResolver
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IExecutionContextAccessor _executionContext;

    public HospitalAccessCallerResolver(
        IHttpContextAccessor httpContextAccessor,
        IExecutionContextAccessor executionContext)
    {
        _httpContextAccessor = httpContextAccessor;
        _executionContext = executionContext;
    }

    public HospitalAccessCaller Resolve()
    {
        var http = _httpContextAccessor.HttpContext;
        var user = http?.User;
        var roles = ReadRoles(user);

        if (HospitalAccessAuthorizer.IsPrivileged(roles))
        {
            return new HospitalAccessCaller(
                HospitalAccessCallerKind.Privileged,
                ReadGuidClaim(user, "organization_id") ?? NullIfEmpty(_executionContext.OrganizationId),
                ReadGuidClaim(user, "hospital_id"),
                roles);
        }

        var jwtOrg = ReadGuidClaim(user, "organization_id") ?? ReadGuidClaim(user, "org_id");
        var jwtHospital = ReadGuidClaim(user, "hospital_id");
        var isUser = user?.Identity?.IsAuthenticated == true && (jwtOrg.HasValue || jwtHospital.HasValue);

        if (isUser)
        {
            return new HospitalAccessCaller(
                HospitalAccessCallerKind.User,
                jwtOrg ?? NullIfEmpty(_executionContext.OrganizationId),
                jwtHospital,
                roles);
        }

        return new HospitalAccessCaller(
            HospitalAccessCallerKind.InternalService,
            NullIfEmpty(_executionContext.OrganizationId),
            null,
            roles);
    }

    private static Guid? NullIfEmpty(Guid value) => value == Guid.Empty ? null : value;

    private static Guid? ReadGuidClaim(ClaimsPrincipal? user, string type)
    {
        var value = user?.FindFirst(type)?.Value;
        return Guid.TryParse(value, out var id) && id != Guid.Empty ? id : null;
    }

    private static IReadOnlyList<string> ReadRoles(ClaimsPrincipal? user)
    {
        if (user is null)
        {
            return Array.Empty<string>();
        }

        return user.FindAll(ClaimTypes.Role)
            .Concat(user.FindAll("role"))
            .Select(c => c.Value)
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }
}
