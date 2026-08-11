using EnterpriseDirectoryService.Domain.ReadModels;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseDirectoryService.Infrastructure.Persistence;

public class EnterpriseDirectoryDbContext(DbContextOptions<EnterpriseDirectoryDbContext> options) : DbContext(options)
{
    public DbSet<DirectoryIndex> DirectoryIndexes => Set<DirectoryIndex>();
    public DbSet<DirectoryFacet> DirectoryFacets => Set<DirectoryFacet>();
    public DbSet<DirectorySnapshot> DirectorySnapshots => Set<DirectorySnapshot>();
    public DbSet<DirectoryRanking> DirectoryRankings => Set<DirectoryRanking>();
    public DbSet<SavedSearch> SavedSearches => Set<SavedSearch>();
    public DbSet<SearchHistory> SearchHistories => Set<SearchHistory>();
    public DbSet<SearchPreference> SearchPreferences => Set<SearchPreference>();
    public DbSet<DirectorySuggestion> DirectorySuggestions => Set<DirectorySuggestion>();
    public DbSet<DirectorySynonym> DirectorySynonyms => Set<DirectorySynonym>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<ProjectionCheckpoint> ProjectionCheckpoints => Set<ProjectionCheckpoint>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<DirectoryIndex>(e =>
        {
            e.ToTable("DirectoryIndex");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.OrganizationId, x.PersonId });
            e.HasIndex(x => x.EmploymentId);
            e.HasIndex(x => new { x.OrganizationId, x.IsActive });
            e.Property(x => x.SearchBlob).HasColumnType("nvarchar(max)");
        });
        builder.Entity<DirectoryFacet>(e =>
        {
            e.ToTable("DirectoryFacets");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.OrganizationId, x.FacetType, x.FacetValue });
        });
        builder.Entity<DirectorySnapshot>(e => { e.ToTable("DirectorySnapshots"); e.HasKey(x => x.Id); });
        builder.Entity<DirectoryRanking>(e => { e.ToTable("DirectoryRankings"); e.HasKey(x => x.Id); });
        builder.Entity<SavedSearch>(e => { e.ToTable("SavedSearches"); e.HasKey(x => x.Id); e.HasIndex(x => new { x.OwnerUserId, x.OrganizationId }); });
        builder.Entity<SearchHistory>(e => { e.ToTable("SearchHistories"); e.HasKey(x => x.Id); e.HasIndex(x => new { x.UserId, x.SearchedAtUtc }); });
        builder.Entity<SearchPreference>(e => { e.ToTable("SearchPreferences"); e.HasKey(x => x.UserId); });
        builder.Entity<DirectorySuggestion>(e => { e.ToTable("DirectorySuggestions"); e.HasKey(x => x.Id); e.HasIndex(x => x.CacheKey).IsUnique(); });
        builder.Entity<DirectorySynonym>(e => { e.ToTable("DirectorySynonyms"); e.HasKey(x => x.Id); e.HasIndex(x => x.Token); });
        builder.Entity<OutboxMessage>(e => { e.ToTable("OutboxMessages"); e.HasKey(x => x.Id); });
        builder.Entity<ProjectionCheckpoint>(e => { e.ToTable("ProjectionCheckpoints"); e.HasKey(x => x.Id); e.HasIndex(x => x.EventType).IsUnique(); });
    }
}
