using KnowledgeBase.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Knowledgebase.Tests;

public sealed class KnowledgeWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = "kb-" + Guid.NewGuid().ToString("N");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            var remove = services.Where(d =>
                d.ServiceType == typeof(DbContextOptions<KnowledgeBaseContext>) ||
                d.ServiceType == typeof(KnowledgeBaseContext)).ToList();
            foreach (var d in remove)
                services.Remove(d);

            services.AddDbContext<KnowledgeBaseContext>(o => o.UseInMemoryDatabase(_databaseName));
        });
    }
}
