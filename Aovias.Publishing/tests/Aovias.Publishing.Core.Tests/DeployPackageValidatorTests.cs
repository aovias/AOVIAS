using System.IO.Compression;
using System.Text.Json;
using Aovias.Deployment.Contracts.Reading;
using Aovias.Publishing.Contracts;
using Aovias.Publishing.Core.Packaging;
using Xunit;

namespace Aovias.Publishing.Core.Tests;

public sealed class DeployPackageValidatorTests
{
    [Fact]
    public async Task ValidateAsync_rejects_package_missing_required_files()
    {
        var packagePath = Path.Combine(Path.GetTempPath(), $"invalid-{Guid.NewGuid():N}.aovdeploy");
        ZipFile.CreateFromDirectory(CreateMinimalInvalidPackage(), packagePath);

        try
        {
            var validator = new DeployPackageValidator(new DeployPackageReader(), new DeploymentPackageContractReader());
            var result = await validator.ValidateAsync(packagePath);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, error => error.Contains("service-image.tar", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            File.Delete(packagePath);
        }
    }

    private static string CreateMinimalInvalidPackage()
    {
        var directory = Path.Combine(Path.GetTempPath(), "ape-invalid-package", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);

        File.WriteAllText(
            Path.Combine(directory, DeployPackageLayout.ManifestFile),
            JsonSerializer.Serialize(new
            {
                service = "sample",
                version = "1.0.0",
                type = "AspNetCore",
                container = "sample",
                health = "/health",
                entryPoint = "Sample.dll",
                ports = new[] { 8080 },
                dependencies = Array.Empty<string>(),
                configuration = DeployPackageLayout.ConfigTemplateFile,
                imageName = "sample",
                imageTag = "1.0.0"
            }));

        File.WriteAllText(Path.Combine(directory, DeployPackageLayout.ComposeFile), "services: {}");
        return directory;
    }
}
