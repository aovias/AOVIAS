using Aovias.Publishing.Core.Generation;
using Xunit;

namespace Aovias.Publishing.Core.Tests;

public sealed class RuntimeAgentDockerContextStagerTests
{
    [Fact]
    public void ResolveRequiredMonorepoFolders_includes_deployment_validation_from_dockerfile()
    {
        var dockerfile = """
            COPY Aovias.Configuration/ Aovias.Configuration/
            COPY Aovias.Deployment.Contracts/ Aovias.Deployment.Contracts/
            COPY Aovias.DeploymentValidation/ Aovias.DeploymentValidation/
            COPY Aovias.Runtime.Agent/src/ Aovias.Runtime.Agent/src/
            """;

        var folders = RuntimeAgentDockerContextStager.ResolveRequiredMonorepoFolders(dockerfile);

        Assert.Contains("Aovias.DeploymentValidation", folders);
        Assert.Contains("Aovias.Deployment.Contracts", folders);
        Assert.Contains("Aovias.Configuration", folders);
        Assert.Contains("Aovias.Runtime.Agent", folders);
    }

    [Fact]
    public void Stage_copies_deployment_validation_into_build_context()
    {
        var platformRoot = ResolvePlatformRoot();
        var dockerfile = File.ReadAllText(Path.Combine(platformRoot, "Aovias.Runtime.Agent", "Dockerfile"));
        var stagingRoot = Path.Combine(Path.GetTempPath(), "runtime-agent-stager-test", Guid.NewGuid().ToString("N"));

        try
        {
            RuntimeAgentDockerContextStager.Stage(platformRoot, stagingRoot, dockerfile);

            Assert.True(
                Directory.Exists(Path.Combine(stagingRoot, "Aovias.DeploymentValidation")),
                "Staged Docker context must include Aovias.DeploymentValidation.");
            Assert.True(
                Directory.Exists(Path.Combine(stagingRoot, "Aovias.Deployment.Contracts")),
                "Staged Docker context must include Aovias.Deployment.Contracts.");
            RuntimeAgentDockerContextStager.ValidateStagedContext(stagingRoot, dockerfile);
        }
        finally
        {
            if (Directory.Exists(stagingRoot))
            {
                Directory.Delete(stagingRoot, recursive: true);
            }
        }
    }

    private static string ResolvePlatformRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (Directory.Exists(Path.Combine(current.FullName, "Aovias.Configuration"))
                && Directory.Exists(Path.Combine(current.FullName, "Aovias.Runtime.Agent")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Unable to locate the AOVIAS platform root for stager tests.");
    }
}
