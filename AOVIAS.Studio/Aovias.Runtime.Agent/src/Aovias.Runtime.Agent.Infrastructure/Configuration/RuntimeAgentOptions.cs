namespace Aovias.Runtime.Agent.Infrastructure.Configuration;

/// <summary>
/// Root configuration options for the runtime agent host.
/// </summary>
public sealed class RuntimeAgentOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "RuntimeAgent";

    /// <summary>
    /// Gets or sets the runtime agent display name.
    /// </summary>
    public string AgentName { get; set; } = "AOVIAS Runtime Agent";

    /// <summary>
    /// Gets or sets the canonical runtime version.
    /// </summary>
    public string RuntimeVersion { get; set; } = "1.0";

    /// <summary>
    /// Gets or sets the deployment environment name.
    /// </summary>
    public string EnvironmentName { get; set; } = "Demo";

    /// <summary>
    /// Gets or sets the HTTPS endpoint exposed by the runtime.
    /// </summary>
    public string HttpsEndpoint { get; set; } = "https://aovias-demo-01.aovias.cloud";

    /// <summary>
    /// Gets or sets the public IP address.
    /// </summary>
    public string PublicIp { get; set; } = "203.0.113.42";

    /// <summary>
    /// Gets or sets a value indicating whether SSL is enabled.
    /// </summary>
    public bool SslEnabled { get; set; } = true;
}

/// <summary>
/// Configuration options for runtime manifest discovery.
/// </summary>
public sealed class RuntimeManifestOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "RuntimeManifest";

    /// <summary>
    /// Gets or sets the canonical runtime manifest path on the host.
    /// </summary>
    public string ManifestPath { get; set; } = "/opt/aovias/runtime/runtime.json";
}

/// <summary>
/// Configuration options for Serilog file logging.
/// </summary>
public sealed class SerilogFileOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "SerilogFile";

    /// <summary>
    /// Gets or sets the log directory path.
    /// </summary>
    public string LogDirectory { get; set; } = "/opt/aovias/logs/runtime-agent";

    /// <summary>
    /// Gets or sets the rolling log file prefix.
    /// </summary>
    public string FileNamePrefix { get; set; } = "runtime-agent-";

    /// <summary>
    /// Gets or sets the number of retained log files.
    /// </summary>
    public int RetainedFileCountLimit { get; set; } = 14;
}
