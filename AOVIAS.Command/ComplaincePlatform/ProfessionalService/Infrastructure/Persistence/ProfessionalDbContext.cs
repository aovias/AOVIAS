using ProfessionalService.Domain.Aggregates;
using ProfessionalService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Domain;

namespace ProfessionalService.Infrastructure.Persistence;

public class ProfessionalDbContext : DbContext
{
    public ProfessionalDbContext(DbContextOptions<ProfessionalDbContext> options) : base(options) { }

    public DbSet<ProfessionalProfile> ProfessionalProfiles => Set<ProfessionalProfile>();
    public DbSet<Qualification> Qualifications => Set<Qualification>();
    public DbSet<ProfessionalRegistration> ProfessionalRegistrations => Set<ProfessionalRegistration>();
    public DbSet<License> Licenses => Set<License>();
    public DbSet<Certification> Certifications => Set<Certification>();
    public DbSet<Competency> Competencies => Set<Competency>();
    public DbSet<ClinicalPrivilege> ClinicalPrivileges => Set<ClinicalPrivilege>();
    public DbSet<CredentialingRecord> CredentialingRecords => Set<CredentialingRecord>();
    public DbSet<TrainingRecord> TrainingRecords => Set<TrainingRecord>();
    public DbSet<TrainingProgram> TrainingPrograms => Set<TrainingProgram>();
    public DbSet<TrainingSession> TrainingSessions => Set<TrainingSession>();
    public DbSet<TrainingAttendance> TrainingAttendances => Set<TrainingAttendance>();
    public DbSet<TrainingAssessment> TrainingAssessments => Set<TrainingAssessment>();
    public DbSet<ContinuingEducationRecord> ContinuingEducationRecords => Set<ContinuingEducationRecord>();
    public DbSet<ProfessionalMembership> ProfessionalMemberships => Set<ProfessionalMembership>();
    public DbSet<ProfessionalSkill> ProfessionalSkills => Set<ProfessionalSkill>();
    public DbSet<ProfessionalSpecialty> ProfessionalSpecialties => Set<ProfessionalSpecialty>();
    public DbSet<RenewalHistoryEntry> RenewalHistory => Set<RenewalHistoryEntry>();
    public DbSet<ProfessionalHistory> ProfessionalHistories => Set<ProfessionalHistory>();
    public DbSet<ProfessionalTimelineEntry> ProfessionalTimeline => Set<ProfessionalTimelineEntry>();
    public DbSet<ProfessionalLookupCategory> ProfessionalLookupCategories => Set<ProfessionalLookupCategory>();
    public DbSet<ProfessionalLookupValue> ProfessionalLookupValues => Set<ProfessionalLookupValue>();
    public DbSet<ProfessionalAuditLog> ProfessionalAuditLogs => Set<ProfessionalAuditLog>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<PlatformSequence> PlatformSequences => Set<PlatformSequence>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<ProfessionalProfile>(e =>
        {
            e.ToTable("ProfessionalProfiles");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.ProfessionalNumber).IsUnique();
            e.HasIndex(x => x.PersonId).IsUnique();
            e.HasIndex(x => x.EmploymentId);
            e.HasIndex(x => x.PersonSearchName);
            e.Property(x => x.RowVersion).IsRowVersion();
        });

        builder.Entity<Qualification>(e => e.ToTable("Qualifications"));
        builder.Entity<ProfessionalRegistration>(e =>
        {
            e.ToTable("ProfessionalRegistrations");
            e.HasIndex(x => new { x.ProfessionalProfileId, x.RegistrationAuthorityValueId, x.RegistrationNumber }).IsUnique();
        });
        builder.Entity<License>(e => e.ToTable("Licenses"));
        builder.Entity<Certification>(e => e.ToTable("Certifications"));
        builder.Entity<Competency>(e => e.ToTable("Competencies"));
        builder.Entity<ClinicalPrivilege>(e => e.ToTable("ClinicalPrivileges"));
        builder.Entity<CredentialingRecord>(e => e.ToTable("CredentialingRecords"));
        builder.Entity<TrainingRecord>(e => e.ToTable("TrainingRecords"));
        builder.Entity<TrainingProgram>(e =>
        {
            e.ToTable("TrainingPrograms");
            e.HasIndex(x => new { x.OrganizationId, x.Code }).IsUnique();
            e.HasIndex(x => x.ControlledDocumentId);
            e.HasIndex(x => new { x.RoleCode, x.DepartmentId });
            e.Property(x => x.Code).HasMaxLength(64);
            e.Property(x => x.Title).HasMaxLength(256);
            e.Property(x => x.RoleCode).HasMaxLength(64);
            e.Property(x => x.Status).HasMaxLength(32);
        });
        builder.Entity<TrainingSession>(e =>
        {
            e.ToTable("TrainingSessions");
            e.HasIndex(x => new { x.TrainingProgramId, x.StartsAt });
            e.Property(x => x.Title).HasMaxLength(256);
            e.Property(x => x.Status).HasMaxLength(32);
            e.HasOne(x => x.Program).WithMany(p => p.Sessions).HasForeignKey(x => x.TrainingProgramId);
        });
        builder.Entity<TrainingAttendance>(e =>
        {
            e.ToTable("TrainingAttendances");
            e.HasIndex(x => new { x.TrainingSessionId, x.ProfessionalProfileId }).IsUnique();
            e.HasIndex(x => x.ExpiryDate);
            e.Property(x => x.AttendanceStatus).HasMaxLength(32);
            e.HasOne(x => x.Session).WithMany(s => s.Attendances).HasForeignKey(x => x.TrainingSessionId);
        });
        builder.Entity<TrainingAssessment>(e =>
        {
            e.ToTable("TrainingAssessments");
            e.HasIndex(x => x.TrainingAttendanceId).IsUnique();
            e.Property(x => x.Score).HasPrecision(18, 2);
            e.Property(x => x.PassMark).HasPrecision(18, 2);
            e.HasOne(x => x.Attendance).WithOne(a => a.Assessment).HasForeignKey<TrainingAssessment>(x => x.TrainingAttendanceId);
        });
        builder.Entity<ContinuingEducationRecord>(e => e.ToTable("ContinuingEducationRecords"));
        builder.Entity<ProfessionalMembership>(e => e.ToTable("ProfessionalMemberships"));
        builder.Entity<ProfessionalSkill>(e => e.ToTable("ProfessionalSkills"));
        builder.Entity<ProfessionalSpecialty>(e => e.ToTable("ProfessionalSpecialties"));
        builder.Entity<RenewalHistoryEntry>(e => e.ToTable("RenewalHistory"));
        builder.Entity<ProfessionalHistory>(e =>
        {
            e.ToTable("ProfessionalHistories");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.ProfessionalProfileId, x.OccurredAt });
        });
        builder.Entity<ProfessionalTimelineEntry>(e =>
        {
            e.ToTable("ProfessionalTimeline");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.ProfessionalProfileId, x.OccurredAt });
        });
        builder.Entity<ProfessionalLookupCategory>(e =>
        {
            e.ToTable("ProfessionalLookupCategories");
            e.HasIndex(x => x.Code).IsUnique();
        });
        builder.Entity<ProfessionalLookupValue>(e =>
        {
            e.ToTable("ProfessionalLookupValues");
            e.HasIndex(x => new { x.CategoryId, x.Code }).IsUnique();
            e.HasOne(x => x.Category).WithMany(x => x.Values).HasForeignKey(x => x.CategoryId);
        });
        builder.Entity<ProfessionalAuditLog>(e =>
        {
            e.ToTable("ProfessionalAuditLogs");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.ProfessionalProfileId);
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
