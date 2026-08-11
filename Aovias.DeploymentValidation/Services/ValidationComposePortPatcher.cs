using System.Text.RegularExpressions;

namespace Aovias.DeploymentValidation.Services;

/// <summary>
/// Patches extracted deployment compose files for isolated validation runs.
/// </summary>
public static class ValidationComposePortPatcher
{
    private static readonly Regex PortMappingPattern = new(
        @"(?<quote>""?)(?<host>\d+):(?<container>\d+)\k<quote>",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>
    /// Rewrites fixed host port mappings to dynamic ports so DRV does not conflict with local dev services.
    /// Only the validation workspace copy is modified; packaged artifacts remain unchanged.
    /// </summary>
    public static void PatchComposeFile(string composeFilePath)
    {
        if (!File.Exists(composeFilePath))
        {
            return;
        }

        var content = File.ReadAllText(composeFilePath);
        var patched = PortMappingPattern.Replace(
            content,
            match => match.Groups["quote"].Value + "0:" + match.Groups["container"].Value + match.Groups["quote"].Value);

        if (!string.Equals(content, patched, StringComparison.Ordinal))
        {
            File.WriteAllText(composeFilePath, patched);
        }
    }

    public static bool TryParseDockerPortMapping(string output, out int hostPort)
    {
        hostPort = 0;

        foreach (var line in output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var separatorIndex = line.LastIndexOf(':');
            if (separatorIndex < 0 || separatorIndex == line.Length - 1)
            {
                continue;
            }

            if (int.TryParse(line[(separatorIndex + 1)..], out hostPort) && hostPort > 0)
            {
                return true;
            }
        }

        return false;
    }
}
