using Shared.Infrastructure.Domain;

namespace ComplianceService.Domain
{
    /// <summary>
    /// Organization-scoped department master used by the Control Center for department risk
    /// roll-ups (Security, Administration, Clinical, HR, ...). Named ComplianceDepartment to avoid
    /// collision with the unrelated <see cref="Shared.Infrastructure.Domain.Department"/> master.
    /// Tasks/findings reference a department by id where available.
    /// </summary>
    public class ComplianceDepartment : BaseEntity
    {
        public Guid OrganizationId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }
}
