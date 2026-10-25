using Aovias.DeploymentValidation.Models;
using Aovias.DeploymentValidation.Services;
using Xunit;

namespace Aovias.DeploymentValidation.Tests;

public sealed class DrvOptimizationTests
{
    [Theory]
    [InlineData(DeploymentValidationMode.Fast, "Package", true)]
    [InlineData(DeploymentValidationMode.Fast, "Contracts", true)]
    [InlineData(DeploymentValidationMode.Fast, "Runtime", false)]
    [InlineData(DeploymentValidationMode.Balanced, "Contracts", true)]
    [InlineData(DeploymentValidationMode.Balanced, "Docker Compose", true)]
    [InlineData(DeploymentValidationMode.Balanced, "Bootstrap Manifest", true)]
    [InlineData(DeploymentValidationMode.Balanced, "Runtime Bootstrap", false)]
    [InlineData(DeploymentValidationMode.Balanced, "Health", false)]
    [InlineData(DeploymentValidationMode.Full, "Health", true)]
    public void ModePolicy_FiltersStages(DeploymentValidationMode mode, string stage, bool expected)
        => Assert.Equal(expected, DeploymentValidationModePolicy.ShouldRunStage(stage, mode));

    [Fact]
    public void DiskBudget_IncludesSafeMargin()
    {
        var budget = DrvResourceMonitor.EstimateDiskBudget(8, DeploymentValidationMode.Balanced);
        Assert.True(budget.MaximumPeakBytes > budget.RequiredBytes);
        Assert.Equal(budget.RequiredBytes + budget.SafeMarginBytes, budget.MaximumPeakBytes);
    }

    [Fact]
    public void ConfigSeeder_DoesNotCopyStorageDirectory()
    {
        var source = Path.Combine(Path.GetTempPath(), "drv-config-src-" + Guid.NewGuid().ToString("N"));
        var target = Path.Combine(Path.GetTempPath(), "drv-config-dst-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(source, "profiles", "local"));
        Directory.CreateDirectory(Path.Combine(source, "storage", "local", "publishing", "validation"));
        File.WriteAllText(Path.Combine(source, "profiles", "local", "settings.api.json"), "{}");
        File.WriteAllText(Path.Combine(source, "storage", "local", "publishing", "validation", "blob.tmp"), "x");

        try
        {
            ValidationConfigSeeder.Seed(source, target, "local");
            Assert.True(Directory.Exists(Path.Combine(target, "profiles", "local")));
            Assert.False(Directory.Exists(Path.Combine(target, "storage")));
        }
        finally
        {
            if (Directory.Exists(source))
            {
                Directory.Delete(source, recursive: true);
            }

            if (Directory.Exists(target))
            {
                Directory.Delete(target, recursive: true);
            }
        }
    }
}
