using Aovias.Configuration.Settings;
using Microsoft.Extensions.Options;

namespace Aovias.Publishing.Core.Execution;

/// <summary>
/// Executes Docker CLI commands.
/// </summary>
public interface IDockerCliExecutor
{
    Task<ProcessExecutionResult> BuildAsync(
        string contextDirectory,
        string dockerfilePath,
        string imageName,
        string tag,
        CancellationToken cancellationToken,
        IReadOnlyDictionary<string, string>? additionalBuildArgs = null,
        bool noCache = false);

    Task<ProcessExecutionResult> SaveAsync(string imageName, string tag, string outputTarPath, CancellationToken cancellationToken);

    Task<ProcessExecutionResult> InspectAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Executes dotnet CLI commands.
/// </summary>
public interface IDotNetCliExecutor
{
    Task<ProcessExecutionResult> RestoreAsync(string projectPath, string workingDirectory, CancellationToken cancellationToken);

    Task<ProcessExecutionResult> BuildAsync(string projectPath, string workingDirectory, CancellationToken cancellationToken);

    Task<ProcessExecutionResult> PublishAsync(string projectPath, string outputDirectory, string workingDirectory, IReadOnlyDictionary<string, string> publishProperties, CancellationToken cancellationToken);

    /// <summary>
    /// Sprint M3.3: runs <c>dotnet test</c> against a project or solution path, for the
    /// end-to-end verification pipeline's Test stage. Accepts a <c>.csproj</c> or <c>.slnx</c>
    /// path identically to <see cref="BuildAsync"/>.
    /// </summary>
    Task<ProcessExecutionResult> TestAsync(string projectOrSolutionPath, string workingDirectory, CancellationToken cancellationToken);

    Task<ProcessExecutionResult> InspectAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Executes Node/npm CLI commands.
/// </summary>
public interface INodeCliExecutor
{
    Task<ProcessExecutionResult> InstallAsync(string workingDirectory, CancellationToken cancellationToken);

    Task<ProcessExecutionResult> RunNpmAsync(string arguments, string workingDirectory, CancellationToken cancellationToken);

    Task<ProcessExecutionResult> InspectAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Docker CLI adapter.
/// </summary>
public sealed class DockerCliExecutor : IDockerCliExecutor
{
    private readonly IProcessRunner processRunner;
    private readonly IOptionsMonitor<PublishingSettings> settings;

    public DockerCliExecutor(IProcessRunner processRunner, IOptionsMonitor<PublishingSettings> settings)
    {
        this.processRunner = processRunner;
        this.settings = settings;
    }

    /// <inheritdoc />
    public Task<ProcessExecutionResult> BuildAsync(
        string contextDirectory,
        string dockerfilePath,
        string imageName,
        string tag,
        CancellationToken cancellationToken,
        IReadOnlyDictionary<string, string>? additionalBuildArgs = null,
        bool noCache = false)
    {
        var docker = settings.CurrentValue.DockerExecutable;
        var timeout = settings.CurrentValue.StepTimeoutSeconds;
        var args = new List<string> { "build", "-f", Quote(dockerfilePath), "-t", $"{imageName}:{tag}" };

        if (noCache)
        {
            args.Add("--no-cache");
        }

        foreach (var pair in settings.CurrentValue.DockerBuildArgs)
        {
            args.Add("--build-arg");
            args.Add($"{pair.Key}={pair.Value}");
        }

        if (additionalBuildArgs is not null)
        {
            foreach (var pair in additionalBuildArgs)
            {
                args.Add("--build-arg");
                args.Add($"{pair.Key}={pair.Value}");
            }
        }

        args.Add(Quote(contextDirectory));
        return processRunner.RunAsync(docker, string.Join(' ', args), contextDirectory, timeout, cancellationToken);
    }

    /// <inheritdoc />
    public Task<ProcessExecutionResult> SaveAsync(string imageName, string tag, string outputTarPath, CancellationToken cancellationToken)
    {
        var docker = settings.CurrentValue.DockerExecutable;
        var timeout = settings.CurrentValue.StepTimeoutSeconds;
        return processRunner.RunAsync(
            docker,
            $"save -o {Quote(outputTarPath)} {imageName}:{tag}",
            Path.GetDirectoryName(outputTarPath) ?? string.Empty,
            timeout,
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<ProcessExecutionResult> InspectAsync(CancellationToken cancellationToken)
        => processRunner.RunAsync(settings.CurrentValue.DockerExecutable, "version", Environment.CurrentDirectory, 30, cancellationToken);

    private static string Quote(string value) => value.Contains(' ') ? $"\"{value}\"" : value;
}

/// <summary>
/// Dotnet CLI adapter.
/// </summary>
public sealed class DotNetCliExecutor : IDotNetCliExecutor
{
    private readonly IProcessRunner processRunner;
    private readonly IOptionsMonitor<PublishingSettings> settings;

    public DotNetCliExecutor(IProcessRunner processRunner, IOptionsMonitor<PublishingSettings> settings)
    {
        this.processRunner = processRunner;
        this.settings = settings;
    }

    /// <inheritdoc />
    public Task<ProcessExecutionResult> RestoreAsync(string projectPath, string workingDirectory, CancellationToken cancellationToken)
        => RunDotNet($"restore {Quote(projectPath)} --nologo", workingDirectory, cancellationToken);

    /// <inheritdoc />
    public Task<ProcessExecutionResult> BuildAsync(string projectPath, string workingDirectory, CancellationToken cancellationToken)
        => RunDotNet($"build {Quote(projectPath)} -c Release --nologo", workingDirectory, cancellationToken);

    /// <inheritdoc />
    public Task<ProcessExecutionResult> PublishAsync(string projectPath, string outputDirectory, string workingDirectory, IReadOnlyDictionary<string, string> publishProperties, CancellationToken cancellationToken)
    {
        var properties = publishProperties is { Count: > 0 }
            ? " " + string.Join(' ', publishProperties.Select(pair => $"/p:{pair.Key}={pair.Value}"))
            : string.Empty;

        return RunDotNet(
            $"publish {Quote(projectPath)} -c Release -o {Quote(outputDirectory)}{properties} --nologo",
            workingDirectory,
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<ProcessExecutionResult> TestAsync(string projectOrSolutionPath, string workingDirectory, CancellationToken cancellationToken)
        => RunDotNet(
            $"test {Quote(projectOrSolutionPath)} -c Release --nologo -p:ContinuousIntegrationBuild=true",
            workingDirectory,
            cancellationToken);

    /// <inheritdoc />
    public Task<ProcessExecutionResult> InspectAsync(CancellationToken cancellationToken)
        => RunDotNet("--version", Environment.CurrentDirectory, cancellationToken);

    private Task<ProcessExecutionResult> RunDotNet(string arguments, string workingDirectory, CancellationToken cancellationToken)
        => processRunner.RunAsync(
            settings.CurrentValue.DotNetExecutable,
            arguments,
            workingDirectory,
            settings.CurrentValue.StepTimeoutSeconds,
            cancellationToken);

    private static string Quote(string value) => value.Contains(' ') ? $"\"{value}\"" : value;
}

/// <summary>
/// Node/npm CLI adapter.
/// </summary>
public sealed class NodeCliExecutor : INodeCliExecutor
{
    private readonly IProcessRunner processRunner;
    private readonly IOptionsMonitor<PublishingSettings> settings;

    public NodeCliExecutor(IProcessRunner processRunner, IOptionsMonitor<PublishingSettings> settings)
    {
        this.processRunner = processRunner;
        this.settings = settings;
    }

    /// <inheritdoc />
    public async Task<ProcessExecutionResult> InstallAsync(string workingDirectory, CancellationToken cancellationToken)
        => await RunNpmAsync("ci", workingDirectory, cancellationToken);

    /// <inheritdoc />
    public async Task<ProcessExecutionResult> RunNpmAsync(string arguments, string workingDirectory, CancellationToken cancellationToken)
    {
        ProcessExecutionResult? last = null;
        foreach (var executable in ResolveNpmCandidates())
        {
            last = await processRunner.RunAsync(
                executable,
                arguments,
                workingDirectory,
                settings.CurrentValue.StepTimeoutSeconds,
                cancellationToken);

            if (!IsStartFailure(last))
            {
                return last;
            }
        }

        return last ?? new ProcessExecutionResult
        {
            ExitCode = -1,
            StandardError = "Unable to start npm."
        };
    }

    /// <inheritdoc />
    public async Task<ProcessExecutionResult> InspectAsync(CancellationToken cancellationToken)
    {
        ProcessExecutionResult? last = null;
        foreach (var executable in ResolveNodeCandidates())
        {
            last = await processRunner.RunAsync(
                executable,
                "--version",
                Environment.CurrentDirectory,
                30,
                cancellationToken);

            if (!IsStartFailure(last))
            {
                return last;
            }
        }

        return last ?? new ProcessExecutionResult
        {
            ExitCode = -1,
            StandardError = "Unable to start node."
        };
    }

    private IEnumerable<string> ResolveNpmCandidates()
    {
        if (OperatingSystem.IsWindows())
        {
            var programFilesNpm = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                "nodejs",
                "npm.cmd");
            if (File.Exists(programFilesNpm))
            {
                yield return programFilesNpm;
            }

            yield return "npm.cmd";
        }

        yield return settings.CurrentValue.NpmExecutable;
    }

    private IEnumerable<string> ResolveNodeCandidates()
    {
        if (OperatingSystem.IsWindows())
        {
            var programFilesNode = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                "nodejs",
                "node.exe");
            if (File.Exists(programFilesNode))
            {
                yield return programFilesNode;
            }

            yield return "node.exe";
        }

        yield return settings.CurrentValue.NodeExecutable;
    }

    private static bool IsStartFailure(ProcessExecutionResult result)
        => result.ExitCode == -1
           && result.StandardError.Contains("Unable to start", StringComparison.OrdinalIgnoreCase);
}
