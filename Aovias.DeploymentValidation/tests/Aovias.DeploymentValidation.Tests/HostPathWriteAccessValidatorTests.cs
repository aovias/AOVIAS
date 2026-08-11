using Aovias.Deployment.Contracts.Provisioning;
using Xunit;

namespace Aovias.DeploymentValidation.Tests;

public sealed class HostPathWriteAccessValidatorTests
{
    [Fact]
    public void ValidateReadable_existingDirectory_passes()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        File.WriteAllText(Path.Combine(path, "probe.txt"), "ok");

        try
        {
            var errors = HostPathWriteAccessValidator.ValidateReadable(path);
            Assert.Empty(errors);
        }
        finally
        {
            Directory.Delete(path, recursive: true);
        }
    }

    [Fact]
    public void ValidateWritable_existingDirectory_passes()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);

        try
        {
            var errors = HostPathWriteAccessValidator.ValidateWritable(path);
            Assert.Empty(errors);
        }
        finally
        {
            Directory.Delete(path, recursive: true);
        }
    }
}
