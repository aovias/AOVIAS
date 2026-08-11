using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.Infrastructure.Domain
{
    public class Organization : PlatformEntity
    {
        public string Name { get; set; }
        /// <summary>User-editable unique organization code (max 20, uppercase).</summary>
        public string OrganizationCode { get; set; } = string.Empty;
        public string Type { get; set; }
        public string Address { get; set; }
        public Guid? ParentOrganizationId { get; set; }
        public string Status { get; set; } = "Active";
        public DateTime? EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
        public bool IsDeleted { get; set; }
    }
}
