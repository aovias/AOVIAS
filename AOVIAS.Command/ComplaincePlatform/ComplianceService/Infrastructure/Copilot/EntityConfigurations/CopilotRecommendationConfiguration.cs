using ComplianceService.Domain.Copilot;
using ComplianceService.Domain.Copilot.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ComplianceService.Infrastructure.Copilot.EntityConfigurations;

public sealed class CopilotRecommendationConfiguration : IEntityTypeConfiguration<CopilotRecommendation>
{
    public void Configure(EntityTypeBuilder<CopilotRecommendation> entity)
    {
        entity.ToTable("CopilotRecommendations");

        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).ValueGeneratedNever();

        entity.Property(e => e.OrganizationId).IsRequired();

        entity.Property(e => e.RecommendationType)
              .IsRequired()
              .HasMaxLength(100);

        entity.Property(e => e.Persona)
              .IsRequired()
              .HasMaxLength(100);

        entity.Property(e => e.Priority).IsRequired();

        entity.Property(e => e.Title)
              .IsRequired()
              .HasMaxLength(500);

        entity.Property(e => e.Description)
              .HasMaxLength(4000)
              .HasDefaultValue(string.Empty);

        entity.Property(e => e.Rationale)
              .IsRequired()
              .HasMaxLength(4000);

        entity.Property(e => e.ExpectedOutcome)
              .IsRequired()
              .HasMaxLength(2000);

        entity.Property(e => e.RiskIfIgnored)
              .IsRequired()
              .HasMaxLength(2000);

        entity.Property(e => e.SourceReferences)
              .HasMaxLength(4000);

        entity.Property(e => e.Status)
              .IsRequired()
              .HasConversion<int>();

        // ── Sprint 13B: Taxonomy + Traceability ──────────────────────────────
        entity.Property(e => e.Category)
              .IsRequired()
              .HasConversion<int>()
              .HasDefaultValue(RecommendationCategory.Accreditation);

        entity.Property(e => e.Severity)
              .IsRequired()
              .HasConversion<int>()
              .HasDefaultValue(RecommendationSeverity.Medium);

        entity.Property(e => e.Confidence)
              .IsRequired()
              .HasPrecision(5, 4)
              .HasDefaultValue(1.0m);

        entity.Property(e => e.SourceEntityType).HasMaxLength(100);
        entity.Property(e => e.SourceEntityId);
        entity.Property(e => e.DeepLink).HasMaxLength(500);
        entity.Property(e => e.RationaleJson);    // nvarchar(max) — machine-readable rationale

        // ── Sprint 13C: Impact estimation ────────────────────────────────────
        entity.Property(e => e.ImpactJson);      // nvarchar(max) — serialized RecommendationImpact

        entity.Property(e => e.ExpiresAtUtc);

        entity.Property(e => e.GeneratedAtUtc).IsRequired();
        entity.Property(e => e.AcceptedAtUtc);
        entity.Property(e => e.DismissedAtUtc);

        entity.Ignore(e => e.DomainEvents);

        // ── Indexes ───────────────────────────────────────────────────────────

        entity.HasIndex(e => e.OrganizationId)
              .HasDatabaseName("IX_CopilotRecommendations_OrganizationId");

        entity.HasIndex(e => e.Status)
              .HasDatabaseName("IX_CopilotRecommendations_Status");

        entity.HasIndex(e => e.Persona)
              .HasDatabaseName("IX_CopilotRecommendations_Persona");

        entity.HasIndex(e => e.GeneratedAtUtc)
              .HasDatabaseName("IX_CopilotRecommendations_GeneratedAtUtc");

        entity.HasIndex(e => e.Category)
              .HasDatabaseName("IX_CopilotRecommendations_Category");

        entity.HasIndex(e => e.Severity)
              .HasDatabaseName("IX_CopilotRecommendations_Severity");

        entity.HasIndex(e => new { e.OrganizationId, e.Persona, e.Status })
              .HasDatabaseName("IX_CopilotRecommendations_OrgPersonaStatus");
    }
}
