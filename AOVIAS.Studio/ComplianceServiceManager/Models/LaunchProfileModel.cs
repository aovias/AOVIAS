namespace ComplianceServiceManager.Models;

public sealed class LaunchProfileModel
{
    public string Name { get; init; } = string.Empty;

    public string CommandName { get; init; } = string.Empty;

    public string? ApplicationUrl { get; init; }

    public string? HttpsUrl { get; init; }

    public int? HttpsPort { get; init; }

    public string? HttpUrl { get; init; }

    public int? HttpPort { get; init; }

    public string? LaunchUrl { get; init; }

    public bool LaunchBrowser { get; init; }

    public IReadOnlyDictionary<string, string> EnvironmentVariables { get; init; } = new Dictionary<string, string>();

    public bool HasHttps => !string.IsNullOrWhiteSpace(HttpsUrl);

    public bool IsProjectProfile => CommandName.Equals("Project", StringComparison.OrdinalIgnoreCase);
}
