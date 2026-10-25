using GovernanceService.Domain.Aggregates;
using GovernanceService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Domain;

namespace GovernanceService.Infrastructure.Persistence;

public class GovernanceDbContext : DbContext
{
    public GovernanceDbContext(DbContextOptions<GovernanceDbContext> options) : base(options) { }

    public DbSet<Responsibility> Responsibilities => Set<Responsibility>();
    public DbSet<GovernanceAssignment> GovernanceAssignments => Set<GovernanceAssignment>();
    public DbSet<Committee> Committees => Set<Committee>();
    public DbSet<CommitteeMembership> CommitteeMemberships => Set<CommitteeMembership>();
    public DbSet<CommitteeMeeting> CommitteeMeetings => Set<CommitteeMeeting>();
    public DbSet<CommitteeDecision> CommitteeDecisions => Set<CommitteeDecision>();
    public DbSet<CommitteeActionItem> CommitteeActionItems => Set<CommitteeActionItem>();
    public DbSet<AgendaTemplate> AgendaTemplates => Set<AgendaTemplate>();
    public DbSet<ApprovalChain> ApprovalChains => Set<ApprovalChain>();
    public DbSet<ApprovalStep> ApprovalSteps => Set<ApprovalStep>();
    public DbSet<EscalationChain> EscalationChains => Set<EscalationChain>();
    public DbSet<EscalationLevel> EscalationLevels => Set<EscalationLevel>();
    public DbSet<GovernanceTimelineEntry> GovernanceTimeline => Set<GovernanceTimelineEntry>();
    public DbSet<GovernanceAuditLog> GovernanceAuditLogs => Set<GovernanceAuditLog>();
    public DbSet<GovernanceLookupCategory> LookupCategories => Set<GovernanceLookupCategory>();
    public DbSet<GovernanceLookupValue> LookupValues => Set<GovernanceLookupValue>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<PlatformSequence> PlatformSequences => Set<PlatformSequence>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<Responsibility>(e =>
        {
            e.ToTable("Responsibilities");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.ResponsibilityCode).IsUnique();
            e.Property(x => x.RowVersion).IsRowVersion();
        });
        builder.Entity<GovernanceAssignment>(e =>
        {
            e.ToTable("GovernanceAssignments");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.ResponsibilityId, x.AssigneeType, x.AssigneeId });
            e.Property(x => x.RowVersion).IsRowVersion();
        });
        builder.Entity<Committee>(e =>
        {
            e.ToTable("Committees");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.CommitteeCode).IsUnique();
            e.Property(x => x.RowVersion).IsRowVersion();
            e.HasMany(x => x.Memberships).WithOne().HasForeignKey(x => x.CommitteeId);
            e.HasMany(x => x.Meetings).WithOne().HasForeignKey(x => x.CommitteeId);
            e.Navigation(x => x.Memberships).UsePropertyAccessMode(PropertyAccessMode.Field);
            e.Navigation(x => x.Meetings).UsePropertyAccessMode(PropertyAccessMode.Field);
        });
        builder.Entity<CommitteeMembership>(e => e.ToTable("CommitteeMemberships"));
        builder.Entity<CommitteeMeeting>(e =>
        {
            e.ToTable("CommitteeMeetings");
            e.Property(x => x.AgendaTemplateCode).HasMaxLength(64);
            e.HasMany(x => x.Decisions).WithOne().HasForeignKey(x => x.CommitteeMeetingId);
            e.HasMany(x => x.ActionItems).WithOne().HasForeignKey(x => x.CommitteeMeetingId);
            e.Navigation(x => x.Decisions).UsePropertyAccessMode(PropertyAccessMode.Field);
            e.Navigation(x => x.ActionItems).UsePropertyAccessMode(PropertyAccessMode.Field);
        });
        builder.Entity<CommitteeDecision>(e =>
        {
            e.ToTable("CommitteeDecisions");
            e.Property(x => x.OwnerAssigneeId);
            e.Property(x => x.ImplementationStatus);
            e.HasIndex(x => x.DeficiencyId);
            e.HasIndex(x => x.RiskId);
            e.HasIndex(x => x.TaskId);
        });
        builder.Entity<CommitteeActionItem>(e =>
        {
            e.ToTable("CommitteeActionItems");
            e.Property(x => x.Title).HasMaxLength(500);
            e.HasIndex(x => x.TaskId);
            e.HasIndex(x => x.CommitteeMeetingId);
        });
        builder.Entity<AgendaTemplate>(e =>
        {
            e.ToTable("AgendaTemplates");
            e.HasIndex(x => x.Code).IsUnique();
            e.Property(x => x.Code).HasMaxLength(64);
            e.Property(x => x.Name).HasMaxLength(200);
        });
        builder.Entity<ApprovalChain>(e =>
        {
            e.ToTable("ApprovalChains");
            e.HasIndex(x => x.ChainCode).IsUnique();
            e.Property(x => x.RowVersion).IsRowVersion();
            e.HasMany(x => x.Steps).WithOne().HasForeignKey(x => x.ApprovalChainId);
            e.Navigation(x => x.Steps).UsePropertyAccessMode(PropertyAccessMode.Field);
        });
        builder.Entity<ApprovalStep>(e => e.ToTable("ApprovalSteps"));
        builder.Entity<EscalationChain>(e =>
        {
            e.ToTable("EscalationChains");
            e.HasIndex(x => x.ChainCode).IsUnique();
            e.Property(x => x.RowVersion).IsRowVersion();
            e.HasMany(x => x.Levels).WithOne().HasForeignKey(x => x.EscalationChainId);
            e.Navigation(x => x.Levels).UsePropertyAccessMode(PropertyAccessMode.Field);
        });
        builder.Entity<EscalationLevel>(e => e.ToTable("EscalationLevels"));
        builder.Entity<GovernanceTimelineEntry>(e =>
        {
            e.ToTable("GovernanceTimeline");
            e.HasIndex(x => new { x.GovernedResourceType, x.GovernedResourceId, x.OccurredAt });
        });
        builder.Entity<GovernanceAuditLog>(e => e.ToTable("GovernanceAuditLogs"));
        builder.Entity<GovernanceLookupCategory>(e => { e.ToTable("LookupCategories"); e.HasIndex(x => x.Code).IsUnique(); });
        builder.Entity<GovernanceLookupValue>(e =>
        {
            e.ToTable("LookupValues");
            e.HasIndex(x => new { x.CategoryId, x.Code }).IsUnique();
            e.HasOne(x => x.Category).WithMany(x => x.Values).HasForeignKey(x => x.CategoryId);
        });
        builder.Entity<OutboxMessage>(e => { e.ToTable("OutboxMessages"); e.HasIndex(x => x.ProcessedOn); });
        builder.Entity<PlatformSequence>(e => e.ToTable("PlatformSequences"));
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries().Where(e => e.State == EntityState.Added))
        {
            if (entry.Metadata.FindProperty("RowVersion") != null)
                entry.Property("RowVersion").CurrentValue = new byte[8];
        }
        return await base.SaveChangesAsync(cancellationToken);
    }
}
