using Aovias.DeploymentValidation.Services;
using Xunit;

namespace Aovias.DeploymentValidation.Tests;

public sealed class ValidationComposePortPatcherTests
{
    [Fact]
    public void PatchComposeFile_rewrites_fixed_host_ports_to_dynamic_ports()
    {
        var composePath = Path.Combine(Path.GetTempPath(), "drv-compose-" + Guid.NewGuid().ToString("N") + ".yml");
        File.WriteAllText(
            composePath,
            """
            services:
              identity-service:
                ports:
                - "5062:5062"
            """);

        try
        {
            ValidationComposePortPatcher.PatchComposeFile(composePath);
            var patched = File.ReadAllText(composePath);

            Assert.Contains("\"0:5062\"", patched, StringComparison.Ordinal);
            Assert.DoesNotContain("\"5062:5062\"", patched, StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(composePath);
        }
    }

    [Theory]
    [InlineData("0.0.0.0:49153\n", 49153)]
    [InlineData("[::]:50620", 50620)]
    public void TryParseDockerPortMapping_parses_docker_port_output(string output, int expected)
    {
        Assert.True(ValidationComposePortPatcher.TryParseDockerPortMapping(output, out var hostPort));
        Assert.Equal(expected, hostPort);
    }
}
