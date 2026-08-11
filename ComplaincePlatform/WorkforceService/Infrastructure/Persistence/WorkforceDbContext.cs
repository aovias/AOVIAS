using WorkforceService.Domain.Aggregates;
using WorkforceService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Domain;

namespace WorkforceService.Infrastructure.Persistence;

public class WorkforceDbContext : DbContext
{
    public WorkforceDbContext(DbContextOptions<WorkforceDbContext> options) : base(options) { }

    public DbSet<Employment> Employments => Set<Employment>();
    public DbSet<Position> Positions => Set<Position>();
    public DbSet<PositionAssignment> PositionAssignments => Set<PositionAssignment>();
    public DbSet<DepartmentAssignment> DepartmentAssignments => Set<DepartmentAssignment>();
    public DbSet<ReportingRelationship> ReportingRelationships => Set<ReportingRelationship>();
    public DbSet<EmploymentContract> EmploymentContracts => Set<EmploymentContract>();
    public DbSet<AvailabilityProfile> AvailabilityProfiles => Set<AvailabilityProfile>();
    public DbSet<EmploymentHistory> EmploymentHistories => Set<EmploymentHistory>();
    public DbSet<WorkforceTimelineEntry> WorkforceTimeline => Set<WorkforceTimelineEntry>();
    public DbSet<TransferRecord> Transfers => Set<TransferRecord>();
    public DbSet<PromotionRecord> Promotions => Set<PromotionRecord>();
    public DbSet<WorkforceLookupCategory> WorkforceLookupCategories => Set<WorkforceLookupCategory>();
    public DbSet<WorkforceLookupValue> WorkforceLookupValues => Set<WorkforceLookupValue>();
    public DbSet<WorkforceAuditLog> WorkforceAuditLogs => Set<WorkforceAuditLog>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<PlatformSequence> PlatformSequences => Set<PlatformSequence>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<Employment>(e =>
        {
            e.ToTable("Employments");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.EmploymentNumber).IsUnique();
            e.HasIndex(x => new { x.PersonId, x.OrganizationId, x.HospitalId });
            e.HasIndex(x => x.PersonSearchName);
            e.Property(x => x.RowVersion).IsRowVersion();
        });

        builder.Entity<Position>(e =>
        {
            e.ToTable("Positions");
            e.HasIndex(x => new { x.OrganizationId, x.PositionCode }).IsUnique();
        });

        builder.Entity<PositionAssignment>(e =>
        {
            e.ToTable("PositionAssignments");
            e.HasIndex(x => new { x.EmploymentId, x.EffectiveFrom });
            e.HasOne(x => x.Employment).WithMany(x => x.PositionAssignments).HasForeignKey(x => x.EmploymentId);
            e.HasOne(x => x.Position).WithMany().HasForeignKey(x => x.PositionId);
        });

        builder.Entity<DepartmentAssignment>(e =>
        {
            e.ToTable("DepartmentAssignments");
            e.HasOne(x => x.Employment).WithMany(x => x.DepartmentAssignments).HasForeignKey(x => x.EmploymentId);
        });

        builder.Entity<ReportingRelationship>(e =>
        {
            e.ToTable("ReportingRelationships");
            e.HasOne(x => x.Employment).WithMany(x => x.ReportingRelationships).HasForeignKey(x => x.EmploymentId);
        });

        builder.Entity<EmploymentContract>(e =>
        {
            e.ToTable("EmploymentContracts");
            e.HasOne(x => x.Employment).WithMany(x => x.Contracts).HasForeignKey(x => x.EmploymentId);
        });

        builder.Entity<AvailabilityProfile>(e =>
        {
            e.ToTable("AvailabilityProfiles");
            e.HasKey(x => x.EmploymentId);
            e.HasOne(x => x.Employment).WithOne(x => x.Availability).HasForeignKey<AvailabilityProfile>(x => x.EmploymentId);
        });

        builder.Entity<EmploymentHistory>(e =>
        {
            e.ToTable("EmploymentHistories");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.EmploymentId, x.OccurredAt });
        });

        builder.Entity<WorkforceTimelineEntry>(e =>
        {
            e.ToTable("WorkforceTimeline");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.EmploymentId, x.OccurredAt });
        });

        builder.Entity<TransferRecord>(e => e.ToTable("Transfers"));
        builder.Entity<PromotionRecord>(e => e.ToTable("Promotions"));

        builder.Entity<WorkforceLookupCategory>(e =>
        {
            e.ToTable("WorkforceLookupCategories");
            e.HasIndex(x => x.Code).IsUnique();
        });

        builder.Entity<WorkforceLookupValue>(e =>
        {
            e.ToTable("WorkforceLookupValues");
            e.HasIndex(x => new { x.CategoryId, x.Code }).IsUnique();
            e.HasOne(x => x.Category).WithMany(x => x.Values).HasForeignKey(x => x.CategoryId);
        });

        builder.Entity<WorkforceAuditLog>(e =>
        {
            e.ToTable("WorkforceAuditLogs");
            e.HasIndex(x => x.EmploymentId);
        });

        builder.Entity<OutboxMessage>(e =>
        {
            e.ToTable("OutboxMessages");
            e.HasIndex(x => x.ProcessedOn);
        });

        builder.Entity<PlatformSequence>(e =>
        {
            e.ToTable("PlatformSequences");
            e.HasIndex(x => x.Prefix).IsUnique();
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State is EntityState.Added or EntityState.Modified && (entry.Entity.RowVersion == null || entry.Entity.RowVersion.Length == 0))
                entry.Entity.RowVersion = new byte[8];
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
