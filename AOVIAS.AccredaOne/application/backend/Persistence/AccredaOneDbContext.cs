using Microsoft.EntityFrameworkCore;

namespace AccredaOne.Api.Persistence;

/// <summary>
/// Minimal EF Core boundary for PostgreSQL integration. No domain entities or schema in DEV-04.
/// </summary>
public sealed class AccredaOneDbContext : DbContext
{
    public AccredaOneDbContext(DbContextOptions<AccredaOneDbContext> options)
        : base(options)
    {
    }
}
