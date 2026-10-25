using ComplianceService.Domain;
using ComplianceService.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ComplianceService.Infrastructure.Persistence.EntityConfigurations;

public class OrganizationComplianceObligationConfiguration
    : IEntityTypeConfiguration<OrganizationComplianceObligation>
{
    public void Configure(EntityTypeBuilder<OrganizationComplianceObligation> builder)
    {
        builder.ToTable("OrganizationComplianceObligations");

        builder.HasKey(x => x.Id);

        // ─── Multi-tenant scoping ────────────────────────────────────────────
        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.OrganizationId).IsRequired();
        builder.Property(x => x.HospitalId).IsRequired();

        // ─── Compliance hierarchy references ─────────────────────────────────
        builder.Property(x => x.FrameworkId).IsRequired();
        builder.Property(x => x.ChapterId).IsRequired();
        builder.Property(x => x.StandardId).IsRequired();
        builder.Property(x => x.ObjectiveElementId).IsRequired();
        builder.Property(x => x.ComplianceNodeId); // nullable

        // ─── Denormalized display data ────────────────────────────────────────
        builder.Property(x => x.Code)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.Title)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.Description)
            .HasMaxLength(2000);

        builder.Property(x => x.MaturityLevel)
            .IsRequired()
            .HasConversion<int>()
            .HasDefaultValue(ObjectiveElementClassification.Core);

        builder.Property(x => x.RiskCategory)
            .HasMaxLength(100);

        builder.Property(x => x.Weight)
            .IsRequired()
            .HasColumnType("decimal(18,4)")
            .HasDefaultValue(1.0m);

        // ─── Operational status enums ─────────────────────────────────────────
        builder.Property(x => x.ImplementationStatus)
            .IsRequired()
            .HasConversion<int>()
            .HasDefaultValue(ImplementationStatus.Unknown);

        builder.Property(x => x.AssessmentStatus)
            .IsRequired()
            .HasConversion<int>()
            .HasDefaultValue(ObligationAssessmentStatus.NotAssessed);

        builder.Property(x => x.JourneyStatus)
            .IsRequired()
            .HasConversion<int>()
            .HasDefaultValue(ObligationJourneyStatus.NotStarted);

        builder.Property(x => x.EvidenceStatus)
            .IsRequired()
            .HasConversion<int>()
            .HasDefaultValue(EvidenceStatus.Missing);

        builder.Property(x => x.VerificationStatus)
            .IsRequired()
            .HasConversion<int>()
            .HasDefaultValue(VerificationStatus.NotVerified);

        // ─── Computed scores ──────────────────────────────────────────────────
        builder.Property(x => x.ReadinessScore)
            .IsRequired()
            .HasColumnType("decimal(5,2)")
            .HasDefaultValue(0m);

        builder.Property(x => x.CompletionPercentage)
            .IsRequired()
            .HasColumnType("decimal(5,2)")
            .HasDefaultValue(0m);

        // ─── Timestamps ───────────────────────────────────────────────────────
        builder.Property(x => x.LastAssessmentDate);
        builder.Property(x => x.LastEvidenceDate);
        builder.Property(x => x.LastVerificationDate);
        builder.Property(x => x.LastSnapshotRefreshDate);
        builder.Property(x => x.UpdatedAt).IsRequired();

        // ─── Audit ────────────────────────────────────────────────────────────
        builder.Property(x => x.CreatedBy).HasMaxLength(256);
        builder.Property(x => x.UpdatedBy).HasMaxLength(256);

        // BaseEntity fields
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.RowVersion).IsRowVersion().IsRequired();

        // Canonical business key: one obligation per (Hospital × Framework × ObjectiveElement).
        builder.HasIndex(x => new { x.HospitalId, x.FrameworkId, x.ObjectiveElementId })
            .IsUnique()
            .HasDatabaseName("UX_OrganizationComplianceObligations_HospitalFrameworkOE");

        // ─── Indexes for common queries ───────────────────────────────────────
        // Tenant isolation — used by global query filter.
        builder.HasIndex(x => x.TenantId)
            .HasDatabaseName("IX_OrganizationComplianceObligations_TenantId");

        // List by org + framework (primary list query).
        builder.HasIndex(x => new { x.TenantId, x.HospitalId, x.FrameworkId })
            .HasDatabaseName("IX_OrganizationComplianceObligations_Tenant_Hospital_Framework");

        // Maturity dashboard aggregation.
        builder.HasIndex(x => new { x.OrganizationId, x.FrameworkId, x.MaturityLevel })
            .HasDatabaseName("IX_OrganizationComplianceObligations_Org_Framework_Maturity");

        // Status filter queries.
        builder.HasIndex(x => new { x.OrganizationId, x.ImplementationStatus })
            .HasDatabaseName("IX_OrganizationComplianceObligations_Org_ImplementationStatus");

        // ─── Foreign keys ─────────────────────────────────────────────────────
        // Restrict delete to prevent accidental cascade destroying obligation history.
        builder.HasOne(x => x.Framework)
            .WithMany()
            .HasForeignKey(x => x.FrameworkId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ObjectiveElement)
            .WithMany()
            .HasForeignKey(x => x.ObjectiveElementId)
            .OnDelete(DeleteBehavior.Restrict);

        // No FK to KnowledgeBase (cross-DB boundary — ComplianceNodeId is nullable reference only).
    }
}
