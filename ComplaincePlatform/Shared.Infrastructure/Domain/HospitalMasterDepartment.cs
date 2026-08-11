namespace Shared.Infrastructure.Domain;

/// <summary>
/// Associates a hospital with selected master departments (onboarding wizard).
/// </summary>
public class HospitalMasterDepartment
{
    public Guid HospitalId { get; set; }
    public Guid MasterDepartmentId { get; set; }

    public Hospital Hospital { get; set; } = null!;
    public MasterDepartment MasterDepartment { get; set; } = null!;
}
