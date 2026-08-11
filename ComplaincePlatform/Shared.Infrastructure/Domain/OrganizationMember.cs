using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Shared.Infrastructure.Domain
{
    public class OrganizationMember : BaseEntity
    {
    public Guid OrganizationId { get; set; }
    public Guid UserId { get; set; }
    public Guid? PersonId { get; set; }

        /// <summary>
        /// Optional master department for role context (onboarding team invites).
        /// </summary>
        public Guid? DepartmentId { get; set; }

        /// <summary>Operational designation (e.g. Quality Head, Chief Pharmacist).</summary>
        [MaxLength(100)]
        public string? Designation { get; set; }

        /// <summary>Reports-to user id for escalation hierarchy (Sprint 9B).</summary>
        public Guid? ReportsToUserId { get; set; }

        public string Role { get; set; }
        [MaxLength(100)]
        public string? FirstName { get; set; }
        [MaxLength(100)]
        public string? LastName { get; set; }
        [MaxLength(256)]
        public string? Email { get; set; }
        [MaxLength(20)]
        public string? PhoneNumber { get; set; }
        [MaxLength(20)]
        public string? AlternatePhoneNumber { get; set; }
        [MaxLength(500)]
        public string? ProfilePicUrl { get; set; }
        public string? Preferences { get; set; }

        public Guid? MembershipTypeValueId { get; set; }
        public string MembershipStatus { get; set; } = "Active";
        public DateTime EffectiveFrom { get; set; } = DateTime.UtcNow;
        public DateTime? EffectiveTo { get; set; }
        public bool IsPrimary { get; set; } = true;
        public Guid? HospitalId { get; set; }
        public Guid? EmploymentId { get; set; }
        public bool IsDeleted { get; set; }
        public Guid? ApprovedByUserId { get; set; }
        [MaxLength(500)]
        public string? ApprovalReason { get; set; }
    }
}
