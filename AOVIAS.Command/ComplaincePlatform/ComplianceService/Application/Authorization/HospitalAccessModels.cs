using Shared.Infrastructure.Api;

namespace ComplianceService.Application.Authorization;

public enum HospitalLookupStatus
{
    Found,
    NotFound,
    Unavailable
}

public sealed record OrganizationHospitalRecord(
    Guid Id,
    Guid OrganizationId,
    string Status,
    bool IsDeleted,
    bool IsOnboarded)
{
    public bool IsUsable =>
        !IsDeleted
        && (string.IsNullOrWhiteSpace(Status)
            || string.Equals(Status, "Active", StringComparison.OrdinalIgnoreCase));
}

public sealed record HospitalLookupResult(HospitalLookupStatus Status, OrganizationHospitalRecord? Hospital)
{
    public static HospitalLookupResult Found(OrganizationHospitalRecord hospital)
        => new(HospitalLookupStatus.Found, hospital);

    public static HospitalLookupResult NotFound()
        => new(HospitalLookupStatus.NotFound, null);

    public static HospitalLookupResult Unavailable()
        => new(HospitalLookupStatus.Unavailable, null);
}

public enum HospitalAccessCallerKind
{
    /// <summary>Authenticated end-user JWT (hospital_id in the token is binding).</summary>
    User,

    /// <summary>Service-to-service with execution-context tenant; no JWT hospital bind.</summary>
    InternalService,

    /// <summary>Platform operator; hospital must exist but may cross org/hospital.</summary>
    Privileged
}

public sealed record HospitalAccessCaller(
    HospitalAccessCallerKind Kind,
    Guid? OrganizationId,
    Guid? HospitalId,
    IReadOnlyList<string> Roles);

public enum HospitalAccessStatus
{
    Allowed,
    HospitalIdRequired,
    HospitalNotFound,
    HospitalInactive,
    OrganizationMismatch,
    HospitalMismatch,
    OrganizationContextMissing,
    OrganizationServiceUnavailable
}

public sealed record HospitalAccessResult(
    HospitalAccessStatus Status,
    Guid RequestedHospitalId,
    Guid OrganizationId,
    OrganizationHospitalRecord? Hospital)
{
    public bool IsAllowed => Status == HospitalAccessStatus.Allowed && OrganizationId != Guid.Empty;

    public static HospitalAccessResult Allowed(Guid hospitalId, OrganizationHospitalRecord hospital)
        => new(HospitalAccessStatus.Allowed, hospitalId, hospital.OrganizationId, hospital);

    public static HospitalAccessResult Deny(HospitalAccessStatus status, Guid hospitalId, Guid organizationId = default)
        => new(status, hospitalId, organizationId, null);

    public ApiResponseCode ToApiResponseCode() => Status switch
    {
        HospitalAccessStatus.Allowed => ApiResponseCode.SUCCESS,
        HospitalAccessStatus.HospitalIdRequired => ApiResponseCode.HOSPITAL_ID_REQUIRED,
        HospitalAccessStatus.HospitalNotFound => ApiResponseCode.RESOURCE_NOT_FOUND,
        HospitalAccessStatus.HospitalInactive => ApiResponseCode.RESOURCE_NOT_FOUND,
        HospitalAccessStatus.OrganizationMismatch => ApiResponseCode.FORBIDDEN,
        HospitalAccessStatus.HospitalMismatch => ApiResponseCode.FORBIDDEN,
        HospitalAccessStatus.OrganizationContextMissing => ApiResponseCode.ORGANIZATION_CONTEXT_MISSING,
        HospitalAccessStatus.OrganizationServiceUnavailable => ApiResponseCode.SERVICE_UNAVAILABLE,
        _ => ApiResponseCode.FORBIDDEN
    };
}

public sealed class HospitalAccessException : InvalidOperationException
{
    public HospitalAccessException(HospitalAccessResult result)
        : base($"Hospital access denied: {result.Status} for {result.RequestedHospitalId}")
    {
        Result = result;
    }

    public HospitalAccessResult Result { get; }
}
