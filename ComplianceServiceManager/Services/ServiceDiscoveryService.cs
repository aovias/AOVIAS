using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using ComplianceServiceManager.Logging;
using ComplianceServiceManager.Models;
using ComplianceServiceManager.Models.SourceContext;
using ComplianceServiceManager.Services.LocalDev;
using ComplianceServiceManager.Services.SourceContexts;

namespace ComplianceServiceManager.Services;

/// <summary>
/// Discovers runnable local development services across discovery roots supplied by a
/// <see cref="SourceContext"/> via <see cref="ISourceDiscoveryBoundary"/>.
/// Shared discovery never reads workspace solution-folder preferences directly.
/// </summary>
public sealed class ServiceDiscoveryService
{
    private static readonly Regex UrlRegex = new(@"(?<scheme>https?)://[^:;]+:(?<port>\d+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private readonly ISourceContextService sourceContexts;
    private readonly ISourceDiscoveryBoundary discoveryBoundary;
    private readonly ILoggingService logger;

    public ServiceDiscoveryService(
        ISourceContextService sourceContexts,
        ISourceDiscoveryBoundary discoveryBoundary,
        ILoggingService logger)
    {
        this.sourceContexts = sourceContexts;
        this.discoveryBoundary = discoveryBoundary;
        this.logger = logger;
    }

    public Task<IReadOnlyList<ServiceInfoModel>> DiscoverAsync(CancellationToken cancellationToken)
        => DiscoverAsync(SourceContextIds.Shared, cancellationToken);

    public async Task<IReadOnlyList<ServiceInfoModel>> DiscoverAsync(
        string contextId,
        CancellationToken cancellationToken)
    {
        var context = await sourceContexts.ResolveAsync(contextId, cancellationToken);
        return await DiscoverAsync(context, cancellationToken);
    }

    public Task<IReadOnlyList<ServiceInfoModel>> DiscoverAsync(
        SourceContext context,
        CancellationToken cancellationToken)
    {
        var roots = discoveryBoundary.GetDiscoveryRoots(context);
        return DiscoverFromRootsAsync(context, roots, cancellationToken);
    }

    internal Task<IReadOnlyList<ServiceInfoModel>> DiscoverFromRootsAsync(
        IReadOnlyList<string> roots,
        CancellationToken cancellationToken)
        => DiscoverFromRootsAsync(
            new SourceContext
            {
                ContextId = SourceContextIds.Shared,
                Kind = SourceContextKind.Shared,
                RootPath = roots.FirstOrDefault() ?? string.Empty
            },
            roots,
            cancellationToken);

    private Task<IReadOnlyList<ServiceInfoModel>> DiscoverFromRootsAsync(
        SourceContext context,
        IReadOnlyList<string> roots,
        CancellationToken cancellationToken)
    {
        return Task.Run(() =>
        {
            var services = new List<ServiceInfoModel>();
            if (roots.Count == 0)
            {
                logger.Warning(
                    "Discovery",
                    $"No discovery roots for {context.DisplayLabel} ({context.RootPath}). Configure workspace folders or verify the source path.");
                return (IReadOnlyList<ServiceInfoModel>)services;
            }

            foreach (var solutionRoot in roots)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!sourceContexts.AcceptsSourcePath(context, solutionRoot))
                {
                    logger.Warning(
                        "Discovery",
                        $"Skipping discovery root outside {context.DisplayLabel}: {solutionRoot}");
                    continue;
                }

                if (!Directory.Exists(solutionRoot))
                {
                    logger.Error("Discovery", $"Solution folder not found: {solutionRoot}");
                    continue;
                }

                services.AddRange(Directory
                    .EnumerateFiles(solutionRoot, "*.csproj", SearchOption.AllDirectories)
                    .Where(path => !IsIgnored(path))
                    .Select(path => TryCreateService(path, solutionRoot))
                    .Where(service => service is not null)
                    .Select(service => service!));

                var angularApp = TryCreateAngularApp(solutionRoot);
                if (angularApp is not null)
                {
                    services.Add(angularApp);
                }
            }

            var ordered = services
                .GroupBy(service => service.ProjectPath, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .OrderBy(service => service.Name)
                .ToList();

            logger.Info(
                "Discovery",
                $"Discovered {ordered.Count} service(s) for {context.DisplayLabel} across {roots.Count} folder(s).");
            return (IReadOnlyList<ServiceInfoModel>)ordered;
        }, cancellationToken);
    }

    private ServiceInfoModel? TryCreateService(string projectPath, string solutionRoot)
    {
        try
        {
            var document = XDocument.Load(projectPath);
            var sdk = document.Root?.Attribute("Sdk")?.Value ?? string.Empty;
            var xml = document.ToString(SaveOptions.DisableFormatting);
            var isWebSdk = sdk.Contains("Microsoft.NET.Sdk.Web", StringComparison.OrdinalIgnoreCase);
            var hasAspNetReferences = xml.Contains("Microsoft.AspNetCore", StringComparison.OrdinalIgnoreCase);
            var isTestProject = projectPath.Contains(".Tests", StringComparison.OrdinalIgnoreCase)
                || xml.Contains("Microsoft.NET.Test.Sdk", StringComparison.OrdinalIgnoreCase);
            var workingDirectory = Path.GetDirectoryName(projectPath) ?? solutionRoot;
            var hasProgramFile = File.Exists(Path.Combine(workingDirectory, "Program.cs"));

            if ((!isWebSdk && !hasAspNetReferences) || isTestProject || !hasProgramFile)
            {
                return null;
            }

            var selectedProfile = TryReadLaunchProfile(workingDirectory);
            var fallbackUrls = selectedProfile is null ? TryReadKestrelFallbackUrls(workingDirectory) : null;
            var projectName = Path.GetFileNameWithoutExtension(projectPath);
            var environmentName = selectedProfile?.EnvironmentVariables.TryGetValue("ASPNETCORE_ENVIRONMENT", out var aspnetEnvironment) == true
                ? aspnetEnvironment
                : "Development";

            if (selectedProfile is null)
            {
                logger.Warning(projectName, "launchSettings.json not found or no usable profile was detected. Falling back to appsettings.Development.json/ASPNETCORE_URLS/default dotnet run.");
            }

            return new ServiceInfoModel
            {
                Name = projectName,
                Kind = ServiceKind.WebApi,
                ProjectPath = projectPath,
                WorkingDirectory = workingDirectory,
                LaunchUrl = selectedProfile?.LaunchUrl,
                Port = selectedProfile?.HttpsPort ?? selectedProfile?.HttpPort ?? fallbackUrls?.HttpsPort ?? fallbackUrls?.HttpPort,
                LaunchProfileName = selectedProfile?.Name,
                HttpsUrl = selectedProfile?.HttpsUrl ?? fallbackUrls?.HttpsUrl,
                HttpsPort = selectedProfile?.HttpsPort ?? fallbackUrls?.HttpsPort,
                HttpUrl = selectedProfile?.HttpUrl ?? fallbackUrls?.HttpUrl,
                HttpPort = selectedProfile?.HttpPort ?? fallbackUrls?.HttpPort,
                EnvironmentName = environmentName,
                Environment = environmentName,
                LaunchEnvironmentVariables = selectedProfile?.EnvironmentVariables ?? new Dictionary<string, string>(),
                LastAction = "Discovered"
            };
        }
        catch (Exception ex)
        {
            logger.Warning("Discovery", $"Skipping {projectPath}: {ex.Message}");
            return null;
        }
    }

    private ServiceInfoModel? TryCreateAngularApp(string solutionRoot)
    {
        try
        {
            var packageJsonPath = Path.Combine(solutionRoot, "package.json");
            var angularJsonPath = Path.Combine(solutionRoot, "angular.json");
            if (!File.Exists(packageJsonPath) || !File.Exists(angularJsonPath))
            {
                return null;
            }

            var port = TryReadAngularServePort(angularJsonPath) ?? 4200;
            var name = Path.GetFileName(solutionRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            return new ServiceInfoModel
            {
                Name = string.IsNullOrWhiteSpace(name) ? "Angular App" : name,
                Kind = ServiceKind.AngularWebApp,
                ProjectPath = packageJsonPath,
                WorkingDirectory = solutionRoot,
                LaunchUrl = $"http://localhost:{port}",
                HttpUrl = $"http://localhost:{port}",
                HttpPort = port,
                Port = port,
                LaunchProfileName = "ng serve",
                EnvironmentName = "Development",
                Environment = "Development",
                LastAction = "Discovered"
            };
        }
        catch (Exception ex)
        {
            logger.Warning("Discovery", $"Skipping Angular app in {solutionRoot}: {ex.Message}");
            return null;
        }
    }

    private static bool IsIgnored(string path)
    {
        return path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
            || path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
            || path.Contains($"{Path.DirectorySeparatorChar}.artifacts{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
            || path.Contains($"{Path.DirectorySeparatorChar}tests{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase);
    }

    private static LaunchProfileModel? TryReadLaunchProfile(string workingDirectory)
    {
        var launchSettingsPath = Path.Combine(workingDirectory, "Properties", "launchSettings.json");
        if (!File.Exists(launchSettingsPath))
        {
            return null;
        }

        using var stream = File.OpenRead(launchSettingsPath);
        using var document = JsonDocument.Parse(stream, new JsonDocumentOptions
        {
            AllowTrailingCommas = true,
            CommentHandling = JsonCommentHandling.Skip
        });

        if (!document.RootElement.TryGetProperty("profiles", out var profiles))
        {
            return null;
        }

        var launchProfiles = new List<LaunchProfileModel>();
        foreach (var profile in profiles.EnumerateObject())
        {
            var value = profile.Value;
            var applicationUrl = value.TryGetProperty("applicationUrl", out var applicationUrlValue)
                ? applicationUrlValue.GetString()
                : null;
            var urls = ParseApplicationUrls(applicationUrl);
            var environmentVariables = ReadEnvironmentVariables(value);

            launchProfiles.Add(new LaunchProfileModel
            {
                Name = profile.Name,
                CommandName = value.TryGetProperty("commandName", out var commandName) ? commandName.GetString() ?? string.Empty : string.Empty,
                ApplicationUrl = applicationUrl,
                HttpsUrl = urls.HttpsUrl,
                HttpsPort = urls.HttpsPort,
                HttpUrl = urls.HttpUrl,
                HttpPort = urls.HttpPort,
                LaunchUrl = value.TryGetProperty("launchUrl", out var launchUrl) ? launchUrl.GetString() : null,
                LaunchBrowser = value.TryGetProperty("launchBrowser", out var launchBrowser) && launchBrowser.ValueKind == JsonValueKind.True,
                EnvironmentVariables = environmentVariables
            });
        }

        return launchProfiles
            .OrderByDescending(profile => profile.HasHttps)
            .ThenByDescending(profile => profile.IsProjectProfile)
            .FirstOrDefault();
    }

    private static int? TryReadAngularServePort(string angularJsonPath)
    {
        try
        {
            using var stream = File.OpenRead(angularJsonPath);
            using var document = JsonDocument.Parse(stream, new JsonDocumentOptions
            {
                AllowTrailingCommas = true,
                CommentHandling = JsonCommentHandling.Skip
            });

            if (!document.RootElement.TryGetProperty("projects", out var projects))
            {
                return null;
            }

            foreach (var project in projects.EnumerateObject())
            {
                if (project.Value.TryGetProperty("architect", out var architect)
                    && architect.TryGetProperty("serve", out var serve)
                    && serve.TryGetProperty("options", out var options)
                    && options.TryGetProperty("port", out var port)
                    && port.TryGetInt32(out var portValue))
                {
                    return portValue;
                }
            }
        }
        catch
        {
        }

        return null;
    }

    private static (string? HttpsUrl, int? HttpsPort, string? HttpUrl, int? HttpPort) ParseApplicationUrls(string? applicationUrl)
    {
        if (string.IsNullOrWhiteSpace(applicationUrl))
        {
            return default;
        }

        string? httpsUrl = null;
        string? httpUrl = null;
        int? httpsPort = null;
        int? httpPort = null;

        foreach (var candidate in applicationUrl.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var match = UrlRegex.Match(candidate);
            if (!match.Success || !int.TryParse(match.Groups["port"].Value, out var port))
            {
                continue;
            }

            if (match.Groups["scheme"].Value.Equals("https", StringComparison.OrdinalIgnoreCase) && httpsUrl is null)
            {
                httpsUrl = candidate.TrimEnd('/');
                httpsPort = port;
            }
            else if (match.Groups["scheme"].Value.Equals("http", StringComparison.OrdinalIgnoreCase) && httpUrl is null)
            {
                httpUrl = candidate.TrimEnd('/');
                httpPort = port;
            }
        }

        return (httpsUrl, httpsPort, httpUrl, httpPort);
    }

    private static Dictionary<string, string> ReadEnvironmentVariables(JsonElement profile)
    {
        var variables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!profile.TryGetProperty("environmentVariables", out var environmentVariables))
        {
            return variables;
        }

        foreach (var variable in environmentVariables.EnumerateObject())
        {
            variables[variable.Name] = variable.Value.GetString() ?? string.Empty;
        }

        return variables;
    }

    private static (string? HttpsUrl, int? HttpsPort, string? HttpUrl, int? HttpPort)? TryReadKestrelFallbackUrls(string workingDirectory)
    {
        var appsettingsPath = Path.Combine(workingDirectory, "appsettings.Development.json");
        if (!File.Exists(appsettingsPath))
        {
            return null;
        }

        try
        {
            using var stream = File.OpenRead(appsettingsPath);
            using var document = JsonDocument.Parse(stream, new JsonDocumentOptions
            {
                AllowTrailingCommas = true,
                CommentHandling = JsonCommentHandling.Skip
            });

            if (!document.RootElement.TryGetProperty("Kestrel", out var kestrel)
                || !kestrel.TryGetProperty("Endpoints", out var endpoints))
            {
                return null;
            }

            string? httpsUrl = null;
            string? httpUrl = null;
            foreach (var endpoint in endpoints.EnumerateObject())
            {
                if (!endpoint.Value.TryGetProperty("Url", out var urlValue))
                {
                    continue;
                }

                var url = urlValue.GetString();
                if (url?.StartsWith("https://", StringComparison.OrdinalIgnoreCase) == true)
                {
                    httpsUrl ??= url;
                }
                else if (url?.StartsWith("http://", StringComparison.OrdinalIgnoreCase) == true)
                {
                    httpUrl ??= url;
                }
            }

            var parsed = ParseApplicationUrls(string.Join(';', new[] { httpsUrl, httpUrl }.Where(url => !string.IsNullOrWhiteSpace(url))));
            return parsed;
        }
        catch
        {
            return null;
        }
    }
}
