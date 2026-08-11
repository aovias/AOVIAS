using ComplianceService.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Infrastructure.Domain.Enums;

namespace ComplianceService.Infrastructure.Persistence.EntityConfigurations;

public class ComplianceAuditFindingConfiguration : IEntityTypeConfiguration<ComplianceAuditFinding>
{
    public void Configure(EntityTypeBuilder<ComplianceAuditFinding> builder)
    {
        builder.ToTable("ComplianceAuditFindings");
        builder.HasKey(x => x.Id);

        // Indexes
        builder.HasIndex(x => x.ComplianceJourneyId);
        builder.HasIndex(x => x.ObjectiveElementId);
        builder.HasIndex(x => x.IsResolved);
        builder.HasIndex(x => x.DueDate);
        builder.HasIndex(x => new { x.ComplianceJourneyId, x.IsResolved });

        // Required fields
        builder.Property(x => x.ComplianceJourneyId).IsRequired();
        builder.Property(x => x.ObjectiveElementId).IsRequired();
        builder.Property(x => x.Type)
            .IsRequired()
            .HasConversion<int>();
        builder.Property(x => x.Description)
            .IsRequired()
            .HasMaxLength(2000);
        builder.Property(x => x.DueDate).IsRequired();
        builder.Property(x => x.IsResolved)
            .IsRequired()
            .HasDefaultValue(false);

        // Optional fields
        builder.Property(x => x.ResolvedAt);

        // Navigation
        builder.HasOne(x => x.ComplianceJourney)
            .WithMany()
            .HasForeignKey(x => x.ComplianceJourneyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.ObjectiveElement)
            .WithMany()
            .HasForeignKey(x => x.ObjectiveElementId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
