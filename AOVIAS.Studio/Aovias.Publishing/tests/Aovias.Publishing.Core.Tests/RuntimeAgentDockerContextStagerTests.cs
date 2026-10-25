using Aovias.Publishing.Core.Generation;
using Xunit;

namespace Aovias.Publishing.Core.Tests;

public sealed class RuntimeAgentDockerContextStagerTests
{
    [Fact]
    public void ResolveRequiredMonorepoFolders_includes_deployment_validation_from_dockerfile()
    {
        var dockerfile = """
            COPY AOVIAS.Studio/Aovias.Configuration/ AOVIAS.Studio/Aovias.Configuration/
            COPY AOVIAS.Studio/Aovias.Deployment.Contracts/ AOVIAS.Studio/Aovias.Deployment.Contracts/
            COPY AOVIAS.Studio/Aovias.DeploymentValidation/ AOVIAS.Studio/Aovias.DeploymentValidation/
            COPY AOVIAS.Studio/Aovias.Runtime.Agent/Directory.Build.props AOVIAS.Studio/Aovias.Runtime.Agent/
            COPY AOVIAS.Studio/Aovias.Runtime.Agent/src/ AOVIAS.Studio/Aovias.Runtime.Agent/src/
            """;

        var folders = RuntimeAgentDockerContextStager.ResolveRequiredMonorepoFolders(dockerfile);

        Assert.Contains("AOVIAS.Studio/Aovias.DeploymentValidation", folders);
        Assert.Contains("AOVIAS.Studio/Aovias.Deployment.Contracts", folders);
        Assert.Contains("AOVIAS.Studio/Aovias.Configuration", folders);
        Assert.Contains("AOVIAS.Studio/Aovias.Runtime.Agent", folders);
    }

    [Fact]
    public void Stage_copies_deployment_validation_into_build_context()
    {
        var platformRoot = ResolvePlatformRoot();
        var dockerfile = File.ReadAllText(Path.Combine(platformRoot, "AOVIAS.Studio", "Aovias.Runtime.Agent", "Dockerfile"));
        var stagingRoot = Path.Combine(Path.GetTempPath(), "runtime-agent-stager-test", Guid.NewGuid().ToString("N"));

        try
        {
            RuntimeAgentDockerContextStager.Stage(platformRoot, stagingRoot, dockerfile);

            Assert.True(
                Directory.Exists(Path.Combine(stagingRoot, "AOVIAS.Studio", "Aovias.DeploymentValidation")),
                "Staged Docker context must include AOVIAS.Studio/Aovias.DeploymentValidation.");
            Assert.True(
                Directory.Exists(Path.Combine(stagingRoot, "AOVIAS.Studio", "Aovias.Deployment.Contracts")),
                "Staged Docker context must include AOVIAS.Studio/Aovias.Deployment.Contracts.");
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
            if (Directory.Exists(Path.Combine(current.FullName, "AOVIAS.Studio", "Aovias.Configuration"))
                && Directory.Exists(Path.Combine(current.FullName, "AOVIAS.Studio", "Aovias.Runtime.Agent")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Unable to locate the AOVIAS platform root for stager tests.");
    }
}
