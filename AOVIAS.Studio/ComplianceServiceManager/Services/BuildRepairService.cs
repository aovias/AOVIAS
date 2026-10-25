using ComplianceServiceManager.Logging;
using ComplianceServiceManager.Models;
using Polly;
using Polly.Retry;
using System.IO;

namespace ComplianceServiceManager.Services;

public sealed class BuildRepairService
{
    private readonly ProcessService processService;
    private readonly BuildService buildService;
    private readonly ILoggingService logger;
    private readonly AsyncRetryPolicy<bool> retryPolicy;

    public BuildRepairService(ProcessService processService, BuildService buildService, ILoggingService logger)
    {
        this.processService = processService;
        this.buildService = buildService;
        this.logger = logger;
        retryPolicy = Policy
            .HandleResult<bool>(success => !success)
            .WaitAndRetryAsync(2, attempt => TimeSpan.FromSeconds(attempt * 2), (result, delay, attempt, _) =>
            {
                logger.Warning("BuildRepair", $"Build attempt {attempt} failed. Retrying in {delay.TotalSeconds:0}s.");
            });
    }

    public async Task<bool> RestoreBuildWithSelfHealingAsync(ServiceInfoModel service, CancellationToken cancellationToken)
    {
        return await retryPolicy.ExecuteAsync(async ct =>
        {
            await AutoCleanAsync(service, ct);
            await processService.RunCommandAsync(
                "dotnet",
                $"restore \"{service.ProjectPath}\" --nologo",
                service.WorkingDirectory,
                service.Name,
                LogLevel.Build,
                ct);

            return await buildService.BuildAsync(service, ct);
        }, cancellationToken);
    }

    private async Task AutoCleanAsync(ServiceInfoModel service, CancellationToken cancellationToken)
    {
        var binPath = Path.Combine(service.WorkingDirectory, "bin");
        var objPath = Path.Combine(service.WorkingDirectory, "obj");
        foreach (var path in new[] { binPath, objPath })
        {
            try
            {
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, recursive: true);
                    logger.Info(service.Name, $"Auto-cleaned {path}.");
                }
            }
            catch (Exception ex)
            {
                logger.Warning(service.Name, $"Failed to clean {path}: {ex.Message}");
            }
        }

        await Task.CompletedTask;
    }
}
