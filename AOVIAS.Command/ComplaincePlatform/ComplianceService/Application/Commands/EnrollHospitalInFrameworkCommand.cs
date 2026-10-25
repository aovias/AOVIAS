namespace ComplianceService.Application.Commands;

/// <summary>
/// Command to enroll a hospital in a compliance framework.
/// </summary>
public class EnrollHospitalInFrameworkCommand
{
    public Guid HospitalId { get; set; }
    public Guid FrameworkId { get; set; }
    public Guid RequestedByUserId { get; set; }

    public EnrollHospitalInFrameworkCommand(Guid hospitalId, Guid frameworkId, Guid requestedByUserId)
    {
        HospitalId = hospitalId;
        FrameworkId = frameworkId;
        RequestedByUserId = requestedByUserId;
    }
}
