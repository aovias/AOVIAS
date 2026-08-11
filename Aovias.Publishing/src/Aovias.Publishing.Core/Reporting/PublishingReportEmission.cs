using System.Text.Json;
using Aovias.Publishing.Contracts;
using Microsoft.Extensions.Logging;

namespace Aovias.Publishing.Core.Reporting;

/// <summary>
/// Writes the sibling publishing report next to the deployment package.
/// </summary>
public static class PublishingReportEmission
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static void EmitSibling(PublishingContext context, ILogger? logger = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(context.PackagePath))
            {
                return;
            }

            var checksumCount = context.PackageVerification?.ChecksumCount
                ?? context.PackageVerificationReport?.ChecksumCount
                ?? 0;
            var report = PublishingReportBuilder.Build(context, context.Request.Profile, context.ReadinessReport, checksumCount);
            var siblingPath = Path.Combine(
                Path.GetDirectoryName(context.PackagePath)!,
                $"{Path.GetFileNameWithoutExtension(context.PackagePath)}.{DeployPackageLayout.PublishingReportFile}");
            File.WriteAllText(siblingPath, JsonSerializer.Serialize(report, JsonOptions));
            context.GeneratedFiles.Add(siblingPath);
        }
        catch (Exception exception)
        {
            logger?.LogWarning(exception, "Failed to emit sibling publishing report.");
        }
    }
}
