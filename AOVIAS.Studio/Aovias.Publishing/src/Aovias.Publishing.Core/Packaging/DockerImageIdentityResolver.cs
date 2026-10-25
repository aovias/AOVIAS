using Aovias.Configuration.Settings;
using Aovias.Publishing.Core.Execution;
using Microsoft.Extensions.Options;

namespace Aovias.Publishing.Core.Packaging;

/// <summary>
/// Resolves Docker image identity via <c>docker image inspect</c> (M1.2 D4).
/// </summary>
public interface IDockerImageIdentityResolver
{
    Task<string?> ResolveAsync(string imageName, string tag, CancellationToken cancellationToken = default);
}

public sealed class DockerImageIdentityResolver : IDockerImageIdentityResolver
{
    private readonly IProcessRunner processRunner;
    private readonly IOptionsMonitor<PublishingSettings> settings;

    public DockerImageIdentityResolver(IProcessRunner processRunner, IOptionsMonitor<PublishingSettings> settings)
    {
        this.processRunner = processRunner;
        this.settings = settings;
    }

    public async Task<string?> ResolveAsync(string imageName, string tag, CancellationToken cancellationToken = default)
    {
        var docker = settings.CurrentValue.DockerExecutable;
        var imageRef = $"{imageName}:{tag}";
        var result = await processRunner.RunAsync(
            docker,
            $"image inspect -f \"{{{{.Id}}}}\" {Quote(imageRef)}",
            Environment.CurrentDirectory,
            settings.CurrentValue.StepTimeoutSeconds,
            cancellationToken);

        return result.IsSuccess && !string.IsNullOrWhiteSpace(result.StandardOutput)
            ? result.StandardOutput.Trim()
            : null;
    }

    private static string Quote(string value) => value.Contains(' ') ? $"\"{value}\"" : value;
}
