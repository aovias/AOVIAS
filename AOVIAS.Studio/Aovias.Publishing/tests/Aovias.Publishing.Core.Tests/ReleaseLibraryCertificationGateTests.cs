using System.IO.Compression;
using Aovias.Publishing.Contracts;
using Xunit;

namespace Aovias.Publishing.Core.Tests;

public sealed class ReleaseLibraryCertificationGateTests
{
    [Fact]
    public void ReleaseLibraryGate_RequiresDeploymentReadinessCertificate()
    {
        var packagePath = Path.Combine(Path.GetTempPath(), $"gate-{Guid.NewGuid():N}.aovdeploy");
        try
        {
            using (var archive = ZipFile.Open(packagePath, ZipArchiveMode.Create))
            {
                foreach (var required in DeployPackageLayout.RequiredFiles)
                {
                    if (required == DeployPackageLayout.DeploymentReadinessCertificateFile)
                    {
                        continue;
                    }

                    var entry = archive.CreateEntry(required);
                    using var writer = new StreamWriter(entry.Open());
                    writer.Write("{}");
                }
            }

            using var readArchive = ZipFile.OpenRead(packagePath);
            Assert.Null(readArchive.GetEntry(DeployPackageLayout.DeploymentReadinessCertificateFile));
        }
        finally
        {
            if (File.Exists(packagePath))
            {
                File.Delete(packagePath);
            }
        }
    }
}
