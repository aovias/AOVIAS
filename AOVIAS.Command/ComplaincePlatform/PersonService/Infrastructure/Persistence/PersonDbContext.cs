using PersonService.Domain.Aggregates;
using PersonService.Domain.Entities;
using PersonService.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Domain;

namespace PersonService.Infrastructure.Persistence;

public class PersonDbContext : DbContext
{
    public PersonDbContext(DbContextOptions<PersonDbContext> options) : base(options) { }

    public DbSet<Person> Persons => Set<Person>();
    public DbSet<PersonContact> PersonContacts => Set<PersonContact>();
    public DbSet<PersonAddress> PersonAddresses => Set<PersonAddress>();
    public DbSet<PersonDocument> PersonDocuments => Set<PersonDocument>();
    public DbSet<EmergencyContact> EmergencyContacts => Set<EmergencyContact>();
    public DbSet<PersonPreferences> PersonPreferences => Set<PersonPreferences>();
    public DbSet<PersonTimelineEntry> PersonTimeline => Set<PersonTimelineEntry>();
    public DbSet<PersonLookupCategory> PersonLookupCategories => Set<PersonLookupCategory>();
    public DbSet<PersonLookupValue> PersonLookupValues => Set<PersonLookupValue>();
    public DbSet<PersonAuditLog> PersonAuditLogs => Set<PersonAuditLog>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<PlatformSequence> PlatformSequences => Set<PlatformSequence>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<Person>(entity =>
        {
            entity.ToTable("Persons");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.PlatformId).IsUnique();
            entity.HasIndex(x => x.SearchName);
            entity.Property(x => x.Status).HasConversion<int>();
            entity.Property(x => x.RowVersion).IsRowVersion();
        });

        builder.Entity<PersonContact>(entity =>
        {
            entity.ToTable("PersonContacts");
            entity.HasIndex(x => new { x.NormalizedValue, x.ContactKind });
            entity.HasOne(x => x.Person).WithMany(x => x.Contacts).HasForeignKey(x => x.PersonId);
        });

        builder.Entity<PersonAddress>(entity =>
        {
            entity.ToTable("PersonAddresses");
            entity.HasOne(x => x.Person).WithMany(x => x.Addresses).HasForeignKey(x => x.PersonId);
        });

        builder.Entity<PersonDocument>(entity =>
        {
            entity.ToTable("PersonDocuments");
            entity.HasIndex(x => new { x.DocumentNumber, x.DocumentTypeValueId });
            entity.HasOne(x => x.Person).WithMany(x => x.Documents).HasForeignKey(x => x.PersonId);
        });

        builder.Entity<EmergencyContact>(entity =>
        {
            entity.ToTable("EmergencyContacts");
            entity.HasOne(x => x.Person).WithMany(x => x.EmergencyContacts).HasForeignKey(x => x.PersonId);
        });

        builder.Entity<PersonPreferences>(entity =>
        {
            entity.ToTable("PersonPreferences");
            entity.HasKey(x => x.PersonId);
            entity.HasOne(x => x.Person).WithOne(x => x.Preferences).HasForeignKey<PersonPreferences>(x => x.PersonId);
        });

        builder.Entity<PersonTimelineEntry>(entity =>
        {
            entity.ToTable("PersonTimeline");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.PersonId, x.OccurredAt });
        });

        builder.Entity<PersonLookupCategory>(entity =>
        {
            entity.ToTable("PersonLookupCategories");
            entity.HasIndex(x => x.Code).IsUnique();
        });

        builder.Entity<PersonLookupValue>(entity =>
        {
            entity.ToTable("PersonLookupValues");
            entity.HasIndex(x => new { x.CategoryId, x.Code }).IsUnique();
            entity.HasOne(x => x.Category).WithMany(x => x.Values).HasForeignKey(x => x.CategoryId);
        });

        builder.Entity<PersonAuditLog>(entity =>
        {
            entity.ToTable("PersonAuditLogs");
            entity.HasIndex(x => x.PersonId);
        });

        builder.Entity<OutboxMessage>(entity =>
        {
            entity.ToTable("OutboxMessages");
            entity.HasIndex(x => x.ProcessedOn);
        });

        builder.Entity<PlatformSequence>(entity =>
        {
            entity.ToTable("PlatformSequences");
            entity.HasIndex(x => x.Prefix).IsUnique();
        });
    }
}
