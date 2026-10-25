using System.Net.Http;
using System.Text.Json;
using Aovias.Bootstrap.Contracts;
using Aovias.Bootstrap.Execution;
using Aovias.Bootstrap.Host;
using Aovias.Configuration.Extensions;
using Aovias.Configuration.Layout;
using Aovias.Configuration.Settings;
using Aovias.Release.Contracts.RemoteDeployment;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Renci.SshNet;

// Sprint M4.1 note: this entrypoint pre-dates M4.1 with a single "deploy" verb that pushes a
// release package to an environment that is already fully prepared (Docker running, config root
// populated, Runtime Agent already installed once). M4.1 ("Zero-Touch Environment Bootstrap") adds
// a second, additive "bootstrap" verb that prepares a *bare* VPS up to and including that starting
// point (Docker install, network, config scaffold, agent install + registration) — a distinct,
// earlier lifecycle stage, not a replacement for "deploy". "deploy"'s existing behavior below is
// unchanged.
if (args.Length == 0)
{
    PrintUsage();
    return 1;
}

return args[0] switch
{
    "deploy" => await RunDeployAsync(args.Skip(1).ToArray()),
    "bootstrap" => await RunBootstrapAsync(args.Skip(1).ToArray()),
    _ => FailWithUsage()
};

static int FailWithUsage()
{
    PrintUsage();
    return 1;
}

static async Task<int> RunDeployAsync(string[] args)
{
    var options = BootstrapOptions.Parse(args);
    if (!File.Exists(options.PackagePath))
    {
        Console.Error.WriteLine($"Package not found: {options.PackagePath}");
        return 1;
    }

    Environment.SetEnvironmentVariable("AOVIAS_CONFIG_ROOT", options.ConfigRoot);
    Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", options.Profile);

    var bootstrapConfiguration = new ConfigurationBuilder()
        .AddJsonFile(Path.Combine(options.ConfigRoot, "aovias.config.json"), optional: false)
        .AddEnvironmentVariables(prefix: "AOVIAS_")
        .Build();

    var configuration = ServiceCollectionExtensions.BuildAoviasConfiguration(
        bootstrapConfiguration,
        options.Profile,
        Path.Combine(options.ConfigRoot, "aovias.config.json"));

    var releaseSettings = configuration.GetSection(ReleaseManagementSettings.SectionName).Get<ReleaseManagementSettings>()
        ?? throw new InvalidOperationException("Release management settings are missing.");
    var nginxSettings = configuration.GetSection(NginxSettings.SectionName).Get<NginxSettings>()
        ?? throw new InvalidOperationException("Nginx settings are missing.");
    var apiSettings = configuration.GetSection(ApiSettings.SectionName).Get<ApiSettings>()
        ?? throw new InvalidOperationException("Api settings are missing.");
    var dockerSettings = configuration.GetSection(DockerSettings.SectionName).Get<DockerSettings>()
        ?? throw new InvalidOperationException("Docker settings are missing.");
    var manifestSettings = configuration.GetSection(RuntimeManifestSettings.SectionName).Get<RuntimeManifestSettings>()
        ?? throw new InvalidOperationException("Runtime manifest settings are missing.");

    Console.WriteLine($"Bootstrap deploy starting for profile '{options.Profile}' -> {options.TargetHost}");

    using var client = CreateSshClient(options);
    client.Connect();

    Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, "bootstrap-temp"));
    var remotePackageName = Path.GetFileName(options.PackagePath);
    var remotePackagePath = $"{releaseSettings.IncomingFolder}/{remotePackageName}";

    EnsureRemoteDirectory(client, releaseSettings.IncomingFolder);
    UploadFile(client, options.PackagePath, remotePackagePath);

    var extractionPath = $"{releaseSettings.ExtractionFolder}/bootstrap-{Guid.NewGuid():N}";
    var commands = new List<string>
    {
        $"mkdir -p \"{extractionPath}\" \"{dockerSettings.ComposeRoot}/runtime-agent\"",
        $"unzip -o \"{remotePackagePath}\" -d \"{extractionPath}\" || tar -xf \"{remotePackagePath}\" -C \"{extractionPath}\"",
        $"if [ -f \"{extractionPath}/service-image.tar\" ]; then docker load -i \"{extractionPath}/service-image.tar\"; fi",
        $"cp \"{extractionPath}/docker-compose.yml\" \"{dockerSettings.ComposeRoot}/runtime-agent/docker-compose.yml\"",
        $"docker compose -f \"{dockerSettings.ComposeRoot}/runtime-agent/docker-compose.yml\" -p aovias-runtime-agent up -d"
    };

    if (nginxSettings.Enabled && File.Exists(nginxSettings.SiteTemplatePath))
    {
        var rendered = RenderNginxTemplate(nginxSettings, configuration);
        var remoteNginxPath = $"{nginxSettings.ConfigRoot}/aovias-runtime-agent.conf";
        UploadText(client, rendered, remoteNginxPath);
        commands.Add($"{nginxSettings.Executable} {nginxSettings.ReloadCommand}");
    }

    foreach (var command in commands)
    {
        Console.WriteLine($"> {command}");
        ExecuteRemote(client, command);
    }

    var healthUrl = $"{apiSettings.BaseUrl.TrimEnd('/')}{apiSettings.HealthEndpoint}";
    if (!await VerifyHealthAsync(healthUrl))
    {
        Console.Error.WriteLine($"Health verification failed at {healthUrl}");
        return 1;
    }

    var manifest = new
    {
        runtimeVersion = configuration.GetSection(RuntimeSettings.SectionName).Get<RuntimeSettings>()?.RuntimeVersion ?? "1.0",
        environment = options.Profile,
        hostname = options.TargetHost,
        serverName = configuration.GetSection(RuntimeSettings.SectionName).Get<RuntimeSettings>()?.ServerName ?? options.TargetHost,
        serverId = configuration.GetSection(RuntimeSettings.SectionName).Get<RuntimeSettings>()?.ServerId ?? options.TargetHost,
        platformVersion = configuration.GetSection(RuntimeSettings.SectionName).Get<RuntimeSettings>()?.PlatformVersion ?? "1.0",
        startedAt = DateTimeOffset.UtcNow,
        lastUpdated = DateTimeOffset.UtcNow
    };

    UploadText(client, JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }), manifestSettings.ManifestPath);
    client.Disconnect();

    Console.WriteLine("Bootstrap complete — connect AOVIAS Command to demo profile.");
    return 0;
}

// Sprint M4.1 "bootstrap" verb: prepares a bare Ubuntu VPS from nothing (Docker install, network,
// config scaffold, Runtime Agent install + registration) via IBootstrapOrchestrator.
// Uses the same SSH.NET dependency as "deploy" above, but through the separate
// IBootstrapTargetExecutor seam (Guideline #9) rather than reusing "deploy"'s
// private local SSH helpers — those are scoped to and validated only against an
// already-config-root-populated environment, whereas bootstrap's very first phase is proving no
// such assumption holds yet, so sharing that code path would tie a not-yet-prepared VPS's success
// to logic written for the opposite precondition.
static async Task<int> RunBootstrapAsync(string[] args)
{
    var arguments = ParseBootstrapArguments(args);
    if (arguments is null)
    {
        Console.Error.WriteLine(
            "Usage: aovias-bootstrap bootstrap --host <host> --user <user> [--port 22] [--key <privateKeyPath> | --password <password>] "
            + "[--sudo-password <password>] "
            + "--profile <profileName> --environment-name <name> --environment-type <LocalDocker|Demo|Qa|Uat|Production|Customer> "
            + "--registration-token <token> --public-url <url> --command-center-url <url> --agent-artifact <localPath> "
            + "--config-root <remoteConfigRoot> --docker-network <name> [--working-directory <path>] [--force]");
        return 1;
    }

    var workingDirectory = arguments.WorkingDirectory ?? Directory.GetCurrentDirectory();

    using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
    var runner = new BootstrapRunner();
    var report = await runner.RunAsync(
        arguments.Target,
        arguments.Force,
        workingDirectory,
        observer: new ConsoleBootstrapObserver(),
        loggerFactory: loggerFactory,
        // Root-cause fix (real-VPS defect): without an elevation handler, any PasswordRequired
        // target failed its very first privileged command with "no elevation handler was supplied".
        elevationHandler: new ConsoleBootstrapElevationHandler(arguments.SudoPassword, arguments.Target.Password));

    Console.WriteLine($"Bootstrap {(report.OverallSuccess ? "SUCCEEDED" : "FAILED")} for '{report.TargetIdentity}'.");
    foreach (var phase in report.Phases)
    {
        Console.WriteLine($"  [{phase.Outcome}] {phase.Phase}: {phase.Message}");
    }

    if (report.ReportPath is not null)
    {
        Console.WriteLine($"Report written to: {report.ReportPath}");
    }

    return report.OverallSuccess ? 0 : 1;
}

static BootstrapArguments? ParseBootstrapArguments(string[] args)
{
    var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    var force = false;

    for (var i = 0; i < args.Length; i++)
    {
        if (string.Equals(args[i], "--force", StringComparison.OrdinalIgnoreCase))
        {
            force = true;
            continue;
        }

        if (!args[i].StartsWith("--", StringComparison.Ordinal) || i + 1 >= args.Length)
        {
            continue;
        }

        map[args[i][2..]] = args[++i];
    }

    if (!map.TryGetValue("host", out var host)
        || !map.TryGetValue("user", out var user)
        || !map.TryGetValue("profile", out var profile)
        || !map.TryGetValue("environment-name", out var environmentName)
        || !map.TryGetValue("registration-token", out var registrationToken)
        || !map.TryGetValue("public-url", out var publicUrl)
        || !map.TryGetValue("command-center-url", out var commandCenterUrl)
        || !map.TryGetValue("agent-artifact", out var agentArtifact)
        || !map.TryGetValue("config-root", out var configRoot)
        || !map.TryGetValue("docker-network", out var dockerNetwork))
    {
        return null;
    }

    if (!map.TryGetValue("environment-type", out var environmentTypeRaw)
        || !Enum.TryParse<EnvironmentType>(environmentTypeRaw, ignoreCase: true, out var environmentType))
    {
        return null;
    }

    // Real-world defect: a CMD-style %USERPROFILE% pasted into PowerShell stays literal, and the
    // resulting "key not found" only surfaced from inside the SSH library at Connect. Expand both
    // %VAR% and shell-agnostic forms here, and fail fast below with the exact resolved path when
    // the file still does not exist — before a single connection attempt.
    var keyPath = map.GetValueOrDefault("key");
    if (keyPath is not null)
    {
        keyPath = Environment.ExpandEnvironmentVariables(keyPath);
        if (!File.Exists(keyPath))
        {
            Console.Error.WriteLine($"Private key file not found: '{keyPath}'. Check the --key path (in PowerShell use $env:USERPROFILE, not %USERPROFILE%).");
            return null;
        }
    }

    var target = new BootstrapTarget
    {
        Host = host,
        Port = map.TryGetValue("port", out var portRaw) && int.TryParse(portRaw, out var port) ? port : 22,
        Username = user,
        PrivateKeyPath = keyPath,
        Password = map.GetValueOrDefault("password"),
        ProfileName = profile,
        EnvironmentName = environmentName,
        EnvironmentType = environmentType,
        RegistrationToken = registrationToken,
        PublicUrl = publicUrl,
        CommandCenterBaseUrl = commandCenterUrl,
        RuntimeAgentArtifactPath = agentArtifact,
        RemoteConfigRoot = configRoot,
        DockerNetworkName = dockerNetwork
    };

    return new BootstrapArguments(target, force, map.GetValueOrDefault("working-directory"), map.GetValueOrDefault("sudo-password"));
}

static SshClient CreateSshClient(BootstrapOptions options)
{
    ConnectionInfo connectionInfo = string.IsNullOrWhiteSpace(options.SshKeyPath)
        ? new ConnectionInfo(options.TargetHost, options.SshUser, new PasswordAuthenticationMethod(options.SshUser, options.SshPassword ?? string.Empty))
        : new ConnectionInfo(options.TargetHost, options.SshUser, new PrivateKeyAuthenticationMethod(options.SshUser, new PrivateKeyFile(options.SshKeyPath)));

    return new SshClient(connectionInfo);
}

static void EnsureRemoteDirectory(SshClient client, string path)
    => ExecuteRemote(client, $"mkdir -p \"{path}\"");

static void UploadFile(SshClient client, string localPath, string remotePath)
{
    using var sftp = new SftpClient(client.ConnectionInfo);
    sftp.Connect();
    using var stream = File.OpenRead(localPath);
    sftp.UploadFile(stream, remotePath, true);
    sftp.Disconnect();
}

static void UploadText(SshClient client, string content, string remotePath)
{
    var temp = Path.GetTempFileName();
    File.WriteAllText(temp, content);
    try
    {
        UploadFile(client, temp, remotePath);
    }
    finally
    {
        File.Delete(temp);
    }
}

static string ExecuteRemote(SshClient client, string command)
{
    using var cmd = client.CreateCommand(command);
    var result = cmd.Execute();
    if (cmd.ExitStatus != 0)
    {
        throw new InvalidOperationException($"Remote command failed ({cmd.ExitStatus}): {cmd.Error}");
    }

    return result;
}

static string RenderNginxTemplate(NginxSettings nginxSettings, IConfiguration configuration)
{
    var template = File.ReadAllText(nginxSettings.SiteTemplatePath);
    var certificate = configuration.GetSection(CertificateSettings.SectionName).Get<CertificateSettings>();
    return template
        .Replace("{{SERVER_NAME}}", nginxSettings.ServerName, StringComparison.Ordinal)
        .Replace("{{SSL_CERTIFICATE_PATH}}", certificate?.CertificatePath ?? string.Empty, StringComparison.Ordinal)
        .Replace("{{SSL_CERTIFICATE_KEY_PATH}}", certificate?.StoreLocation ?? string.Empty, StringComparison.Ordinal)
        .Replace("{{PUBLIC_PATH_PREFIX}}", nginxSettings.PublicPathPrefix.TrimEnd('/'), StringComparison.Ordinal)
        .Replace("{{UPSTREAM_HOST}}", nginxSettings.UpstreamHost, StringComparison.Ordinal)
        .Replace("{{UPSTREAM_PORT}}", nginxSettings.UpstreamPort.ToString(), StringComparison.Ordinal)
        .Replace("{{UPLOAD_BODY_LIMIT_MB}}", nginxSettings.UploadBodyLimitMegabytes.ToString(), StringComparison.Ordinal)
        .Replace("{{UPLOAD_TIMEOUT_SECONDS}}", nginxSettings.UploadTimeoutSeconds.ToString(), StringComparison.Ordinal);
}

static async Task<bool> VerifyHealthAsync(string healthUrl)
{
    using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
    for (var attempt = 0; attempt < 10; attempt++)
    {
        try
        {
            using var response = await http.GetAsync(healthUrl);
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Health OK: {healthUrl}");
                return true;
            }
        }
        catch
        {
            // retry
        }

        await Task.Delay(TimeSpan.FromSeconds(3));
    }

    return false;
}

static void PrintUsage()
{
    Console.WriteLine("""
        AOVIAS Bootstrap Host

        aovias bootstrap deploy
          --package RuntimeAgent.aovdeploy
          --profile demo
          --target vps-hostname-or-ip
          --ssh-user deploy
          [--ssh-key path]
          [--config-root path]

        aovias-bootstrap bootstrap   (Sprint M4.1 — Zero-Touch Environment Bootstrap)
          --host <host> --user <user> [--port 22] [--key <path> | --password <pw>]
          [--sudo-password <pw>]   (required for a non-root user with password sudo, unless
                                    --password is supplied and matches, or running interactively)
          --profile <name> --environment-name <name> --environment-type <type>
          --registration-token <token> --public-url <url> --command-center-url <url>
          --agent-artifact <localPath> --config-root <remoteConfigRoot> --docker-network <name>
          [--working-directory <path>] [--force]
        """);
}

internal sealed record BootstrapArguments(BootstrapTarget Target, bool Force, string? WorkingDirectory, string? SudoPassword);

internal sealed class BootstrapOptions
{
    public required string PackagePath { get; init; }
    public required string Profile { get; init; }
    public required string TargetHost { get; init; }
    public required string SshUser { get; init; }
    public string? SshKeyPath { get; init; }
    public string? SshPassword { get; init; }
    public string ConfigRoot { get; init; } = "";

    public static BootstrapOptions Parse(string[] args)
    {
        string? package = null;
        string profile = "demo";
        string? target = null;
        string sshUser = "deploy";
        string? sshKey = null;
        string? configRoot = null;

        for (var index = 0; index < args.Length; index++)
        {
            switch (args[index])
            {
                case "--package" when index + 1 < args.Length:
                    package = args[++index];
                    break;
                case "--profile" when index + 1 < args.Length:
                    profile = args[++index];
                    break;
                case "--target" when index + 1 < args.Length:
                    target = args[++index];
                    break;
                case "--ssh-user" when index + 1 < args.Length:
                    sshUser = args[++index];
                    break;
                case "--ssh-key" when index + 1 < args.Length:
                    sshKey = args[++index];
                    break;
                case "--config-root" when index + 1 < args.Length:
                    configRoot = args[++index];
                    break;
            }
        }

        if (string.IsNullOrWhiteSpace(package) || string.IsNullOrWhiteSpace(target))
        {
            throw new ArgumentException("Both --package and --target are required.");
        }

        return new BootstrapOptions
        {
            PackagePath = Path.GetFullPath(package),
            Profile = profile,
            TargetHost = target,
            SshUser = sshUser,
            SshKeyPath = sshKey,
            ConfigRoot = configRoot
                ?? Environment.GetEnvironmentVariable("AOVIAS_CONFIG_ROOT")
                ?? RepositoryLayoutAoviasConfigPaths.ResolveConfigRootDirectory(ResolveLocalPlatformRoot())
        };
    }

    private static string ResolveLocalPlatformRoot()
        => RepositoryLayoutProductionAdapter.ResolvePublishingPlatformRoot(AppContext.BaseDirectory);
}
