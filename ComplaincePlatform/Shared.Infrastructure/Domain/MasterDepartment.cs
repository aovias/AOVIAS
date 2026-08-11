using System;
using System.ComponentModel.DataAnnotations;

namespace Shared.Infrastructure.Domain
{
    /// <summary>
    /// Master list of departments used across organizations.
    /// Mirrors the semantics of <see cref="Department"/> and <see cref="Service"/>,
    /// but supports an optional display order for consistent sorting.
    /// </summary>
    public class MasterDepartment : BaseEntity
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

        /// <summary>
        /// Optional explicit order for UI lists. When null, records will be ordered
        /// by name after those with a defined DisplayOrder.
        /// </summary>
        public int? DisplayOrder { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime? UpdatedAt { get; set; }

        public Guid? LinkedDepartmentId { get; set; }
    }
}

