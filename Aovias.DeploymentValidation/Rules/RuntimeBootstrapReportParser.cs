namespace Aovias.DeploymentValidation.Rules;

/// <summary>
/// Parses the textual "Runtime Bootstrap Report" emitted by the Runtime Agent on
/// startup (see Aovias.Configuration.Loading.RuntimeBootstrapReport).
/// </summary>
public sealed record RuntimeBootstrapParseResult(
    bool ReportFound,
    bool StartupSucceeded,
    IReadOnlyList<string> FailedModules);

/// <summary>
/// Extracts structured startup status from raw container output.
/// </summary>
public static class RuntimeBootstrapReportParser
{
    private const string ReportHeader = "Runtime Bootstrap Report";
    private const string StartupMarker = "Startup";

    public static RuntimeBootstrapParseResult Parse(string output)
    {
        if (string.IsNullOrWhiteSpace(output) || !output.Contains(ReportHeader, StringComparison.OrdinalIgnoreCase))
        {
            return new RuntimeBootstrapParseResult(false, false, []);
        }

        var lines = output
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .ToList();

        var startupSucceeded = false;
        var failedModules = new List<string>();

        for (var index = 0; index < lines.Count; index++)
        {
            var line = lines[index];

            if (line.Equals(StartupMarker, StringComparison.OrdinalIgnoreCase) && index + 1 < lines.Count)
            {
                startupSucceeded = lines[index + 1].Equals("SUCCESS", StringComparison.OrdinalIgnoreCase);
            }

            // A module line is the module name immediately followed by its status.
            if (line.Equals("FAILED", StringComparison.OrdinalIgnoreCase) && index > 0)
            {
                failedModules.Add(lines[index - 1]);
            }
        }

        return new RuntimeBootstrapParseResult(true, startupSucceeded, failedModules);
    }
}
