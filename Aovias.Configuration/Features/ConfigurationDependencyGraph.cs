namespace Aovias.Configuration.Features;

/// <summary>
/// Documents which configuration sections each platform module requires.
/// </summary>
public static class ConfigurationDependencyGraph
{
    public static IReadOnlyDictionary<string, IReadOnlyList<string>> ModuleDependencies { get; } =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["Runtime"] =
            [
                Settings.LoggingSettings.SectionName,
                Settings.StorageSettings.SectionName,
                Settings.RuntimeManifestSettings.SectionName,
                Settings.RuntimeSettings.SectionName,
                Settings.ApiSettings.SectionName,
                Settings.InfrastructureSettings.SectionName,
                Settings.NetworkSettings.SectionName
            ],
            ["ReleaseManagement"] =
            [
                Settings.ReleaseManagementSettings.SectionName,
                Settings.DeploymentSettings.SectionName,
                Settings.OrchestrationSettings.SectionName
            ],
            ["DockerProvider"] = [Settings.DockerSettings.SectionName, Settings.ProviderSettings.SectionName, Settings.NetworkSettings.SectionName],
            ["SignalR"] = [Settings.SignalRSettings.SectionName],
            ["Publishing"] = [Settings.PublishingSettings.SectionName],
            ["Monitoring"] = [Settings.MonitoringSettings.SectionName],
            ["Certificates"] = [Settings.CertificateSettings.SectionName],
            ["Observability"] = [Settings.ObservabilitySettings.SectionName],
            ["Secrets"] = [Secrets.SecretsSettings.SectionName]
        };

    /// <summary>
    /// Returns configuration section names reachable from the supplied feature activation.
    /// </summary>
    public static IReadOnlyCollection<string> GetReachableSections(PlatformFeatureActivation features)
    {
        var sections = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (features.Runtime)
        {
            AddSections(sections, "Runtime");
        }

        if (features.ReleaseManagement)
        {
            AddSections(sections, "ReleaseManagement");
        }

        if (features.DockerProvider)
        {
            AddSections(sections, "DockerProvider");
        }

        if (features.SignalR)
        {
            AddSections(sections, "SignalR");
        }

        if (features.Publishing)
        {
            AddSections(sections, "Publishing");
        }

        if (features.Monitoring)
        {
            AddSections(sections, "Monitoring");
        }

        if (features.Certificates)
        {
            AddSections(sections, "Certificates");
        }

        if (features.Observability)
        {
            AddSections(sections, "Observability");
        }

        if (features.RequiresSecretsValidation)
        {
            AddSections(sections, "Secrets");
        }

        return sections;
    }

    private static void AddSections(ISet<string> sections, string moduleName)
    {
        if (!ModuleDependencies.TryGetValue(moduleName, out var dependencies))
        {
            return;
        }

        foreach (var dependency in dependencies)
        {
            sections.Add(dependency);
        }
    }
}
