using System;
using System.ComponentModel.DataAnnotations;

namespace Shared.Infrastructure.Domain
{
    public class Hospital : BaseEntity
    {
        [Required]
        public Guid OrganizationId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; }

        [Required]
        [MaxLength(20)]
        public string Code { get; set; }

        [Required]
        public string Address { get; set; }

        [MaxLength(100)]
        public string? Type { get; set; }

        public int? BedCapacity { get; set; }

        /// <summary>
        /// Optional UI bucket for bed count (e.g. "101-200") from onboarding wizard; distinct from numeric <see cref="BedCapacity"/>.
        /// </summary>
        [MaxLength(50)]
        public string? BedCapacityRange { get; set; }

        public int? WizardVersion { get; set; }

        [MaxLength(10)]
        public string? CountryCode { get; set; }

        [MaxLength(10)]
        public string? StateCode { get; set; }

        [Required]
        public bool IsOnboarded { get; set; } = false;

        public string? NABHLevel { get; set; }

        public string Status { get; set; } = "Active";
        public DateTime? EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
        public bool IsDeleted { get; set; }
        public Guid? ParentOrganizationalUnitId { get; set; }
    }
}
