using System;
using System.ComponentModel.DataAnnotations;

namespace Shared.Infrastructure.Domain
{
    public class FrameworkSubscription : BaseEntity
    {
        [Required]
        public Guid OrganizationSubscriptionId { get; set; }

        [Required]
        public Guid FrameworkId { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime ActivatedAt { get; set; }

        public DateTime? DeactivatedAt { get; set; }
    }
}

