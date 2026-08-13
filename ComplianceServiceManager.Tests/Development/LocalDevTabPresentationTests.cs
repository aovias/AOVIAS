using ComplianceServiceManager.ViewModels.Development;

namespace ComplianceServiceManager.Tests.Development;

/// <summary>
/// Presentation-structure checks for V2.0 Local Development tabs.
/// Does not exercise D1–D5 engines.
/// </summary>
public sealed class LocalDevTabPresentationTests
{
    [Fact]
    public void TabHeaders_MatchFiveTabInformationArchitecture()
    {
        Assert.Equal(
            ["Overview", "Git & PR", "Agent Sessions", "Services", "Monitoring"],
            LocalDevServicesViewModel.LocalDevelopmentTabHeaders);
        Assert.Equal(0, LocalDevServicesViewModel.OverviewTabIndex);
        Assert.Equal(1, LocalDevServicesViewModel.GitAndPrTabIndex);
        Assert.Equal(2, LocalDevServicesViewModel.AgentSessionsTabIndex);
        Assert.Equal(3, LocalDevServicesViewModel.ServicesTabIndex);
        Assert.Equal(4, LocalDevServicesViewModel.MonitoringTabIndex);
    }

    [Fact]
    public void SelectedLocalDevTabIndex_DefaultsToOverview()
    {
        var field = typeof(LocalDevServicesViewModel)
            .GetField("selectedLocalDevTabIndex",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        Assert.NotNull(field);
        Assert.Equal(typeof(int), field!.FieldType);
        // Default for int backing field is 0 = Overview; refresh commands must not reassign it.
        var source = System.IO.File.ReadAllText(
            FindViewModelSource());
        Assert.DoesNotContain("SelectedLocalDevTabIndex =", source);
        Assert.DoesNotContain("selectedLocalDevTabIndex =", source.Replace("private int selectedLocalDevTabIndex;", string.Empty));
    }

    [Fact]
    public void LocalDevView_UsesCanonicalOperatorVocabulary()
    {
        var xaml = File.ReadAllText(FindViewSource());
        Assert.Contains("Source branch:", xaml);
        Assert.Contains("Target branch:", xaml);
        Assert.Contains("Source SHA:", xaml);
        Assert.Contains("Target SHA:", xaml);
        Assert.Contains("This records what an agent is working on", xaml);
        Assert.Contains("Does not launch Cursor or Claude", xaml);
        Assert.Contains("Mark Active", xaml);
        Assert.Contains("Pause Record", xaml);
        Assert.Contains("Header=\"Source\" Binding=\"{Binding SourcePath}\"", xaml);
        Assert.Contains("Header=\"Owner\" Binding=\"{Binding OwnershipDisplay}\"", xaml);
        Assert.Contains("Start All (legacy)", xaml);
        Assert.DoesNotContain("COMPARATIVE CLASS", xaml);
        Assert.DoesNotContain("HIGH MEMORY PRESSURE", xaml);
        Assert.DoesNotContain("Definition vs execution context", xaml);
        Assert.Contains("Estimated resource level", File.ReadAllText(FindResourceIntelligenceSource()));
    }

    private static string FindViewSource()
    {
        var candidates = new[]
        {
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..",
                "ComplianceServiceManager", "Views", "Development", "LocalDevServicesView.xaml")),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..",
                "ComplianceServiceManager", "Views", "Development", "LocalDevServicesView.xaml"))
        };

        foreach (var path in candidates)
        {
            if (File.Exists(path))
            {
                return path;
            }
        }

        throw new FileNotFoundException("LocalDevServicesView.xaml not found relative to test output.");
    }

    private static string FindResourceIntelligenceSource()
    {
        var candidates = new[]
        {
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..",
                "ComplianceServiceManager", "Services", "ResourceIntelligence", "ResourceIntelligenceService.cs")),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..",
                "ComplianceServiceManager", "Services", "ResourceIntelligence", "ResourceIntelligenceService.cs"))
        };

        foreach (var path in candidates)
        {
            if (File.Exists(path))
            {
                return path;
            }
        }

        throw new FileNotFoundException("ResourceIntelligenceService.cs not found relative to test output.");
    }

    private static string FindViewModelSource()
    {
        var candidates = new[]
        {
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..",
                "ComplianceServiceManager", "ViewModels", "Development", "LocalDevServicesViewModel.cs")),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..",
                "ComplianceServiceManager", "ViewModels", "Development", "LocalDevServicesViewModel.cs"))
        };

        foreach (var path in candidates)
        {
            if (File.Exists(path))
            {
                return path;
            }
        }

        throw new FileNotFoundException("LocalDevServicesViewModel.cs not found relative to test output.");
    }
}
