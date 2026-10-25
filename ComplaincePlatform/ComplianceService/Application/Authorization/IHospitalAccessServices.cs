namespace ComplianceService.Application.Authorization;

/// <summary>
/// Resolves a hospital from OrganizationService without treating transport failures as "not found".
/// </summary>
public interface IHospitalDirectory
{
    Task<HospitalLookupResult> GetHospitalAsync(Guid hospitalId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Canonical hospital/tenant authorization: requested hospital exists, belongs to the caller tenant,
/// and the caller is allowed to operate on that hospital.
/// </summary>
public interface IHospitalAccessAuthorizer
{
    Task<HospitalAccessResult> AuthorizeAsync(
        Guid requestedHospitalId,
        HospitalAccessCaller caller,
        CancellationToken cancellationToken = default);
}

/// <summary>Per-request authorized hospital, set by the HTTP filter after a successful check.</summary>
public interface IHospitalAccessContext
{
    HospitalAccessResult? Current { get; set; }
}

public interface IHospitalAccessCallerResolver
{
    HospitalAccessCaller Resolve();
}
