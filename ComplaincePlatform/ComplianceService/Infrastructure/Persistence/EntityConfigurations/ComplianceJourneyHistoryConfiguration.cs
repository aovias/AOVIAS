using ComplianceService.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ComplianceService.Infrastructure.Persistence.EntityConfigurations;

public class ComplianceJourneyHistoryConfiguration : IEntityTypeConfiguration<ComplianceJourneyHistory>
{
    public void Configure(EntityTypeBuilder<ComplianceJourneyHistory> builder)
    {
        builder.ToTable("ComplianceJourneyHistories");
        builder.HasKey(x => x.Id);

        // Indexes for audit trail queries
        builder.HasIndex(x => x.ComplianceJourneyId);
        builder.HasIndex(x => x.ChangedAt);
        builder.HasIndex(x => x.ChangedBy);
        builder.HasIndex(x => new { x.ComplianceJourneyId, x.ChangedAt });

        // Required fields
        builder.Property(x => x.ComplianceJourneyId).IsRequired();
        builder.Property(x => x.OldStatus)
            .IsRequired()
            .HasConversion<int>();
        builder.Property(x => x.NewStatus)
            .IsRequired()
            .HasConversion<int>();
        builder.Property(x => x.ChangedBy).IsRequired();
        builder.Property(x => x.ChangedAt).IsRequired();
        builder.Property(x => x.SnapshotJson)
            .IsRequired()
            .HasColumnType("nvarchar(max)");

        // Optional fields
        builder.Property(x => x.Remarks).HasMaxLength(2000);

        // Navigation
        builder.HasOne(x => x.ComplianceJourney)
            .WithMany()
            .HasForeignKey(x => x.ComplianceJourneyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
