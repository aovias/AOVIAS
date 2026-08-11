using ComplianceService.Domain.Copilot;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ComplianceService.Infrastructure.Copilot.EntityConfigurations;

public sealed class CopilotEffectivenessSnapshotConfiguration : IEntityTypeConfiguration<CopilotEffectivenessSnapshot>
{
    public void Configure(EntityTypeBuilder<CopilotEffectivenessSnapshot> entity)
    {
        entity.ToTable("CopilotEffectivenessSnapshots");
        entity.HasKey(e => e.Id);

        entity.HasIndex(e => e.OrganizationId);
        entity.HasIndex(e => new { e.OrganizationId, e.SnapshotDate }).IsUnique();
    }
}
