using Aovias.Configuration.Features;
using Aovias.Configuration.Loading;

namespace Aovias.Configuration.Loading;

/// <summary>
/// Writes structured bootstrap diagnostics for Runtime Agent startup.
/// </summary>
public static class AoviasPlatformBootstrapDiagnostics
{
    /// <summary>
    /// Writes bootstrap startup header.
    /// </summary>
    public static void WriteBootstrapStart(TextWriter output)
        => output.WriteLine("Bootstrapping Runtime Agent...");

    /// <summary>
    /// Writes resolved bootstrap values.
    /// </summary>
    public static void WriteResolvedConfiguration(TextWriter output, AoviasPlatformBootstrapOptions bootstrap)
    {
        var builder = new System.Text.StringBuilder();
        builder.AppendLine();
        builder.AppendLine("Configuration Root:");
        builder.AppendLine(bootstrap.ConfigRoot);
        builder.AppendLine();
        builder.AppendLine("Active Profile:");
        builder.AppendLine(bootstrap.ActiveProfile);
        builder.AppendLine();
        builder.AppendLine("Profiles Directory:");
        builder.AppendLine(bootstrap.ProfilesPath);
        builder.AppendLine();
        builder.AppendLine("Loading profile...");
        output.WriteLine(builder.ToString().TrimEnd());
    }

    /// <summary>
    /// Writes bootstrap success message.
    /// </summary>
    public static void WriteSuccess(TextWriter output, AoviasPlatformBootstrapOptions bootstrap)
        => output.WriteLine($"Profile '{bootstrap.ActiveProfile}' loaded from {bootstrap.ActiveProfilePath}.");

    /// <summary>
    /// Writes bootstrap validation failure details.
    /// </summary>
    public static void WriteFailure(TextWriter output, AoviasConfigurationBootstrapException exception)
        => output.WriteLine(exception.Message);

    /// <summary>
    /// Populates a bootstrap report from resolved feature activation.
    /// </summary>
    public static void PopulateModuleStatuses(RuntimeBootstrapReport report, PlatformFeatureActivation features)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentNullException.ThrowIfNull(features);

        RecordFeatureModule(report, "Runtime", features.Runtime);
        RecordFeatureModule(report, "Release Management", features.ReleaseManagement);
        RecordFeatureModule(report, "Docker", features.DockerProvider);
        RecordFeatureModule(report, "SignalR", features.SignalR);
        RecordFeatureModule(report, "Publishing", features.Publishing);
        RecordFeatureModule(report, "Monitoring", features.Monitoring);
        RecordFeatureModule(report, "Certificates", features.Certificates);
        RecordFeatureModule(report, "Observability", features.Observability);
        RecordFeatureModule(report, "Secrets", features.RequiresSecretsValidation);
    }

    /// <summary>
    /// Writes the structured bootstrap report.
    /// </summary>
    public static void WriteReport(TextWriter output, RuntimeBootstrapReport report)
        => report.Write(output);

    private static void RecordFeatureModule(RuntimeBootstrapReport report, string moduleName, bool enabled)
    {
        if (enabled)
        {
            report.RecordPass(moduleName);
            return;
        }

        report.RecordSkipped(moduleName);
    }
}
