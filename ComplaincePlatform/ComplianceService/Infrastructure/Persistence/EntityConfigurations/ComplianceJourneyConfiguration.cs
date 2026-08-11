using ComplianceService.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Infrastructure.Domain.Enums;

namespace ComplianceService.Infrastructure.Persistence.EntityConfigurations;

public class ComplianceJourneyConfiguration : IEntityTypeConfiguration<ComplianceJourney>
{
    public void Configure(EntityTypeBuilder<ComplianceJourney> builder)
    {
        builder.ToTable("ComplianceJourneys");
        builder.HasKey(x => x.Id);

        // Multi-tenant indexes
        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.OrganizationId);
        builder.HasIndex(x => x.HospitalId);
        builder.HasIndex(x => x.FrameworkId);
        builder.HasIndex(x => new { x.HospitalId, x.FrameworkId }).IsUnique(); // One journey per hospital-framework

        // Status index for queries
        builder.HasIndex(x => x.Status);

        // Dates for expiry/renewal queries
        builder.HasIndex(x => x.ExpiryDate);
        builder.HasIndex(x => x.RenewalWindowStartDate);

        // Concurrency token
        builder.Property(x => x.RowVersion)
            .IsRowVersion()
            .IsRequired();

        // Required fields
        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.OrganizationId).IsRequired();
        builder.Property(x => x.HospitalId).IsRequired();
        builder.Property(x => x.FrameworkId).IsRequired();
        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<int>()
            .HasDefaultValue(ComplianceLifecycleState.Draft);

        builder.Property(x => x.OperationalMode)
            .IsRequired()
            .HasConversion<int>()
            .HasDefaultValue(OperationalMode.SelfAssessment);

        // Decimal precision
        builder.Property(x => x.MinimumRequiredScore)
            .HasColumnType("decimal(5,2)")
            .IsRequired();
        builder.Property(x => x.CurrentReadinessScore)
            .HasColumnType("decimal(5,2)")
            .IsRequired()
            .HasDefaultValue(0m);
        builder.Property(x => x.RiskScore)
            .HasColumnType("decimal(5,2)")
            .IsRequired()
            .HasDefaultValue(0m);

        // Integer fields
        builder.Property(x => x.ValidityPeriodInMonths).IsRequired();

        // Audit metadata
        builder.Property(x => x.AuditDate);
        builder.Property(x => x.AuditTeamDetails)
            .HasMaxLength(1000);
        builder.Property(x => x.CorrectiveActionDeadline);

        // Financial fields
        builder.Property(x => x.EnrollmentPaymentId);
        builder.Property(x => x.EnrollmentFeeAmount)
            .HasColumnType("decimal(18,2)");
        builder.Property(x => x.EnrollmentFeePaidOn);

        // String fields
        builder.Property(x => x.ExternalReferenceId).HasMaxLength(100);
        builder.Property(x => x.CertificateNumber).HasMaxLength(50);
        builder.Property(x => x.SuspendedReason).HasMaxLength(1000);
        builder.Property(x => x.RejectionReason).HasMaxLength(1000);

        // Boolean fields
        builder.Property(x => x.IsPublicCertificateVisible)
            .IsRequired()
            .HasDefaultValue(false);
        builder.Property(x => x.IsLocked)
            .IsRequired()
            .HasDefaultValue(false);

        // Timestamps
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();

        // Navigation to Framework
        builder.HasOne(x => x.Framework)
            .WithMany()
            .HasForeignKey(x => x.FrameworkId)
            .OnDelete(DeleteBehavior.Restrict);

        // Prevent direct status updates (enforce through domain method)
        builder.Ignore(x => x.DomainEvents);
    }
}
