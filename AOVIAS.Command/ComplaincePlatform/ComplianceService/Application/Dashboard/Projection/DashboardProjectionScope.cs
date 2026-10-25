namespace ComplianceService.Application.Dashboard.Projection;

public readonly record struct DashboardProjectionScope(
    Guid OrganizationId,
    Guid HospitalId,
    Guid FrameworkId)
{
    public static DashboardProjectionScope ForHospital(Guid organizationId, Guid hospitalId, Guid? frameworkId = null) =>
        new(organizationId, hospitalId, frameworkId is { } fid && fid != Guid.Empty ? fid : Guid.Empty);

    public static DashboardProjectionScope ForTenant(Guid organizationId) =>
        new(organizationId, Guid.Empty, Guid.Empty);

    public bool IsTenantWide => HospitalId == Guid.Empty;

    public bool IsAllFrameworks => FrameworkId == Guid.Empty;
}
