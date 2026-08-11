using IntegrationService.Domain.ReadModels;
using Microsoft.EntityFrameworkCore;

namespace IntegrationService.Infrastructure.Persistence;

public class IntegrationDbContext(DbContextOptions<IntegrationDbContext> options) : DbContext(options)
{
    public DbSet<IntegrationConnector> Connectors => Set<IntegrationConnector>();
    public DbSet<ConnectorCredential> ConnectorCredentials => Set<ConnectorCredential>();
    public DbSet<SynchronizationJob> SynchronizationJobs => Set<SynchronizationJob>();
    public DbSet<SynchronizationSchedule> SynchronizationSchedules => Set<SynchronizationSchedule>();
    public DbSet<SynchronizationRun> SynchronizationRuns => Set<SynchronizationRun>();
    public DbSet<MappingProfile> MappingProfiles => Set<MappingProfile>();
    public DbSet<FieldMapping> FieldMappings => Set<FieldMapping>();
    public DbSet<TransformationRule> TransformationRules => Set<TransformationRule>();
    public DbSet<WebhookSubscription> WebhookSubscriptions => Set<WebhookSubscription>();
    public DbSet<WebhookDelivery> WebhookDeliveries => Set<WebhookDelivery>();
    public DbSet<ImportJob> ImportJobs => Set<ImportJob>();
    public DbSet<ExportJob> ExportJobs => Set<ExportJob>();
    public DbSet<ConnectorHealth> ConnectorHealthRecords => Set<ConnectorHealth>();
    public DbSet<IntegrationAuditLog> IntegrationAuditLogs => Set<IntegrationAuditLog>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<IntegrationConnector>(e =>
        {
            e.ToTable("IntegrationConnectors");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.OrganizationId, x.Name });
            e.Property(x => x.RowVersion).IsRowVersion();
        });
        builder.Entity<ConnectorCredential>(e => { e.ToTable("ConnectorCredentials"); e.HasKey(x => x.Id); });
        builder.Entity<SynchronizationJob>(e => { e.ToTable("SynchronizationJobs"); e.HasKey(x => x.Id); });
        builder.Entity<SynchronizationSchedule>(e => { e.ToTable("SynchronizationSchedules"); e.HasKey(x => x.Id); e.HasIndex(x => x.SynchronizationJobId); });
        builder.Entity<SynchronizationRun>(e => { e.ToTable("SynchronizationRuns"); e.HasKey(x => x.Id); e.HasIndex(x => x.SynchronizationJobId); });
        builder.Entity<MappingProfile>(e => { e.ToTable("MappingProfiles"); e.HasKey(x => x.Id); });
        builder.Entity<FieldMapping>(e => { e.ToTable("FieldMappings"); e.HasKey(x => x.Id); e.HasIndex(x => x.MappingProfileId); });
        builder.Entity<TransformationRule>(e => { e.ToTable("TransformationRules"); e.HasKey(x => x.Id); });
        builder.Entity<WebhookSubscription>(e => { e.ToTable("WebhookSubscriptions"); e.HasKey(x => x.Id); });
        builder.Entity<WebhookDelivery>(e => { e.ToTable("WebhookDeliveries"); e.HasKey(x => x.Id); e.HasIndex(x => x.WebhookSubscriptionId); });
        builder.Entity<ImportJob>(e => { e.ToTable("ImportJobs"); e.HasKey(x => x.Id); });
        builder.Entity<ExportJob>(e => { e.ToTable("ExportJobs"); e.HasKey(x => x.Id); });
        builder.Entity<ConnectorHealth>(e => { e.ToTable("ConnectorHealth"); e.HasKey(x => x.Id); e.HasIndex(x => x.ConnectorId); });
        builder.Entity<IntegrationAuditLog>(e => { e.ToTable("IntegrationAuditLogs"); e.HasKey(x => x.Id); e.HasIndex(x => new { x.OrganizationId, x.OccurredAtUtc }); });
        builder.Entity<OutboxMessage>(e => { e.ToTable("OutboxMessages"); e.HasKey(x => x.Id); });
    }
}
