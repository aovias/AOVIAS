using System;
using System.ComponentModel.DataAnnotations;

namespace Shared.Infrastructure.Domain
{
    public class Service : BaseEntity
    {
        [Required]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Code { get; set; } = string.Empty;

    public Guid DepartmentId { get; set; }

        [MaxLength(100)]
        public string? Category { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

    public bool IsCritical { get; set; } = false;

        public bool IsActive { get; set; } = true;

        public DateTime? UpdatedAt { get; set; }

    public Department? Department { get; set; }
    }
}
