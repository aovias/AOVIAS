using KnowledgeBase.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeBase.Infrastructure.Persistence;

public sealed class KnowledgeBaseContext : DbContext
{
    public KnowledgeBaseContext(DbContextOptions<KnowledgeBaseContext> options)
        : base(options)
    {
    }

    public DbSet<Framework> Frameworks => Set<Framework>();
    public DbSet<ComplianceNode> ComplianceNodes => Set<ComplianceNode>();
    public DbSet<RequirementDetail> RequirementDetails => Set<RequirementDetail>();
    public DbSet<KnowledgeItem> KnowledgeItems => Set<KnowledgeItem>();
    public DbSet<FrameworkClassification> FrameworkClassifications => Set<FrameworkClassification>();
    public DbSet<ScoringConfig> ScoringConfigs => Set<ScoringConfig>();
    public DbSet<RequirementScore> RequirementScores => Set<RequirementScore>();
    public DbSet<Assessment> Assessments => Set<Assessment>();
    public DbSet<AssessmentItem> AssessmentItems => Set<AssessmentItem>();
    public DbSet<AIInteraction> AIInteractions => Set<AIInteraction>();
    public DbSet<DecisionLog> DecisionLogs => Set<DecisionLog>();
    public DbSet<GapAnalysisReport> GapAnalysisReports => Set<GapAnalysisReport>();
    public DbSet<GapAnalysisItem> GapAnalysisItems => Set<GapAnalysisItem>();
    public DbSet<WizardQuestion> WizardQuestions => Set<WizardQuestion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Framework>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Code).HasMaxLength(64).IsRequired();
            e.Property(x => x.Name).HasMaxLength(256).IsRequired();
            e.Property(x => x.Version).HasMaxLength(64).IsRequired();
            e.HasIndex(x => x.Code);
        });

        modelBuilder.Entity<ComplianceNode>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Code).HasMaxLength(128).IsRequired();
            e.Property(x => x.Title).HasMaxLength(512).IsRequired();
            e.Property(x => x.NodeType).HasMaxLength(64).IsRequired();
            e.HasIndex(x => x.FrameworkId);
            e.HasIndex(x => x.ParentId);
            e.HasIndex(x => new { x.FrameworkId, x.ParentId });
            e.HasIndex(x => new { x.FrameworkId, x.Sequence });
            e.HasOne(x => x.Framework)
                .WithMany(x => x.Nodes)
                .HasForeignKey(x => x.FrameworkId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Parent)
                .WithMany(x => x.Children)
                .HasForeignKey(x => x.ParentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RequirementDetail>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.NodeId).IsUnique();
            e.HasOne(x => x.Node)
                .WithOne(x => x.RequirementDetail)
                .HasForeignKey<RequirementDetail>(x => x.NodeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<KnowledgeItem>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.NodeId);
            e.HasOne(x => x.Node)
                .WithMany(x => x.KnowledgeItems)
                .HasForeignKey(x => x.NodeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<FrameworkClassification>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.FrameworkId);
            e.HasOne(x => x.Framework)
                .WithMany(x => x.Classifications)
                .HasForeignKey(x => x.FrameworkId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ScoringConfig>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.FrameworkId).IsUnique();
            e.HasOne(x => x.Framework)
                .WithOne(x => x.ScoringConfig)
                .HasForeignKey<ScoringConfig>(x => x.FrameworkId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RequirementScore>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.NodeId).IsUnique();
            e.HasOne(x => x.Node)
                .WithOne(x => x.RequirementScore)
                .HasForeignKey<RequirementScore>(x => x.NodeId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Classification)
                .WithMany()
                .HasForeignKey(x => x.ClassificationId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Assessment>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.FrameworkId);
            e.HasIndex(x => x.FacilityId);
            e.HasOne(x => x.Framework)
                .WithMany()
                .HasForeignKey(x => x.FrameworkId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AssessmentItem>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.AssessmentId, x.NodeId }).IsUnique();
            e.HasOne(x => x.Assessment)
                .WithMany(x => x.Items)
                .HasForeignKey(x => x.AssessmentId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Node)
                .WithMany()
                .HasForeignKey(x => x.NodeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AIInteraction>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.FrameworkId);
            e.HasIndex(x => x.ContextNodeId);
            e.HasOne(x => x.Framework)
                .WithMany()
                .HasForeignKey(x => x.FrameworkId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.ContextNode)
                .WithMany()
                .HasForeignKey(x => x.ContextNodeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<DecisionLog>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.NodeId);
            e.HasIndex(x => x.AIInteractionId);
            e.HasOne(x => x.Framework)
                .WithMany()
                .HasForeignKey(x => x.FrameworkId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Node)
                .WithMany(x => x.DecisionLogs)
                .HasForeignKey(x => x.NodeId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.AIInteraction)
                .WithMany(x => x.DecisionLogs)
                .HasForeignKey(x => x.AIInteractionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<GapAnalysisReport>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.AssessmentId).IsUnique();
            e.HasIndex(x => x.FrameworkId);
            e.HasIndex(x => x.FacilityId);
            e.HasOne(x => x.Assessment)
                .WithMany()
                .HasForeignKey(x => x.AssessmentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<GapAnalysisItem>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.NodeCode).HasMaxLength(128).IsRequired();
            e.Property(x => x.NodeTitle).HasMaxLength(512).IsRequired();
            e.Property(x => x.ComplianceStatus).HasMaxLength(64).IsRequired();
            e.Property(x => x.Classification).HasMaxLength(128).IsRequired();
            e.Property(x => x.Category).HasMaxLength(32).IsRequired();
            e.HasIndex(x => new { x.ReportId, x.NodeId }).IsUnique();
            e.HasOne(x => x.Report)
                .WithMany(x => x.Items)
                .HasForeignKey(x => x.ReportId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Node)
                .WithMany()
                .HasForeignKey(x => x.NodeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<WizardQuestion>(e =>
        {
            e.HasKey(x => x.Id);

            // Constrained-length columns
            e.Property(x => x.FrameworkVersion).HasMaxLength(64).IsRequired();
            e.Property(x => x.QuestionTitle).HasMaxLength(512).IsRequired();
            e.Property(x => x.Category).HasMaxLength(128).IsRequired();
            e.Property(x => x.WeightBand).HasMaxLength(16).IsRequired();
            e.Property(x => x.RiskCategory).HasMaxLength(64).IsRequired();
            e.Property(x => x.CreatedBy).HasMaxLength(128).IsRequired();
            e.Property(x => x.UpdatedBy).HasMaxLength(128);

            // Weight / maturity numeric types
            e.Property(x => x.Weight).HasColumnType("decimal(18,4)");
            e.Property(x => x.UnlockThreshold).HasColumnType("decimal(5,2)");

            // Maturity fields
            e.Property(x => x.AssessmentMaturityLevel).HasMaxLength(32).IsRequired();
            e.Property(x => x.AssessmentPhase).HasMaxLength(32).IsRequired();

            // Idempotency: one active row per OE per version
            e.HasIndex(x => new { x.ObjectiveElementId, x.Version }).IsUnique();

            // Query indexes
            e.HasIndex(x => new { x.FrameworkId, x.IsActive });
            e.HasIndex(x => new { x.FrameworkId, x.AssessmentMaturityLevel, x.IsActive });
            e.HasIndex(x => new { x.ChapterId, x.DisplayOrder });
            e.HasIndex(x => new { x.Category, x.IsActive });
            e.HasIndex(x => x.StandardId);

            // FK: Framework (Restrict — wizard questions must not vanish if framework changes)
            e.HasOne(x => x.Framework)
                .WithMany()
                .HasForeignKey(x => x.FrameworkId)
                .OnDelete(DeleteBehavior.Restrict);

            // FK: ObjectiveElement node
            e.HasOne(x => x.ObjectiveElement)
                .WithMany()
                .HasForeignKey(x => x.ObjectiveElementId)
                .OnDelete(DeleteBehavior.Restrict);

            // FK: Chapter node
            e.HasOne(x => x.Chapter)
                .WithMany()
                .HasForeignKey(x => x.ChapterId)
                .OnDelete(DeleteBehavior.Restrict);

            // FK: Standard node
            e.HasOne(x => x.Standard)
                .WithMany()
                .HasForeignKey(x => x.StandardId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
