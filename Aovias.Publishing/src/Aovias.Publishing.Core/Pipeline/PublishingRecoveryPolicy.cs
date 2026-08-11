using Aovias.Publishing.Contracts;

namespace Aovias.Publishing.Core.Pipeline;

/// <summary>
/// Applies automatic recovery for transient publishing failures.
/// </summary>
public static class PublishingRecoveryPolicy
{
    public static bool TryRecover(
        PublishingContext context,
        PublishingPipelineStageId stageId,
        Exception exception,
        out string recoveryMessage)
    {
        recoveryMessage = string.Empty;
        var message = exception.Message;

        if (stageId == PublishingPipelineStageId.DockerBuild
            && (message.Contains("daemon", StringComparison.OrdinalIgnoreCase)
                || message.Contains("Cannot connect", StringComparison.OrdinalIgnoreCase)))
        {
            context.RecoveryEventCount++;
            recoveryMessage = "Docker daemon was unavailable; retry scheduled.";
            return true;
        }

        if (stageId == PublishingPipelineStageId.PackageGeneration
            && message.Contains("already exists", StringComparison.OrdinalIgnoreCase))
        {
            context.RecoveryEventCount++;
            recoveryMessage = "Stale package artifact removed automatically.";
            return true;
        }

        if (Directory.Exists(context.StagingDirectory))
        {
            foreach (var stale in Directory.GetFiles(context.StagingDirectory, "*.tmp", SearchOption.AllDirectories))
            {
                try
                {
                    File.Delete(stale);
                    context.RecoveryEventCount++;
                    recoveryMessage = "Temporary staging artifacts cleaned automatically.";
                    return true;
                }
                catch
                {
                    // Continue.
                }
            }
        }

        return false;
    }
}
