using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace KnowledgeBase.Infrastructure.Persistence;

public sealed class KnowledgeBaseContextFactory : IDesignTimeDbContextFactory<KnowledgeBaseContext>
{
    public KnowledgeBaseContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<KnowledgeBaseContext>()
            .UseSqlServer("Server=localhost\\SQLEXPRESS;Database=KnowledgeBase;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;
        return new KnowledgeBaseContext(options);
    }
}
