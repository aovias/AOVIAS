using Microsoft.EntityFrameworkCore;
using WorkforceIntelligenceService.Domain.ReadModels;

namespace WorkforceIntelligenceService.Infrastructure.Persistence;

public class WorkforceIntelligenceDbContext(DbContextOptions<WorkforceIntelligenceDbContext> options) : DbContext(options)
{
    public DbSet<WorkforceSnapshot> WorkforceSnapshots => Set<WorkforceSnapshot>();
    public DbSet<ExecutiveMetrics> ExecutiveMetrics => Set<ExecutiveMetrics>();
    public DbSet<SkillMatrixEntry> SkillMatrixEntries => Set<SkillMatrixEntry>();
    public DbSet<CompetencyMatrixEntry> CompetencyMatrixEntries => Set<CompetencyMatrixEntry>();
    public DbSet<TrainingMatrixEntry> TrainingMatrixEntries => Set<TrainingMatrixEntry>();
    public DbSet<CertificationMatrixEntry> CertificationMatrixEntries => Set<CertificationMatrixEntry>();
    public DbSet<LicenseMatrixEntry> LicenseMatrixEntries => Set<LicenseMatrixEntry>();
    public DbSet<OrganizationAnalyticsSnapshot> OrganizationAnalyticsSnapshots => Set<OrganizationAnalyticsSnapshot>();
    public DbSet<ResponsibilityMatrixEntry> ResponsibilityMatrixEntries => Set<ResponsibilityMatrixEntry>();
    public DbSet<CommitteeAnalyticsSnapshot> CommitteeAnalyticsSnapshots => Set<CommitteeAnalyticsSnapshot>();
    public DbSet<AvailabilityAnalyticsSnapshot> AvailabilityAnalyticsSnapshots => Set<AvailabilityAnalyticsSnapshot>();
    public DbSet<ForecastSnapshot> ForecastSnapshots => Set<ForecastSnapshot>();
    public DbSet<WorkforceRecommendation> WorkforceRecommendations => Set<WorkforceRecommendation>();
    public DbSet<AnalyticsCache> AnalyticsCaches => Set<AnalyticsCache>();
    public DbSet<RecommendationCache> RecommendationCaches => Set<RecommendationCache>();
    public DbSet<WorkforceSearchIndex> WorkforceSearchIndexes => Set<WorkforceSearchIndex>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<ProjectionCheckpoint> ProjectionCheckpoints => Set<ProjectionCheckpoint>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<WorkforceSnapshot>(e =>
        {
            e.ToTable("WorkforceSnapshots");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.OrganizationId, x.AsOfUtc });
        });
        builder.Entity<ExecutiveMetrics>(e =>
        {
            e.ToTable("ExecutiveMetrics");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.OrganizationId, x.AsOfUtc });
        });
        builder.Entity<SkillMatrixEntry>(e => { e.ToTable("SkillMatrixEntries"); e.HasKey(x => x.Id); e.HasIndex(x => new { x.OrganizationId, x.AsOfUtc }); });
        builder.Entity<CompetencyMatrixEntry>(e => { e.ToTable("CompetencyMatrixEntries"); e.HasKey(x => x.Id); e.HasIndex(x => new { x.OrganizationId, x.AsOfUtc }); });
        builder.Entity<TrainingMatrixEntry>(e => { e.ToTable("TrainingMatrixEntries"); e.HasKey(x => x.Id); e.HasIndex(x => new { x.OrganizationId, x.AsOfUtc }); });
        builder.Entity<CertificationMatrixEntry>(e => { e.ToTable("CertificationMatrixEntries"); e.HasKey(x => x.Id); e.HasIndex(x => new { x.OrganizationId, x.Status }); });
        builder.Entity<LicenseMatrixEntry>(e => { e.ToTable("LicenseMatrixEntries"); e.HasKey(x => x.Id); e.HasIndex(x => new { x.OrganizationId, x.Status }); });
        builder.Entity<OrganizationAnalyticsSnapshot>(e => { e.ToTable("OrganizationAnalyticsSnapshots"); e.HasKey(x => x.Id); e.HasIndex(x => new { x.OrganizationId, x.AsOfUtc }); });
        builder.Entity<ResponsibilityMatrixEntry>(e => { e.ToTable("ResponsibilityMatrixEntries"); e.HasKey(x => x.Id); e.HasIndex(x => new { x.OrganizationId, x.ResponsibilityId }); });
        builder.Entity<CommitteeAnalyticsSnapshot>(e => { e.ToTable("CommitteeAnalyticsSnapshots"); e.HasKey(x => x.Id); e.HasIndex(x => new { x.OrganizationId, x.CommitteeId }); });
        builder.Entity<AvailabilityAnalyticsSnapshot>(e => { e.ToTable("AvailabilityAnalyticsSnapshots"); e.HasKey(x => x.Id); e.HasIndex(x => new { x.OrganizationId, x.AsOfUtc }); });
        builder.Entity<ForecastSnapshot>(e =>
        {
            e.ToTable("ForecastSnapshots");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.OrganizationId, x.ForecastType, x.Version });
        });
        builder.Entity<WorkforceRecommendation>(e =>
        {
            e.ToTable("WorkforceRecommendations");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.OrganizationId, x.IsActive, x.GeneratedAtUtc });
        });
        builder.Entity<AnalyticsCache>(e => { e.ToTable("AnalyticsCaches"); e.HasKey(x => x.Id); e.HasIndex(x => x.CacheKey).IsUnique(); });
        builder.Entity<RecommendationCache>(e => { e.ToTable("RecommendationCaches"); e.HasKey(x => x.Id); e.HasIndex(x => x.OrganizationId); });
        builder.Entity<WorkforceSearchIndex>(e =>
        {
            e.ToTable("WorkforceSearchIndexes");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.OrganizationId);
            e.HasIndex(x => x.PersonId);
        });
        builder.Entity<OutboxMessage>(e => { e.ToTable("OutboxMessages"); e.HasKey(x => x.Id); e.HasIndex(x => x.ProcessedOn); });
        builder.Entity<ProjectionCheckpoint>(e => { e.ToTable("ProjectionCheckpoints"); e.HasKey(x => x.Id); e.HasIndex(x => x.EventType).IsUnique(); });
    }
}
