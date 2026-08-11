using System;
using System.ComponentModel.DataAnnotations;

namespace Shared.Infrastructure.Domain
{
    public class Department : BaseEntity
    {
        [Required]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Category { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime? UpdatedAt { get; set; }

        public Guid? OrganizationId { get; set; }
        public Guid? HospitalId { get; set; }
        public Guid? ParentDepartmentId { get; set; }
        public Guid? DepartmentTypeValueId { get; set; }
        public Guid? ManagerEmploymentId { get; set; }
        [MaxLength(50)]
        public string? CostCenter { get; set; }
        [MaxLength(100)]
        public string? ServiceLine { get; set; }
        public string Status { get; set; } = "Active";
        public DateTime? EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
        public bool IsDeleted { get; set; }

    public ICollection<Service> Services { get; set; } = new List<Service>();
    }
}
