namespace ComplianceService.Application.Contracts;

/// <summary>
/// Resolved data scope for a Control Center request: tenant/organization, the in-scope hospital
/// ids (single hospital or org-wide fallback), and an optional framework filter.
/// </summary>
public sealed class ControlCenterScope
{
    public Guid TenantId { get; init; }
    public Guid OrganizationId { get; init; }
    public IReadOnlyList<Guid> HospitalIds { get; init; } = Array.Empty<Guid>();
    public Guid? HospitalId { get; init; }
    public Guid? FrameworkId { get; init; }

    public bool HasHospitals => HospitalIds.Count > 0;
}
