using Microsoft.Extensions.Logging;
using Shared.Infrastructure.Auth;

namespace ComplianceService.Application.Authorization;

public sealed class HospitalAccessAuthorizer : IHospitalAccessAuthorizer
{
    private readonly IHospitalDirectory _directory;
    private readonly ILogger<HospitalAccessAuthorizer> _logger;

    public HospitalAccessAuthorizer(
        IHospitalDirectory directory,
        ILogger<HospitalAccessAuthorizer> logger)
    {
        _directory = directory;
        _logger = logger;
    }

    public async Task<HospitalAccessResult> AuthorizeAsync(
        Guid requestedHospitalId,
        HospitalAccessCaller caller,
        CancellationToken cancellationToken = default)
    {
        if (requestedHospitalId == Guid.Empty)
        {
            return HospitalAccessResult.Deny(HospitalAccessStatus.HospitalIdRequired, requestedHospitalId);
        }

        var lookup = await _directory.GetHospitalAsync(requestedHospitalId, cancellationToken);
        if (lookup.Status == HospitalLookupStatus.Unavailable)
        {
            _logger.LogWarning(
                "OrganizationService unavailable while authorizing hospital {HospitalId}",
                requestedHospitalId);
            return HospitalAccessResult.Deny(HospitalAccessStatus.OrganizationServiceUnavailable, requestedHospitalId);
        }

        if (lookup.Status == HospitalLookupStatus.NotFound || lookup.Hospital is null)
        {
            return HospitalAccessResult.Deny(HospitalAccessStatus.HospitalNotFound, requestedHospitalId);
        }

        var hospital = lookup.Hospital;
        if (!hospital.IsUsable)
        {
            return HospitalAccessResult.Deny(HospitalAccessStatus.HospitalInactive, requestedHospitalId);
        }

        if (caller.Kind == HospitalAccessCallerKind.Privileged)
        {
            return HospitalAccessResult.Allowed(requestedHospitalId, hospital);
        }

        if (caller.Kind == HospitalAccessCallerKind.InternalService)
        {
            var internalOrg = caller.OrganizationId.GetValueOrDefault();
            if (internalOrg != Guid.Empty && hospital.OrganizationId != internalOrg)
            {
                return HospitalAccessResult.Deny(
                    HospitalAccessStatus.OrganizationMismatch,
                    requestedHospitalId,
                    hospital.OrganizationId);
            }

            return HospitalAccessResult.Allowed(requestedHospitalId, hospital);
        }

        var callerOrg = caller.OrganizationId.GetValueOrDefault();
        if (callerOrg == Guid.Empty)
        {
            return HospitalAccessResult.Deny(HospitalAccessStatus.OrganizationContextMissing, requestedHospitalId);
        }

        if (hospital.OrganizationId != callerOrg)
        {
            _logger.LogWarning(
                "Hospital {HospitalId} belongs to organization {HospitalOrg} but caller organization is {CallerOrg}",
                requestedHospitalId,
                hospital.OrganizationId,
                callerOrg);
            return HospitalAccessResult.Deny(
                HospitalAccessStatus.OrganizationMismatch,
                requestedHospitalId,
                hospital.OrganizationId);
        }

        if (caller.Kind == HospitalAccessCallerKind.User)
        {
            var callerHospital = caller.HospitalId.GetValueOrDefault();
            if (callerHospital == Guid.Empty)
            {
                return HospitalAccessResult.Deny(HospitalAccessStatus.OrganizationContextMissing, requestedHospitalId);
            }

            if (callerHospital != requestedHospitalId)
            {
                return HospitalAccessResult.Deny(
                    HospitalAccessStatus.HospitalMismatch,
                    requestedHospitalId,
                    hospital.OrganizationId);
            }
        }

        return HospitalAccessResult.Allowed(requestedHospitalId, hospital);
    }

    public static bool IsPrivileged(IEnumerable<string> roles)
    {
        foreach (var role in roles)
        {
            if (string.Equals(role, ApplicationRoles.SUPER_ADMIN, StringComparison.Ordinal)
                || string.Equals(role, ApplicationRoles.SYSTEM, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
