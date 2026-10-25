using ComplianceService.Domain;
using ComplianceService.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ComplianceService.Infrastructure.Persistence.EntityConfigurations;

public class ComplianceDeficiencyConfiguration : IEntityTypeConfiguration<ComplianceDeficiency>
{
    public void Configure(EntityTypeBuilder<ComplianceDeficiency> entity)
    {
        entity.ToTable("ComplianceDeficiencies");

        entity.HasKey(e => e.Id);

        entity.Property(e => e.Id).ValueGeneratedNever();

        entity.Property(e => e.RowVersion)
              .IsRowVersion()
              .IsConcurrencyToken();

        entity.Property(e => e.DeficiencyCode)
              .IsRequired()
              .HasMaxLength(50);

        entity.Property(e => e.Title)
              .IsRequired()
              .HasMaxLength(500);

        entity.Property(e => e.Description)
              .HasMaxLength(2000);

        entity.Property(e => e.DeficiencyType)
              .IsRequired()
              .HasConversion<int>();

        entity.Property(e => e.Severity)
              .IsRequired()
              .HasConversion<int>();

        entity.Property(e => e.Status)
              .IsRequired()
              .HasConversion<int>();

        entity.Property(e => e.MaturityLevel)
              .IsRequired()
              .HasConversion<int>();

        entity.Property(e => e.RootCauseCategory)
              .HasConversion<int?>();

        entity.Property(e => e.ProblemStatement).HasMaxLength(4000);
        entity.Property(e => e.RootCauseSummary).HasMaxLength(2000);
        entity.Property(e => e.ImpactDescription).HasMaxLength(2000);
        entity.Property(e => e.CorrectiveActionSummary).HasMaxLength(2000);
        entity.Property(e => e.PreventiveActionSummary).HasMaxLength(2000);
        entity.Property(e => e.Why1).HasMaxLength(1000);
        entity.Property(e => e.Why2).HasMaxLength(1000);
        entity.Property(e => e.Why3).HasMaxLength(1000);
        entity.Property(e => e.Why4).HasMaxLength(1000);
        entity.Property(e => e.Why5).HasMaxLength(1000);
        entity.Property(e => e.FinalRootCause).HasMaxLength(2000);
        entity.Property(e => e.Department).HasMaxLength(100);
        entity.Property(e => e.ActionPlan).HasMaxLength(4000);
        entity.Property(e => e.ExpectedOutcome).HasMaxLength(2000);
        entity.Property(e => e.SuccessCriteria).HasMaxLength(2000);
        entity.Property(e => e.AssessedRiskLevel).HasConversion<int?>();
        entity.Property(e => e.EffectivenessReviewStatus).HasConversion<int>();

        entity.Property(e => e.RiskCategory).HasMaxLength(200);
        entity.Property(e => e.AssignedTo).HasMaxLength(500);
        entity.Property(e => e.Source).IsRequired().HasMaxLength(100);
        entity.Property(e => e.OriginType).HasConversion<int?>();
        entity.Property(e => e.OriginId);
        entity.Property(e => e.OriginReference).HasMaxLength(64);
        entity.Property(e => e.PromotedAt);
        entity.Property(e => e.CreatedBy).HasMaxLength(500);
        entity.Property(e => e.UpdatedBy).HasMaxLength(500);

        entity.Property(e => e.CurrentSlaStatus).HasConversion<int?>();
        entity.Property(e => e.CurrentEscalationState).HasConversion<int?>();
        entity.HasIndex(e => e.CurrentSlaStatus).HasDatabaseName("IX_ComplianceDeficiencies_CurrentSlaStatus");

        entity.Property(e => e.CapaNumber).HasMaxLength(40);
        entity.Property(e => e.OwnerAcceptedBy).HasMaxLength(500);
        entity.Property(e => e.OwnerRejectedBy).HasMaxLength(500);
        entity.Property(e => e.OwnershipRejectionReason).HasMaxLength(2000);
        entity.Property(e => e.LinkedSourceType).HasMaxLength(100);

        entity.Property(e => e.CorrectiveActionOwner).HasMaxLength(64);
        entity.Property(e => e.PreventiveActionOwner).HasMaxLength(64);
        entity.Property(e => e.PreventiveActionSuccessCriteria).HasMaxLength(2000);
        entity.Property(e => e.EffectivenessReviewer).HasMaxLength(64);
        entity.Property(e => e.EffectivenessReviewNotes).HasMaxLength(4000);
        entity.Property(e => e.EffectivenessReviewOutcome).HasMaxLength(500);
        entity.Property(e => e.RecurrenceReason).HasMaxLength(500);
        entity.Property(e => e.CorrectiveActionStatus).HasConversion<int>();
        entity.Property(e => e.PreventiveActionStatus).HasConversion<int>();
        entity.HasIndex(e => e.IsRecurringIssue).HasDatabaseName("IX_ComplianceDeficiencies_IsRecurringIssue");

        entity.HasIndex(e => new { e.OrganizationId, e.CapaNumber })
              .IsUnique()
              .HasDatabaseName("IX_ComplianceDeficiencies_OrganizationId_CapaNumber")
              .HasFilter("[CapaNumber] IS NOT NULL");

        entity.Property(e => e.DetectedDate).IsRequired();
        entity.Property(e => e.LastEvaluatedDate).IsRequired();
        entity.Property(e => e.CreatedAt).IsRequired();
        entity.Property(e => e.UpdatedAt).IsRequired();

        // Ignore aggregate domain events — not persisted.
        entity.Ignore(e => e.DomainEvents);

        // ── Indexes ───────────────────────────────────────────────────────────

        entity.HasIndex(e => new { e.HospitalId, e.FrameworkId })
              .HasDatabaseName("IX_ComplianceDeficiencies_HospitalId_FrameworkId");

        entity.HasIndex(e => e.Status)
              .HasDatabaseName("IX_ComplianceDeficiencies_Status");

        entity.HasIndex(e => e.Severity)
              .HasDatabaseName("IX_ComplianceDeficiencies_Severity");

        // Exclude Closed/Cancelled; allow multiple open CAPAs when OriginType = AuditFinding (2).
        entity.HasIndex(e => new { e.OrganizationId, e.ComplianceObligationId, e.DeficiencyType })
              .HasDatabaseName("IX_ComplianceDeficiencies_UniqueOpenPerObligationType")
              // SQL Server filtered indexes cannot use OR/ISNULL for OriginType=AuditFinding exclusion.
              .HasFilter("[Status] <> 6 AND [Status] <> 7");
    }
}
