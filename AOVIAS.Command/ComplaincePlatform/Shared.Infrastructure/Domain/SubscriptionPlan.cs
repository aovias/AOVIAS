using System;
using System.ComponentModel.DataAnnotations;

namespace Shared.Infrastructure.Domain
{
    public class SubscriptionPlan : BaseEntity
    {
        [Required]
        [MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        public int MaxUsers { get; set; }

        public int MaxFrameworks { get; set; }

        [Required]
        [MaxLength(10)]
        public string BillingPeriod { get; set; } = "monthly"; // monthly, yearly, etc.

        public decimal PriceAmount { get; set; }

        [MaxLength(10)]
        public string Currency { get; set; } = "INR";

        public bool IsActive { get; set; } = true;

        /// <summary>
        /// CRP Phase 6 catalogue lifecycle: Draft | Published | Retired.
        /// Published aligns with IsActive=true; Retired/Draft with IsActive=false when set by admin.
        /// </summary>
        [MaxLength(20)]
        public string LifecycleStatus { get; set; } = "Published";
    }
}

