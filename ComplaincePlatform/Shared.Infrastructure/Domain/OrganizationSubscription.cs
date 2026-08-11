using System;
using System.ComponentModel.DataAnnotations;

namespace Shared.Infrastructure.Domain
{
    public class OrganizationSubscription : BaseEntity
    {
        [Required]
        public Guid OrganizationId { get; set; }

        [Required]
        public Guid SubscriptionPlanId { get; set; }

        public DateTime StartsAt { get; set; }

        public DateTime? EndsAt { get; set; }

        public bool IsTrial { get; set; }

        public bool IsActive { get; set; } = true;

        public int CurrentUserCount { get; set; }

        public int CurrentFrameworkCount { get; set; }
    }
}

