using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Shared.Infrastructure.Domain
{
    public abstract class PlatformEntity : BaseEntity
    {
        [Required]
        [MaxLength(10)]
        public string PlatformId { get; set; }
    }
}
