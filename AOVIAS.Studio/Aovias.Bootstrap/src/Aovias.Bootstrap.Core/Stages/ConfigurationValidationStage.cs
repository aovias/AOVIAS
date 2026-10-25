using Aovias.Bootstrap.Contracts;

namespace Aovias.Bootstrap.Core.Stages;

/// <summary>
/// Pure, local contract validation — no SSH round-trip needed, since the exact content
/// <see cref="ConfigurationGenerationStage"/> synchronizes is regenerated deterministically from
/// the same target and validated in-process. See <c>RuntimeConfigurationValidator</c>'s remarks for
/// why this checks each <c>Aovias.Configuration.Settings</c> class's own declared requirements
/// rather than a hand-duplicated list. Never starts Docker, never touches the target host — a
/// failure here stops Bootstrap before Runtime Startup does anything at all. Re-runs on every
/// attempt by design: it is cheap, and "never skip validation" is the pipeline's rule.
/// </summary>
public sealed class ConfigurationValidationStage : BootstrapStage
{
    public override BootstrapPhaseName Phase => BootstrapPhaseName.ValidateRuntimeConfiguration;

    public override Task<StageOutcome> ExecuteAsync(BootstrapStageContext context, CancellationToken cancellationToken)
    {
        var settingsByFile = RuntimeConfigurationScaffoldBuilder.BuildRequiredProfileSettings(context.Target);
        var failures = RuntimeConfigurationValidator.Validate(settingsByFile);

        if (failures.Count > 0)
        {
            return Task.FromResult(StageOutcome.Fail(
                $"Configuration Validation Failed — {failures.Count} required setting(s) missing or invalid across {failures.Select(f => f.File).Distinct().Count()} file(s).",
                RuntimeConfigurationValidator.FormatFailures(failures)));
        }

        return Task.FromResult(StageOutcome.Ok($"Validated {settingsByFile.Count} required configuration files — all required properties present."));
    }

    /// <summary>In-process validation is its own verification.</summary>
    public override Task<StageOutcome> VerifyAsync(BootstrapStageContext context, CancellationToken cancellationToken)
        => Task.FromResult(StageOutcome.Ok("Configuration contract validation is its own verification."));
}
