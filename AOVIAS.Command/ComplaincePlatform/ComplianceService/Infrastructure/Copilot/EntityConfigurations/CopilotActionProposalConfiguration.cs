using ComplianceService.Domain.Copilot;
using ComplianceService.Domain.Copilot.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ComplianceService.Infrastructure.Copilot.EntityConfigurations;

public sealed class CopilotActionProposalConfiguration : IEntityTypeConfiguration<CopilotActionProposal>
{
    public void Configure(EntityTypeBuilder<CopilotActionProposal> entity)
    {
        entity.ToTable("CopilotActionProposals");

        entity.HasKey(e => e.Id);

        entity.Property(e => e.Id)
              .IsRequired()
              .ValueGeneratedNever();

        entity.Property(e => e.OrganizationId)
              .IsRequired();

        entity.Property(e => e.RecommendationId)
              .IsRequired();

        entity.Property(e => e.DraftId)
              .IsRequired(false);

        entity.Property(e => e.ProposalType)
              .HasConversion<int>()
              .HasDefaultValue(ProposalType.CreateTask)
              .IsRequired();

        entity.Property(e => e.Title)
              .HasMaxLength(250)
              .IsRequired();

        entity.Property(e => e.Description)
              .HasMaxLength(4000)
              .HasDefaultValue(string.Empty)
              .IsRequired(false);

        entity.Property(e => e.PayloadJson)
              .HasColumnType("nvarchar(max)")
              .HasDefaultValue("{}")
              .IsRequired(false);

        entity.Property(e => e.ProposedBy)
              .HasMaxLength(100)
              .HasDefaultValue("Copilot")
              .IsRequired();

        entity.Property(e => e.ProposedAtUtc)
              .IsRequired();

        entity.Property(e => e.ExpiresAtUtc)
              .IsRequired();

        entity.Property(e => e.Status)
              .HasConversion<int>()
              .HasDefaultValue(ProposalStatus.Proposed)
              .IsRequired();

        entity.Property(e => e.ApprovedBy)
              .IsRequired(false);

        entity.Property(e => e.ApprovedAtUtc)
              .IsRequired(false);

        entity.Property(e => e.RejectedBy)
              .IsRequired(false);

        entity.Property(e => e.RejectedAtUtc)
              .IsRequired(false);

        entity.Property(e => e.RejectionReason)
              .HasMaxLength(2000)
              .IsRequired(false);

        entity.Property(e => e.ResultEntityId)
              .IsRequired(false);

        entity.Property(e => e.ResultEntityType)
              .HasMaxLength(100)
              .IsRequired(false);

        entity.Property(e => e.ExecutionNotes)
              .HasMaxLength(2000)
              .IsRequired(false);

        entity.Property(e => e.ExecutedAtUtc)
              .IsRequired(false);

        entity.Property(e => e.ExecutedBy)
              .IsRequired(false);

        // ── Indexes ───────────────────────────────────────────────────────────

        entity.HasIndex(e => e.OrganizationId)
              .HasDatabaseName("IX_CopilotActionProposals_OrganizationId");

        entity.HasIndex(e => e.Status)
              .HasDatabaseName("IX_CopilotActionProposals_Status");

        entity.HasIndex(e => e.RecommendationId)
              .HasDatabaseName("IX_CopilotActionProposals_RecommendationId");

        entity.HasIndex(e => e.ProposalType)
              .HasDatabaseName("IX_CopilotActionProposals_ProposalType");

        entity.HasIndex(e => e.ExpiresAtUtc)
              .HasDatabaseName("IX_CopilotActionProposals_ExpiresAtUtc");

        entity.HasIndex(new[] { nameof(CopilotActionProposal.OrganizationId), nameof(CopilotActionProposal.Status) })
              .HasDatabaseName("IX_CopilotActionProposals_OrgId_Status");
    }
}
