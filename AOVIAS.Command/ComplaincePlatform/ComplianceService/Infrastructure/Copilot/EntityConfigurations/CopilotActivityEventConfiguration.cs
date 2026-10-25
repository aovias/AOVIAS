using ComplianceService.Domain.Copilot;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ComplianceService.Infrastructure.Copilot.EntityConfigurations;

public sealed class CopilotActivityEventConfiguration : IEntityTypeConfiguration<CopilotActivityEvent>
{
    public void Configure(EntityTypeBuilder<CopilotActivityEvent> entity)
    {
        entity.ToTable("CopilotActivityEvents");
        entity.HasKey(e => e.Id);

        entity.Property(e => e.EventType).HasMaxLength(50).IsRequired();
        entity.Property(e => e.Persona).HasMaxLength(100);
        entity.Property(e => e.ActorId).HasMaxLength(100);
        entity.Property(e => e.ActorEmail).HasMaxLength(200);
        entity.Property(e => e.EntityType).HasMaxLength(100);
        entity.Property(e => e.Notes).HasMaxLength(2000);

        entity.HasIndex(e => e.OrganizationId);
        entity.HasIndex(e => e.EventType);
        entity.HasIndex(e => e.OccurredAtUtc);
    }
}
