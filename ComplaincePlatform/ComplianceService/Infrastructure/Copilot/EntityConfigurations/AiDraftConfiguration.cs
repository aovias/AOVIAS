using ComplianceService.Domain.Copilot;
using ComplianceService.Domain.Copilot.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ComplianceService.Infrastructure.Copilot.EntityConfigurations;

public sealed class AiDraftConfiguration : IEntityTypeConfiguration<AiDraft>
{
    public void Configure(EntityTypeBuilder<AiDraft> entity)
    {
        entity.ToTable("AiDrafts");

        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).ValueGeneratedNever();

        // ── Identity ──────────────────────────────────────────────────────────
        entity.Property(e => e.OrganizationId).IsRequired();
        entity.Property(e => e.GroupId).IsRequired();

        // ── Classification ────────────────────────────────────────────────────
        entity.Property(e => e.DraftType)
              .IsRequired()
              .HasConversion<int>();

        entity.Property(e => e.SourceType)
              .IsRequired()
              .HasConversion<int>()
              .HasDefaultValue(DraftSourceType.Manual);

        entity.Property(e => e.SourceEntityId);
        entity.Property(e => e.RecommendationId);

        entity.Property(e => e.Persona)
              .IsRequired()
              .HasMaxLength(100);

        // ── Content ───────────────────────────────────────────────────────────
        entity.Property(e => e.Title)
              .IsRequired()
              .HasMaxLength(500);

        entity.Property(e => e.Summary)
              .HasMaxLength(2000)
              .HasDefaultValue(string.Empty);

        entity.Property(e => e.Content)
              .IsRequired()
              .HasColumnType("nvarchar(max)");

        // ── Generation Provenance ─────────────────────────────────────────────
        entity.Property(e => e.GeneratedBy)
              .HasMaxLength(200)
              .HasDefaultValue(string.Empty);

        entity.Property(e => e.Confidence)
              .HasColumnType("decimal(5,4)")
              .HasDefaultValue(1.0m);

        entity.Property(e => e.MetadataJson)
              .HasColumnType("nvarchar(max)");

        entity.Property(e => e.GeneratedAtUtc).IsRequired();

        // ── Lifecycle ─────────────────────────────────────────────────────────
        entity.Property(e => e.Status)
              .IsRequired()
              .HasConversion<int>();

        entity.Property(e => e.Version).IsRequired();
        entity.Property(e => e.CreatedUtc).IsRequired();
        entity.Property(e => e.UpdatedUtc).IsRequired();

        entity.Ignore(e => e.DomainEvents);

        // ── Indexes ───────────────────────────────────────────────────────────

        entity.HasIndex(e => e.OrganizationId)
              .HasDatabaseName("IX_AiDrafts_OrganizationId");

        entity.HasIndex(e => e.GroupId)
              .HasDatabaseName("IX_AiDrafts_GroupId");

        entity.HasIndex(e => e.Status)
              .HasDatabaseName("IX_AiDrafts_Status");

        entity.HasIndex(e => e.Persona)
              .HasDatabaseName("IX_AiDrafts_Persona");

        entity.HasIndex(e => e.DraftType)
              .HasDatabaseName("IX_AiDrafts_DraftType");

        entity.HasIndex(e => e.RecommendationId)
              .HasDatabaseName("IX_AiDrafts_RecommendationId");

        entity.HasIndex(e => e.CreatedUtc)
              .HasDatabaseName("IX_AiDrafts_CreatedUtc");

        entity.HasIndex(new[] { nameof(AiDraft.OrganizationId), nameof(AiDraft.GroupId) })
              .HasDatabaseName("IX_AiDrafts_OrgId_GroupId");
    }
}
