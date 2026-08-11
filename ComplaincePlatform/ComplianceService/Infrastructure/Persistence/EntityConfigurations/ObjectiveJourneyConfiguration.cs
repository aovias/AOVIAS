using ComplianceService.Domain;
using ComplianceService.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ComplianceService.Infrastructure.Persistence.EntityConfigurations;

public class ObjectiveJourneyConfiguration : IEntityTypeConfiguration<ObjectiveJourney>
{
    public void Configure(EntityTypeBuilder<ObjectiveJourney> builder)
    {
        builder.ToTable("ObjectiveJourneys");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.WizardPayloadJson)
            .IsRequired()
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.CurrentStepIndex).IsRequired();

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<int>()
            .HasDefaultValue(ObjectiveJourneyStatus.Draft);

        builder.Property(x => x.LastUpdatedAt).IsRequired();

        builder.Property(x => x.RowVersion)
            .IsRowVersion()
            .IsRequired();

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.HospitalId).IsRequired();
        builder.Property(x => x.ComplianceJourneyId).IsRequired();
        builder.Property(x => x.ObjectiveElementId).IsRequired();

        builder.HasIndex(x => new { x.ComplianceJourneyId, x.ObjectiveElementId })
            .IsUnique();

        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.HospitalId);

        builder.HasOne<ComplianceJourney>()
            .WithMany()
            .HasForeignKey(x => x.ComplianceJourneyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<ObjectiveElement>()
            .WithMany()
            .HasForeignKey(x => x.ObjectiveElementId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
