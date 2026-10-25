using ComplianceServiceManager.Logging;
using ComplianceServiceManager.Models;

namespace ComplianceServiceManager.Services;

public sealed class BuildService
{
    private readonly ProcessService processService;
    private readonly ILoggingService logger;

    public BuildService(ProcessService processService, ILoggingService logger)
    {
        this.processService = processService;
        this.logger = logger;
    }

    public async Task<bool> BuildAsync(ServiceInfoModel service, CancellationToken cancellationToken)
    {
        if (service.Kind == ServiceKind.AngularWebApp)
        {
            logger.Build(service.Name, "Skipping dotnet build for Angular WebApp. ng serve will compile the app.");
            return true;
        }

        logger.Build(service.Name, "Build started.");
        var result = await processService.RunCommandAsync(
            "dotnet",
            $"build \"{service.ProjectPath}\" --nologo",
            service.WorkingDirectory,
            service.Name,
            LogLevel.Build,
            cancellationToken);

        if (result.Succeeded)
        {
            logger.Build(service.Name, "Build succeeded.");
            return true;
        }

        logger.Warning(service.Name, $"Build failed with exit code {result.ExitCode}. Retrying without project dependencies (helps when shared dependency DLLs are locked by running services).");
        var retryResult = await processService.RunCommandAsync(
            "dotnet",
            $"build \"{service.ProjectPath}\" --nologo --no-dependencies",
            service.WorkingDirectory,
            service.Name,
            LogLevel.Build,
            cancellationToken);

        if (retryResult.Succeeded)
        {
            logger.Build(service.Name, "Build succeeded on retry without dependencies.");
            return true;
        }

        logger.Error(service.Name, $"Build failed with exit code {retryResult.ExitCode}.");
        return false;
    }
}
