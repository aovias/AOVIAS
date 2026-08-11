namespace ComplianceService.Application.Analytics;

/// <summary>Standard filter scope for cross-module analytics KPIs.</summary>
public sealed record AnalyticsScope(
    Guid OrganizationId,
    Guid HospitalId,
    Guid FrameworkId)
{
    public static AnalyticsScope ForHospital(Guid organizationId, Guid hospitalId, Guid frameworkId) =>
        new(organizationId, hospitalId, frameworkId);
}
