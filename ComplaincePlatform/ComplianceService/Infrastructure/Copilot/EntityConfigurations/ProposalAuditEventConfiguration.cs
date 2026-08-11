using ComplianceService.Domain.Copilot;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ComplianceService.Infrastructure.Copilot.EntityConfigurations;

public sealed class ProposalAuditEventConfiguration : IEntityTypeConfiguration<ProposalAuditEvent>
{
    public void Configure(EntityTypeBuilder<ProposalAuditEvent> entity)
    {
        entity.ToTable("CopilotProposalAuditEvents");

        entity.HasKey(e => e.Id);

        entity.Property(e => e.Id)
              .IsRequired()
              .ValueGeneratedNever();

        entity.Property(e => e.ProposalId)
              .IsRequired();

        entity.Property(e => e.OrganizationId)
              .IsRequired();

        entity.Property(e => e.EventType)
              .HasMaxLength(50)
              .IsRequired();

        entity.Property(e => e.ActorId)
              .HasMaxLength(100)
              .IsRequired(false);

        entity.Property(e => e.ActorEmail)
              .HasMaxLength(200)
              .IsRequired(false);

        entity.Property(e => e.Notes)
              .HasMaxLength(2000)
              .IsRequired(false);

        entity.Property(e => e.ContextJson)
              .HasColumnType("nvarchar(max)")
              .IsRequired(false);

        entity.Property(e => e.OccurredAtUtc)
              .IsRequired();

        // ── Indexes ───────────────────────────────────────────────────────────

        entity.HasIndex(e => e.ProposalId)
              .HasDatabaseName("IX_CopilotProposalAuditEvents_ProposalId");

        entity.HasIndex(e => e.OrganizationId)
              .HasDatabaseName("IX_CopilotProposalAuditEvents_OrganizationId");

        entity.HasIndex(e => e.EventType)
              .HasDatabaseName("IX_CopilotProposalAuditEvents_EventType");

        entity.HasIndex(e => e.OccurredAtUtc)
              .HasDatabaseName("IX_CopilotProposalAuditEvents_OccurredAtUtc");
    }
}
