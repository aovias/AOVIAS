using System.Text;
using System.Text.Json;
using Aovias.Bootstrap.Contracts;
using Aovias.Bootstrap.Core.Stages;
using Microsoft.Extensions.Logging;

namespace Aovias.Bootstrap.Core;

/// <summary>
/// Sprint M7.x / M7.x.2 — the Enterprise Bootstrap Orchestrator. Drives every
/// <see cref="BootstrapStage"/> through a fixed lifecycle state machine:
///
/// <code>
/// per stage, in fixed BootstrapPhaseName order (Connect and ValidatePrivileges run first, always,
/// never checkpoint-skipped — reachability and privilege mode are session properties):
///     Discover ──satisfied──▶ AlreadySatisfied (remote state is authoritative; the local
///     │                       checkpoint is only a hint some stages consult — see
///     │                       BootstrapStageContext.Checkpointed)
///     └─not satisfied─▶ Validate ──blocked──▶ Failed (with diagnostics)
///                       └─ Execute ──failed──▶ Repair ──repaired──▶ re-Execute (once)
///                          └─ Verify ──failed──▶ Repair ──repaired──▶ re-Verify (once)
///                             └─ Completed (checkpoint recorded)
/// </code>
///
/// A stage failure stops the run — later stages all depend on every prior stage having genuinely
/// completed. Every failure carries the stage's own root-cause detail, the automatic repair
/// attempts that were made, and the stage's collected diagnostics — never a bare error.
///
/// Sprint M7.x.2 hardening: every one of a stage's six lifecycle calls (Discover, Validate,
/// Execute, Verify, Repair, CollectDiagnostics) is individually wrapped, immediately at the call
/// site, so an exception thrown from ANY of them is caught before it can pass through another
/// call frame and lose its origin. This is what guarantees the reported failure always matches the
/// actual failed stage — the "Failed at Connect when the real exception was in
/// DockerNetworkCreation" defect traced to the *opposite* of this: connectivity and privilege
/// detection used to run as unguarded code before the stage loop even started, so an exception
/// there had no stage to attach to at all. They are now real stages (<see cref="Stages.ConnectStage"/>,
/// <see cref="Stages.ValidatePrivilegesStage"/>) subject to the exact same guarantee as every other
/// phase. The full session command transcript is persisted alongside the report.
/// </summary>
public sealed class BootstrapOrchestrator(
    IBootstrapTargetExecutor executor,
    IBootstrapRegistrationClient registrationClient,
    IBootstrapStateStore stateStore,
    ILogger<BootstrapOrchestrator> logger,
    int healthVerificationMaxAttempts = 10,
    int healthVerificationPollDelayMs = 3000,
    IBootstrapElevationHandler? elevationHandler = null,
    int registrationMaxAttempts = 5,
    int registrationRetryDelayMs = 2000,
    int containerHealthMaxAttempts = 10,
    int containerHealthPollDelayMs = 3000,
    int connectRetryDelayMs = 2000,
    int transientCommandRetryAttempts = 2,
    int transientCommandRetryDelayMs = 1000) : IBootstrapOrchestrator
{
    public async Task<BootstrapReport> BootstrapAsync(BootstrapRequest request, IBootstrapExecutionObserver? observer = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var target = request.Target;

        // Per-run composition: the engine (and therefore the transcript and cached sudo password)
        // lives exactly as long as one bootstrap session.
        var engine = new RemoteExecutionEngine(executor, elevationHandler, logger, transientCommandRetryAttempts, transientCommandRetryDelayMs);
        var context = new BootstrapStageContext
        {
            Target = target,
            Engine = engine,
            Executor = executor,
            RegistrationClient = registrationClient,
            Permissions = new PermissionRepairService(engine),
            Docker = new DockerManager(engine),
            Configuration = new ConfigurationSynchronizer(executor, engine),
            Observer = observer,
            Tunables = new BootstrapTunables(
                healthVerificationMaxAttempts,
                healthVerificationPollDelayMs,
                registrationMaxAttempts,
                registrationRetryDelayMs,
                containerHealthMaxAttempts,
                containerHealthPollDelayMs,
                connectRetryDelayMs)
        };

        try
        {
            // Pre-session cancellation point: an already-cancelled request must never issue a
            // single command to the target — not even the Connect stage's own probe.
            cancellationToken.ThrowIfCancellationRequested();

            if (request.Force)
            {
                await stateStore.ResetAsync(target.StableIdentity, cancellationToken);
            }

            var completed = await stateStore.GetCompletedPhasesAsync(target.StableIdentity, cancellationToken);
            var results = new List<BootstrapPhaseResult>();

            foreach (var stage in BuildPipeline())
            {
                // Between-stage cancellation point — a stage that has already started still runs to
                // its own next between-command boundary; this only stops a *new* stage from starting.
                cancellationToken.ThrowIfCancellationRequested();

                observer?.OnPhaseStarting(stage.Phase);
                context.Checkpointed = completed.Contains(stage.Phase);

                var result = await RunStageAsync(stage, context, cancellationToken);

                if (result.Succeeded && !context.Checkpointed)
                {
                    await stateStore.MarkCompletedAsync(target.StableIdentity, stage.Phase, cancellationToken);
                }

                observer?.OnPhaseFinished(result);
                results.Add(result);

                if (!result.Succeeded)
                {
                    break;
                }
            }

            return WriteReport(new BootstrapReport { TargetIdentity = target.StableIdentity, Phases = results }, request.WorkingDirectory, engine);
        }
        finally
        {
            engine.ClearSensitiveState();
        }
    }

    /// <summary>The fixed pipeline, in <see cref="BootstrapPhaseName"/> order — platform policy, never reordered or filtered per caller. Connect and ValidatePrivileges lead every run.</summary>
    private static IReadOnlyList<BootstrapStage> BuildPipeline() =>
    [
        new ConnectStage(),
        new ValidatePrivilegesStage(),
        new PreflightStage(),
        new DockerInstallationStage(),
        new DockerNetworkStage(),
        new RuntimeDirectoryStage(),
        new ConfigurationGenerationStage(),
        new ConfigurationValidationStage(),
        new RuntimeStartupStage(),
        new RegistrationStage(),
        new VerificationStage()
    ];

    /// <summary>
    /// One stage through the full lifecycle — see the class remarks for the state diagram. Every
    /// lifecycle call below is individually guarded (try/catch immediately around that one call),
    /// which is the actual mechanism guaranteeing correct failure attribution: whatever throws,
    /// wherever it throws, is caught before it can travel through another frame and lose its origin.
    /// </summary>
    private async Task<BootstrapPhaseResult> RunStageAsync(BootstrapStage stage, BootstrapStageContext context, CancellationToken cancellationToken)
    {
        var startedUtc = DateTimeOffset.UtcNow;

        StageDiscovery discovery;
        try
        {
            discovery = await stage.DiscoverAsync(context, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Genuine cancellation (the Cancel button, via this exact token) always propagates —
            // matching OperationEngine's own Cancelled-status handling. A bare OperationCanceledException
            // NOT tied to this token (e.g. RemoteExecutionEngine using it to signal "the operator
            // declined the sudo password prompt") is a normal, reportable stage failure and falls
            // through to the general catch below instead.
            throw;
        }
        catch (Exception exception)
        {
            return await FailWithDiagnosticsAsync(stage, context, ThrownDuring(stage, BootstrapStageLifecycleStep.Discover, exception), repairs: [], startedUtc, retryCount: 0, cancellationToken);
        }

        if (discovery.Satisfied)
        {
            return PhaseResult(stage.Phase, BootstrapPhaseOutcome.AlreadySatisfied, discovery.Evidence, rootCause: null, startedUtc, retryCount: 0, repairs: [], verification: discovery.Evidence);
        }

        StageValidation validation;
        try
        {
            validation = await stage.ValidateAsync(context, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // See the Discover step's catch above for why this guard exists.
            throw;
        }
        catch (Exception exception)
        {
            return await FailWithDiagnosticsAsync(stage, context, ThrownDuring(stage, BootstrapStageLifecycleStep.Validate, exception), repairs: [], startedUtc, retryCount: 0, cancellationToken);
        }

        if (!validation.Valid)
        {
            return await FailWithDiagnosticsAsync(stage, context, ($"Phase '{stage.Phase}' preconditions are not met.", validation.Blockers), repairs: [], startedUtc, retryCount: 0, cancellationToken);
        }

        var repairs = new List<string>();
        var retryCount = 0;

        var execution = await ExecuteGuardedAsync(stage, context, cancellationToken);
        if (!execution.Succeeded)
        {
            var repair = await RepairGuardedAsync(stage, context, execution.Message, executionFailed: true, cancellationToken);
            RecordRepair(context, stage, repairs, repair);
            if (repair is { Attempted: true, Succeeded: true })
            {
                retryCount++;
                execution = await ExecuteGuardedAsync(stage, context, cancellationToken);
            }
        }

        if (!execution.Succeeded)
        {
            return await FailWithDiagnosticsAsync(stage, context, (execution.Message, execution.Detail), repairs, startedUtc, retryCount, cancellationToken);
        }

        var verification = await VerifyGuardedAsync(stage, context, cancellationToken);
        if (!verification.Succeeded)
        {
            var repair = await RepairGuardedAsync(stage, context, verification.Message, executionFailed: false, cancellationToken);
            RecordRepair(context, stage, repairs, repair);
            if (repair is { Attempted: true, Succeeded: true })
            {
                retryCount++;
                verification = await VerifyGuardedAsync(stage, context, cancellationToken);
            }
        }

        if (!verification.Succeeded)
        {
            return await FailWithDiagnosticsAsync(stage, context, (verification.Message, verification.Detail), repairs, startedUtc, retryCount, cancellationToken);
        }

        return PhaseResult(stage.Phase, BootstrapPhaseOutcome.Completed, execution.Message, rootCause: null, startedUtc, retryCount, repairs, verification.Message);
    }

    private async Task<StageOutcome> ExecuteGuardedAsync(BootstrapStage stage, BootstrapStageContext context, CancellationToken cancellationToken)
    {
        try
        {
            return await stage.ExecuteAsync(context, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // See the Discover step's catch above for why this guard exists.
            throw;
        }
        catch (Exception exception)
        {
            var (message, detail) = ThrownDuring(stage, BootstrapStageLifecycleStep.Execute, exception);
            return StageOutcome.Fail(message, detail);
        }
    }

    private async Task<StageOutcome> VerifyGuardedAsync(BootstrapStage stage, BootstrapStageContext context, CancellationToken cancellationToken)
    {
        try
        {
            return await stage.VerifyAsync(context, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // See the Discover step's catch above for why this guard exists.
            throw;
        }
        catch (Exception exception)
        {
            var (message, detail) = ThrownDuring(stage, BootstrapStageLifecycleStep.Verify, exception);
            return StageOutcome.Fail(message, detail);
        }
    }

    private async Task<StageRepair> RepairGuardedAsync(BootstrapStage stage, BootstrapStageContext context, string failureReason, bool executionFailed, CancellationToken cancellationToken)
    {
        try
        {
            return await stage.TryRepairAsync(context, failureReason, executionFailed, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // See the Discover step's catch above for why this guard exists.
            throw;
        }
        catch (Exception exception)
        {
            var stageException = new BootstrapStageException(stage.Phase, BootstrapStageLifecycleStep.Repair, exception);
            logger.LogError(stageException, "Bootstrap stage {Phase} threw during Repair.", stage.Phase);
            return StageRepair.Failed($"The repair attempt itself threw: {exception.Message}");
        }
    }

    /// <summary>Builds a <see cref="BootstrapStageException"/> immediately at the catch site, logs it with full stage/step context, and returns the (message, detail) pair a <see cref="StageOutcome"/>/failure result needs — the single place every "an exception was thrown" path funnels through, so the message shape is consistent everywhere.</summary>
    private (string Message, string? Detail) ThrownDuring(BootstrapStage stage, BootstrapStageLifecycleStep step, Exception exception)
    {
        var stageException = new BootstrapStageException(stage.Phase, step, exception);
        logger.LogError(stageException, "Bootstrap stage {Phase} threw during {Step}.", stage.Phase, step);
        return ($"Phase '{stage.Phase}' threw during {step}.", stageException.InnerException?.ToString() ?? stageException.Message);
    }

    private static void RecordRepair(BootstrapStageContext context, BootstrapStage stage, List<string> repairs, StageRepair repair)
    {
        if (!repair.Attempted)
        {
            return;
        }

        var entry = $"{(repair.Succeeded ? "Repaired" : "Repair failed")}: {repair.Description}";
        repairs.Add(entry);
        // Sprint M7.x.2 Stage Timeline: repair activity also flows into the stage's own live log
        // feed (via IBootstrapExecutionObserver.OnLog, which the CSM adapter attributes to whatever
        // stage most recently called OnPhaseStarting — always this one, since we're mid-lifecycle
        // for it) so an operator watching the run sees repair attempts as they happen, not only
        // after the fact inside a failure card's RootCause.
        context.Observer?.OnLog($"[{stage.Phase}] {entry}");
    }

    /// <summary>
    /// Diagnostics framework: every failure's root cause carries the stage's own detail, the
    /// automatic repair attempts that were made (and how each went), and the stage's collected
    /// evidence — never a bare error. Diagnostics collection is itself guarded (Sprint M7.x.2: the
    /// CollectDiagnostics lifecycle step) so it can never mask or replace the real failure it is
    /// attached to.
    /// </summary>
    private static async Task<BootstrapPhaseResult> FailWithDiagnosticsAsync(
        BootstrapStage stage,
        BootstrapStageContext context,
        (string Message, string? Detail) failure,
        IReadOnlyList<string> repairs,
        DateTimeOffset startedUtc,
        int retryCount,
        CancellationToken cancellationToken)
    {
        var sections = new List<string>();
        if (!string.IsNullOrWhiteSpace(failure.Detail))
        {
            sections.Add(failure.Detail);
        }

        if (repairs.Count > 0)
        {
            sections.Add($"=== Automatic Repair Attempts ==={Environment.NewLine}{string.Join(Environment.NewLine, repairs)}");
        }

        try
        {
            var diagnostics = await stage.CollectDiagnosticsAsync(context, cancellationToken);
            if (!string.IsNullOrWhiteSpace(diagnostics))
            {
                sections.Add(diagnostics);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // See RunStageAsync's Discover-step catch for why this guard exists.
            throw;
        }
        catch (Exception exception)
        {
            var stageException = new BootstrapStageException(stage.Phase, BootstrapStageLifecycleStep.CollectDiagnostics, exception);
            sections.Add($"(diagnostics collection itself threw: {stageException.InnerException?.Message ?? stageException.Message})");
        }

        return PhaseResult(
            stage.Phase,
            BootstrapPhaseOutcome.Failed,
            failure.Message,
            sections.Count > 0 ? string.Join($"{Environment.NewLine}{Environment.NewLine}", sections) : null,
            startedUtc,
            retryCount,
            repairs,
            verification: null);
    }

    private static BootstrapPhaseResult PhaseResult(
        BootstrapPhaseName phase,
        BootstrapPhaseOutcome outcome,
        string message,
        string? rootCause,
        DateTimeOffset startedUtc,
        int retryCount,
        IReadOnlyList<string> repairs,
        string? verification)
        => new()
        {
            Phase = phase,
            Outcome = outcome,
            Message = message,
            RootCause = rootCause,
            StartedUtc = startedUtc,
            RetryCount = retryCount,
            RepairAttempts = repairs,
            VerificationResult = verification
        };

    /// <summary>Persists the report and the session command transcript side by side — the exportable evidence trail for the whole run.</summary>
    private static BootstrapReport WriteReport(BootstrapReport report, string workingDirectory, RemoteExecutionEngine engine)
    {
        var reportsDirectory = Path.Combine(workingDirectory, "bootstrap-reports");
        Directory.CreateDirectory(reportsDirectory);
        var sanitizedIdentity = string.Concat(report.TargetIdentity.Select(c => char.IsLetterOrDigit(c) ? c : '-'));
        var baseName = $"{sanitizedIdentity}-{report.GeneratedUtc:yyyyMMddHHmmss}";

        var reportPath = Path.Combine(reportsDirectory, $"{baseName}.json");
        File.WriteAllText(reportPath, JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));

        var transcript = new StringBuilder();
        foreach (var record in engine.Transcript)
        {
            transcript.Append('[').Append(record.StartedUtc.ToString("O")).Append("] ")
                .Append(record.Elevated ? "[elevated] " : string.Empty)
                .Append("[exit=").Append(record.ExitCode).Append("] ")
                .Append('[').Append(record.DurationMs).Append(" ms] ")
                .AppendLine(record.Command);
            if (record.ExitCode != 0 && !string.IsNullOrWhiteSpace(record.StandardErrorFirstLine))
            {
                transcript.Append("    stderr: ").AppendLine(record.StandardErrorFirstLine);
            }
        }

        File.WriteAllText(Path.Combine(reportsDirectory, $"{baseName}-transcript.log"), transcript.ToString());

        return report with { ReportPath = reportPath };
    }
}
