using ComplianceServiceManager.Logging;
using ComplianceServiceManager.Models;
using ComplianceServiceManager.Services;
using ComplianceServiceManager.Services.LocalDev;
using ComplianceServiceManager.Services.RuntimeProfiles;
using System.Collections.ObjectModel;

namespace ComplianceServiceManager.Tests.RuntimeProfiles;

/// <summary>
/// Resolves the seeded AOVIAS Web Development profile against live discovery when folders are configured.
/// Does not start processes (full stack / SQL not required for S2 graph-resolution validation).
/// </summary>
public sealed class RuntimeProfileDiscoveryValidationTests
{
    [Fact]
    public async Task SeedProfile_ValidatesAgainstLiveDiscovery_WhenServicesPresent()
    {
        var logger = new NoopLoggingService();
        var folderRegistry = new SolutionFolderRegistry();
        if (folderRegistry.Folders.Count == 0)
        {
            return;
        }

        var discovery = new ServiceDiscoveryService(folderRegistry, logger);
        var services = await discovery.DiscoverAsync(CancellationToken.None);
        if (services.Count == 0)
        {
            return;
        }

        var seed = RuntimeProfileRegistry.BuildDefaults().Profiles[0];
        var discoveredNames = services.Select(service => service.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var missing = seed.Services
            .Select(entry => entry.ServiceName)
            .Where(name => !discoveredNames.Contains(name))
            .ToList();

        if (missing.Count > 0)
        {
            Assert.True(
                missing.Count < seed.Services.Count,
                $"None of the seed profile services were discovered. Missing: {string.Join(", ", missing)}");
            return;
        }

        var result = new RuntimeProfileValidator().Validate(seed, services);
        Assert.True(result.IsValid, string.Join("; ", result.Issues.Select(issue => issue.Message)));
        Assert.Equal(5, result.StartupOrder.Count);
        Assert.Equal("WebApp", result.StartupOrder[^1]);
        Assert.Contains(services, s => s.Name == "WebApp" && s.Kind == ServiceKind.AngularWebApp);
    }

    [Fact]
    public async Task SeedProfileMembers_MatchDiscoveredIdentity_WhenFoldersConfigured()
    {
        var logger = new NoopLoggingService();
        var folderRegistry = new SolutionFolderRegistry();
        if (folderRegistry.Folders.Count == 0)
        {
            return;
        }

        var discovery = new ServiceDiscoveryService(folderRegistry, logger);
        var services = await discovery.DiscoverAsync(CancellationToken.None);
        var seed = RuntimeProfileRegistry.BuildDefaults().Profiles[0];
        var discoveredNames = services.Select(service => service.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var member in seed.Services)
        {
            var match = services.FirstOrDefault(s =>
                string.Equals(s.Name, member.ServiceName, StringComparison.OrdinalIgnoreCase));
            Assert.True(
                match is not null,
                $"{member.ServiceName} | NO | discovered=[{string.Join(", ", discoveredNames.OrderBy(n => n))}] | mismatch");
        }

        var webApp = services.Single(s => s.Name == "WebApp");
        Assert.Equal(ServiceKind.AngularWebApp, webApp.Kind);
    }

    private sealed class NoopLoggingService : ILoggingService
    {
        public ObservableCollection<LogEntryModel> Entries { get; } = [];

        public void Log(LogLevel level, string source, string message)
        {
        }

        public void Info(string source, string message)
        {
        }

        public void Warning(string source, string message)
        {
        }

        public void Error(string source, string message)
        {
        }

        public void Build(string source, string message)
        {
        }

        public void Process(string source, string message)
        {
        }
    }
}
