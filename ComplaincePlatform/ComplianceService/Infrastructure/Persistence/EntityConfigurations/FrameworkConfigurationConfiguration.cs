using ComplianceService.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ComplianceService.Infrastructure.Persistence.EntityConfigurations;

public class FrameworkConfigurationConfiguration : IEntityTypeConfiguration<FrameworkConfiguration>
{
    public void Configure(EntityTypeBuilder<FrameworkConfiguration> builder)
    {
        builder.ToTable("FrameworkConfigurations");
        builder.HasKey(x => x.Id);

        // Unique constraint - one configuration per framework
        builder.HasIndex(x => x.FrameworkId).IsUnique();

        // Required fields (inherits PlatformEntity.PlatformId)
        builder.Property(x => x.PlatformId)
            .IsRequired()
            .HasMaxLength(10);
        builder.Property(x => x.FrameworkId).IsRequired();
        builder.Property(x => x.DefaultValidityMonths).IsRequired();
        builder.Property(x => x.MinimumReadinessScore)
            .HasColumnType("decimal(5,2)")
            .IsRequired();
        builder.Property(x => x.RenewalDaysBeforeExpiry)
            .IsRequired()
            .HasDefaultValue(90);
        builder.Property(x => x.ReapplyWaitingDays)
            .IsRequired()
            .HasDefaultValue(30);
        builder.Property(x => x.MajorNcResolutionDays)
            .IsRequired()
            .HasDefaultValue(60);

        // Navigation
        builder.HasOne(x => x.Framework)
            .WithMany()
            .HasForeignKey(x => x.FrameworkId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
