using AIOrchestratorService.Domain;
using AIOrchestratorService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Domain;
using Shared.Infrastructure.EntityFrameworkCore;

namespace AIOrchestratorService.Infrastructure.Persistence;

public class AIOrchestratorDbContext : DbContext
{
    public AIOrchestratorDbContext(DbContextOptions<AIOrchestratorDbContext> options)
        : base(options) { }

    public DbSet<DraftArtifact> DraftArtifacts => Set<DraftArtifact>();
    public DbSet<AIAuditEntry> AIAuditEntries => Set<AIAuditEntry>();
    public DbSet<AgentExecution> AgentExecutions => Set<AgentExecution>();
    public DbSet<AgentExecutionStep> AgentExecutionSteps => Set<AgentExecutionStep>();
    public DbSet<AgentExecutionLog> AgentExecutionLogs => Set<AgentExecutionLog>();
    public DbSet<AIAssistantProfile> AIAssistantProfiles => Set<AIAssistantProfile>();
    public DbSet<AIIdentity> AIIdentities => Set<AIIdentity>();
    public DbSet<AIIdentityScopeAssignment> AIIdentityScopeAssignments => Set<AIIdentityScopeAssignment>();
    public DbSet<AIProfilePermissionGrant> AIProfilePermissionGrants => Set<AIProfilePermissionGrant>();
    public DbSet<AIAutomationPolicy> AIAutomationPolicies => Set<AIAutomationPolicy>();
    public DbSet<AIApprovalPolicy> AIApprovalPolicies => Set<AIApprovalPolicy>();
    public DbSet<AIPromptProfile> AIPromptProfiles => Set<AIPromptProfile>();
    public DbSet<AIPromptVersion> AIPromptVersions => Set<AIPromptVersion>();
    public DbSet<AIConversationContext> AIConversationContexts => Set<AIConversationContext>();
    public DbSet<AIConversationMessage> AIConversationMessages => Set<AIConversationMessage>();
    public DbSet<AIExecutionContext> AIExecutionContexts => Set<AIExecutionContext>();
    public DbSet<AIExecutionAudit> AIExecutionAudits => Set<AIExecutionAudit>();
    public DbSet<AIUsageStatistic> AIUsageStatistics => Set<AIUsageStatistic>();
    public DbSet<AIApprovalRequest> AIApprovalRequests => Set<AIApprovalRequest>();
    public DbSet<AIOutboxMessage> AIOutboxMessages => Set<AIOutboxMessage>();
    public DbSet<KnowledgeEmbeddingChunk> KnowledgeEmbeddingChunks => Set<KnowledgeEmbeddingChunk>();
    public DbSet<AceInsight> AceInsights => Set<AceInsight>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // DraftArtifact configuration
        builder.Entity<DraftArtifact>(entity =>
        {
            entity.ToTable("DraftArtifacts");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.RowVersion)
                  .IsRowVersion();

            entity.Property(x => x.DraftType)
                  .IsRequired()
                  .HasConversion<string>();

            entity.Property(x => x.SourceEntityId)
                  .IsRequired();

            entity.Property(x => x.Content)
                  .IsRequired();

            entity.Property(x => x.Explainability)
                  .ConfigureExplainability();

            entity.Property(x => x.GeneratedBy)
                  .IsRequired()
                  .HasMaxLength(100);

            entity.Property(x => x.Status)
                  .IsRequired()
                  .HasConversion<string>();

            entity.Property(x => x.GeneratedAt)
                  .IsRequired();

            entity.Property(x => x.CreatedAt)
                  .IsRequired();

            // Indexes for common queries
            entity.HasIndex(x => x.SourceEntityId);
            entity.HasIndex(x => x.DraftType);
            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => new { x.SourceEntityId, x.DraftType });
        });

        builder.Entity<AIAuditEntry>(entity =>
        {
            entity.ToTable("AIAuditEntries");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.UseCase).IsRequired().HasMaxLength(100);
            entity.Property(x => x.PromptTemplateId).IsRequired().HasMaxLength(100);
            entity.Property(x => x.PromptTemplateVersion).IsRequired().HasMaxLength(30);
            entity.Property(x => x.ContextVersionId).IsRequired().HasMaxLength(128);
            entity.Property(x => x.ValidationOutcome).IsRequired().HasMaxLength(20);
            entity.Property(x => x.DecisionPath).IsRequired().HasMaxLength(256);
            entity.Property(x => x.SourceReferencesJson).IsRequired();
            entity.Property(x => x.ModelVersion).HasMaxLength(64);
            entity.HasIndex(x => new { x.HospitalId, x.OccurredAtUtc });
        });

        builder.Entity<AgentExecution>(entity =>
        {
            entity.ToTable("AgentExecutions");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.WorkflowTemplateId).IsRequired().HasMaxLength(128);
            entity.Property(x => x.WorkflowTemplateVersion).IsRequired().HasMaxLength(30);
            entity.Property(x => x.Goal).IsRequired().HasMaxLength(1000);
            entity.Property(x => x.ContextVersionId).IsRequired().HasMaxLength(128);
            entity.Property(x => x.Status).HasConversion<int>().IsRequired();
            entity.Property(x => x.FailureType).HasConversion<int>().IsRequired();
            entity.Property(x => x.CurrentStepId).HasMaxLength(128);
            entity.Property(x => x.CompletedStepIdsJson).IsRequired();
            entity.Property(x => x.PendingStepIdsJson).IsRequired();
            entity.Property(x => x.FailureReason).HasMaxLength(2000);
            entity.Property(x => x.ApprovalReason).HasMaxLength(1000);
            entity.HasIndex(x => new { x.TenantId, x.Status, x.UpdatedAtUtc });
        });

        builder.Entity<AgentExecutionStep>(entity =>
        {
            entity.ToTable("AgentExecutionSteps");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.StepId).IsRequired().HasMaxLength(128);
            entity.Property(x => x.ToolName).IsRequired().HasMaxLength(128);
            entity.Property(x => x.Status).HasConversion<int>().IsRequired();
            entity.Property(x => x.FailureType).HasConversion<int>().IsRequired();
            entity.Property(x => x.DependsOnJson).IsRequired();
            entity.Property(x => x.InputJson).IsRequired();
            entity.Property(x => x.OutputJson).IsRequired();
            entity.Property(x => x.ApprovalReason).HasMaxLength(1000);
            entity.Property(x => x.FailureReason).HasMaxLength(2000);
            entity.HasIndex(x => new { x.ExecutionId, x.Sequence }).IsUnique();
            entity.HasIndex(x => new { x.ExecutionId, x.StepId }).IsUnique();
        });

        builder.Entity<AgentExecutionLog>(entity =>
        {
            entity.ToTable("AgentExecutionLogs");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.EventType).IsRequired().HasMaxLength(128);
            entity.Property(x => x.Message).IsRequired().HasMaxLength(2000);
            entity.Property(x => x.MetadataJson).IsRequired();
            entity.HasIndex(x => new { x.ExecutionId, x.OccurredAtUtc });
        });

        builder.Entity<AIAssistantProfile>(e => { e.ToTable("AIAssistantProfiles"); e.HasKey(x => x.Id); e.HasIndex(x => x.Code).IsUnique(); });
        builder.Entity<AIIdentity>(e => { e.ToTable("AIIdentities"); e.HasKey(x => x.Id); e.HasIndex(x => x.PlatformId).IsUnique(); });
        builder.Entity<AIIdentityScopeAssignment>(e => { e.ToTable("AIIdentityScopeAssignments"); e.HasKey(x => x.Id); });
        builder.Entity<AIProfilePermissionGrant>(e => { e.ToTable("AIProfilePermissionGrants"); e.HasKey(x => x.Id); });
        builder.Entity<AIAutomationPolicy>(e => { e.ToTable("AIAutomationPolicies"); e.HasKey(x => x.Id); });
        builder.Entity<AIApprovalPolicy>(e => { e.ToTable("AIApprovalPolicies"); e.HasKey(x => x.Id); });
        builder.Entity<AIPromptProfile>(e => { e.ToTable("AIPromptProfiles"); e.HasKey(x => x.Id); e.HasIndex(x => x.Code); });
        builder.Entity<AIPromptVersion>(e => { e.ToTable("AIPromptVersions"); e.HasKey(x => x.Id); e.HasIndex(x => new { x.PromptProfileId, x.Version }).IsUnique(); });
        builder.Entity<AIConversationContext>(e => { e.ToTable("AIConversationContexts"); e.HasKey(x => x.Id); });
        builder.Entity<AIConversationMessage>(e => { e.ToTable("AIConversationMessages"); e.HasKey(x => x.Id); e.HasIndex(x => new { x.ConversationId, x.Sequence }); });
        builder.Entity<AIExecutionContext>(e => { e.ToTable("AIExecutionContexts"); e.HasKey(x => x.Id); });
        builder.Entity<AIExecutionAudit>(e => { e.ToTable("AIExecutionAudits"); e.HasKey(x => x.Id); e.HasIndex(x => new { x.ProfileId, x.OccurredAtUtc }); e.HasIndex(x => x.TraceId); });
        builder.Entity<AIUsageStatistic>(e => { e.ToTable("AIUsageStatistics"); e.HasKey(x => x.Id); });
        builder.Entity<AIApprovalRequest>(e => { e.ToTable("AIApprovalRequests"); e.HasKey(x => x.Id); e.HasIndex(x => x.Status); });
        builder.Entity<AIOutboxMessage>(e => { e.ToTable("AIOutboxMessages"); e.HasKey(x => x.Id); e.HasIndex(x => x.ProcessedOn); });

        builder.Entity<KnowledgeEmbeddingChunk>(entity =>
        {
            entity.ToTable("KnowledgeEmbeddingChunks");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.SourceType).IsRequired().HasMaxLength(64);
            entity.Property(x => x.SourceId).IsRequired().HasMaxLength(128);
            entity.Property(x => x.Title).IsRequired().HasMaxLength(512);
            entity.Property(x => x.Content).IsRequired();
            entity.Property(x => x.EmbeddingJson).IsRequired();
            entity.Property(x => x.EmbeddingModel).IsRequired().HasMaxLength(64);
            entity.Property(x => x.DocumentId).HasMaxLength(128);
            entity.Property(x => x.Version).HasMaxLength(64);
            entity.HasIndex(x => new { x.HospitalId, x.SourceType, x.SourceId }).IsUnique();
            entity.HasIndex(x => new { x.HospitalId, x.FrameworkId });
        });

        builder.Entity<AceInsight>(entity =>
        {
            entity.ToTable("AceInsights");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Category).IsRequired().HasMaxLength(64);
            entity.Property(x => x.Title).IsRequired().HasMaxLength(512);
            entity.Property(x => x.Summary).IsRequired();
            entity.Property(x => x.Reasoning).IsRequired();
            entity.Property(x => x.Status).IsRequired().HasMaxLength(32);
            entity.Property(x => x.CitationsJson).IsRequired();
            entity.Property(x => x.SuggestedActionsJson).IsRequired();
            entity.HasIndex(x => new { x.HospitalId, x.Status, x.Priority });
            entity.HasIndex(x => x.CreatedAtUtc);
        });
    }
}
