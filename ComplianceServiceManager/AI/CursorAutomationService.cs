using ComplianceServiceManager.Logging;
using ComplianceServiceManager.Models;
using ComplianceServiceManager.Services;
using System.IO;

namespace ComplianceServiceManager.AI;

public sealed class CursorAutomationService : ICursorAutomationService
{
    private readonly ProcessService processService;
    private readonly ILoggingService logger;
    private readonly ICursorPromptBuilder promptBuilder;
    private readonly IGitSafetyService gitSafetyService;

    public CursorAutomationService(
        ProcessService processService,
        ILoggingService logger,
        ICursorPromptBuilder promptBuilder,
        IGitSafetyService gitSafetyService)
    {
        this.processService = processService;
        this.logger = logger;
        this.promptBuilder = promptBuilder;
        this.gitSafetyService = gitSafetyService;
    }

    public async Task<IReadOnlyList<string>> ExecuteFixAsync(ServiceInfoModel service, DiagnosticBundle bundle, CancellationToken cancellationToken)
    {
        var prompt = promptBuilder.BuildPrompt(service, bundle);
        await File.WriteAllTextAsync(bundle.PromptPath, prompt, cancellationToken);
        logger.Info(service.Name, $"AI Fix prompt generated at {bundle.PromptPath}");

        var preFiles = await gitSafetyService.GetModifiedFilesAsync(service.WorkingDirectory, cancellationToken);

        var cursorCommand = Environment.GetEnvironmentVariable("COMPLIANCE_CURSOR_CLI_COMMAND");
        if (!string.IsNullOrWhiteSpace(cursorCommand))
        {
            logger.Info(service.Name, "Launching configured Cursor automation command.");
            await processService.RunCommandAsync(
                "cmd",
                $"/c {cursorCommand} \"{bundle.PromptPath}\"",
                service.WorkingDirectory,
                service.Name,
                LogLevel.Process,
                cancellationToken);
        }
        else
        {
            logger.Warning(service.Name, "Cursor CLI command is not configured. Stored prompt and continuing with self-healing pipeline.");
        }

        var postFiles = await gitSafetyService.GetModifiedFilesAsync(service.WorkingDirectory, cancellationToken);
        return postFiles.Except(preFiles, StringComparer.OrdinalIgnoreCase).ToList();
    }
}
