using AuditService.Domain;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Domain;
using Shared.Infrastructure.EntityFrameworkCore;

namespace AuditService.Infrastructure.Persistence
{
    public class AuditDbContext : DbContext
    {
        public AuditDbContext(DbContextOptions<AuditDbContext> options) : base(options) { }

        public DbSet<AuditSession> AuditSessions { get; set; }
        public DbSet<AuditFinding> AuditFindings { get; set; }
        public DbSet<CorrectiveAction> CorrectiveActions { get; set; }
        public DbSet<AuditReport> AuditReports { get; set; }
        public DbSet<PlatformSequence> PlatformSequences { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<PlatformSequence>(entity =>
            {
                entity.ToTable("PlatformSequences");

                entity.HasKey(x => x.Id);

                entity.Property(x => x.Prefix)
                      .IsRequired()
                      .HasMaxLength(10);

                entity.HasIndex(x => x.Prefix)
                      .IsUnique();

                entity.Property(x => x.CurrentValue)
                      .IsRequired();
            });

            // AuditSession configuration
            builder.Entity<AuditSession>(entity =>
            {
                entity.Property(x => x.Explainability)
                      .ConfigureExplainability();
            });

            // AuditFinding configuration
            builder.Entity<AuditFinding>(entity =>
            {
                entity.Property(x => x.Explainability)
                      .ConfigureExplainability();
            });
        }
    }
}
