namespace ComplianceService.Application.Commands;

/// <summary>
/// Triggers idempotent synchronization of OrganizationComplianceObligation records
/// for the given organization × framework pair. Typically invoked after framework enrollment.
/// </summary>
public class SyncObligationsForOrganizationCommand
{
    public Guid OrganizationId { get; set; }
    public Guid HospitalId { get; set; }
    public Guid TenantId { get; set; }
    public Guid FrameworkId { get; set; }

    public SyncObligationsForOrganizationCommand(
        Guid organizationId,
        Guid hospitalId,
        Guid tenantId,
        Guid frameworkId)
    {
        OrganizationId = organizationId;
        HospitalId = hospitalId;
        TenantId = tenantId;
        FrameworkId = frameworkId;
    }
}
