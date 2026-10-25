using ComplianceService.Domain.Enums;
using Shared.Infrastructure.Domain;

namespace ComplianceService.Domain
{
    public class ComplianceFramework : PlatformEntity
    {
        public string Name { get; set; }           // NABH
        public string Authority { get; set; }      // QCI
        public string Version { get; set; }        // Entry Level / 2024

        /// <summary>
        /// Legacy flag kept in sync with <see cref="Status"/>:
        /// <c>IsActive == (Status == FrameworkCatalogStatus.Active)</c>.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>Catalog lifecycle. Defaults to Active for seed/backward compatibility.</summary>
        public FrameworkCatalogStatus Status { get; set; } = FrameworkCatalogStatus.Active;

        public DateTime? PublishedAt { get; set; }
        public DateTime? RetiredAt { get; set; }
        public DateTime? ArchivedAt { get; set; }

        /// <summary>Keeps <see cref="IsActive"/> aligned with <see cref="Status"/>.</summary>
        public void SyncIsActiveFromStatus()
        {
            IsActive = Status == FrameworkCatalogStatus.Active;
        }
    }
}
