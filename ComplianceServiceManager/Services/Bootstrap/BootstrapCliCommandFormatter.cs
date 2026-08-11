using System.Text;
using Aovias.Bootstrap.Contracts;

namespace ComplianceServiceManager.Services.Bootstrap;

/// <summary>
/// Builds the equivalent <c>dotnet run -- bootstrap ...</c> command line shown in the Advanced
/// panel — so an operator can reproduce this exact run outside Studio (a terminal, a CI job) even
/// though Studio itself now runs <see cref="Aovias.Bootstrap.Execution.BootstrapRunner"/> in-process
/// rather than launching this CLI as a child process. The flag set mirrors
/// <c>Aovias.Bootstrap.Host.Program.ParseBootstrapArguments</c> exactly. Secrets (password,
/// registration token) are always masked — this string is for display only, never executed.
/// </summary>
public static class BootstrapCliCommandFormatter
{
    public static string Compose(BootstrapTarget target, bool force, string workingDirectory)
    {
        var builder = new StringBuilder("dotnet run --project \"")
            .Append(RootPathResolver.ResolveBootstrapHostProjectPath())
            .Append("\" -- bootstrap");

        Append(builder, "--host", target.Host);
        Append(builder, "--user", target.Username);
        if (target.Port != 22)
        {
            Append(builder, "--port", target.Port.ToString());
        }

        if (!string.IsNullOrWhiteSpace(target.PrivateKeyPath))
        {
            Append(builder, "--key", target.PrivateKeyPath);
        }
        else if (!string.IsNullOrWhiteSpace(target.Password))
        {
            Append(builder, "--password", "********");
        }

        Append(builder, "--profile", target.ProfileName);
        Append(builder, "--environment-name", target.EnvironmentName);
        Append(builder, "--environment-type", target.EnvironmentType.ToString());
        Append(builder, "--registration-token", "********");
        Append(builder, "--public-url", target.PublicUrl);
        Append(builder, "--command-center-url", target.CommandCenterBaseUrl);
        Append(builder, "--agent-artifact", target.RuntimeAgentArtifactPath);
        Append(builder, "--config-root", target.RemoteConfigRoot);
        Append(builder, "--docker-network", target.DockerNetworkName);
        Append(builder, "--working-directory", workingDirectory);
        if (force)
        {
            builder.Append(" --force");
        }

        return builder.ToString();
    }

    private static void Append(StringBuilder builder, string flag, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        builder.Append(' ').Append(flag).Append(" \"").Append(value.Replace("\"", "\\\"")).Append('"');
    }
}
