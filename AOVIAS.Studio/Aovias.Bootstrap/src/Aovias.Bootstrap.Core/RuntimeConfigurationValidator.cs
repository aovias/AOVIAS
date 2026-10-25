using System.ComponentModel.DataAnnotations;
using Aovias.Configuration.Settings;

namespace Aovias.Bootstrap.Core;

/// <summary>
/// One failure: a single required setting that is missing, empty, or otherwise invalid in one
/// generated configuration file. Deliberately flat and file-scoped (not exception-based) so the
/// Operation Engine's failure card can show every problem found, not just the first one — matching
/// the "before Runtime startup, not after a crash" point of this whole defect fix.
/// </summary>
public sealed record RuntimeConfigurationValidationFailure
{
    public required string File { get; init; }

    public required string Property { get; init; }

    public required string Expected { get; init; }

    public required string Actual { get; init; }

    public required string SuggestedFix { get; init; }
}

/// <summary>
/// Validates every file <c>RuntimeConfigurationScaffoldBuilder.BuildRequiredProfileSettings</c>
/// generates against the SAME <see cref="System.ComponentModel.DataAnnotations"/> requirements each
/// <c>Aovias.Configuration.Settings</c> class already declares — this is "each configuration binder
/// exposes its required settings" taken literally: the <c>[Required]</c>/<c>[Range]</c> attributes on
/// those classes ARE the contract, and this validator asks each class about itself
/// (<c>Validator.TryValidateObject</c>) rather than hand-duplicating a second list of "things
/// that must not be empty" that could silently drift from what the classes actually declare.
///
/// A small number of real Runtime Agent requirements are enforced by a custom method/lambda rather
/// than a declared attribute (see <c>Aovias.Configuration.Extensions.ModularConfigurationExtensions</c>
/// and <c>Aovias.Configuration.Settings.LoggingSettingsBinder</c>) — <see cref="LoggingSettings.LogRoot"/>
/// (the exact real-world defect this fix targets: <c>LoggingSettingsBinder.BindAndPrepare</c> requires
/// it but the class itself carries no <c>[Required]</c> attribute at all) and
/// <see cref="DeploymentStorageSettings.Root"/> are the ones that matter for Bootstrap's own generated
/// files (the others — Security's SigningKey/ApiKey, Network's Subnet/Gateway — are declared
/// <c>[Required]</c> but not actually enforced at Runtime Agent startup, so validating those two via
/// the generic pass below is a deliberately *stricter* proactive check, not a gap).
/// </summary>
public static class RuntimeConfigurationValidator
{
    public static IReadOnlyList<RuntimeConfigurationValidationFailure> Validate(IReadOnlyDictionary<string, object> settingsByFile)
    {
        var failures = new List<RuntimeConfigurationValidationFailure>();

        foreach (var (file, settings) in settingsByFile)
        {
            var context = new ValidationContext(settings);
            var results = new List<ValidationResult>();
            Validator.TryValidateObject(settings, context, results, validateAllProperties: true);

            foreach (var result in results)
            {
                foreach (var memberName in result.MemberNames.DefaultIfEmpty(string.Empty))
                {
                    var property = settings.GetType().GetProperty(memberName);
                    var actual = property?.GetValue(settings)?.ToString();
                    failures.Add(new RuntimeConfigurationValidationFailure
                    {
                        File = file,
                        Property = string.IsNullOrEmpty(memberName) ? "(unknown)" : memberName,
                        Expected = result.ErrorMessage ?? "a valid, non-empty value",
                        Actual = string.IsNullOrWhiteSpace(actual) ? "<empty>" : actual!,
                        SuggestedFix = $"Regenerate Runtime Configuration — '{memberName}' in '{file}' must be set."
                    });
                }
            }

            if (settings is DeploymentStorageSettings { Root: var root } && string.IsNullOrWhiteSpace(root))
            {
                failures.Add(new RuntimeConfigurationValidationFailure
                {
                    File = file,
                    Property = nameof(DeploymentStorageSettings.Root),
                    Expected = "a non-empty deployment storage root path",
                    Actual = "<empty>",
                    SuggestedFix = $"Regenerate Runtime Configuration — 'Root' in '{file}' must be set."
                });
            }

            // LoggingSettingsBinder.ResolveLogRoot prefers LogRoot, falling back to the legacy
            // LogDirectory alias — only when BOTH are empty does the Runtime Agent actually crash.
            if (settings is LoggingSettings { LogRoot: var logRoot, LogDirectory: var logDirectory }
                && string.IsNullOrWhiteSpace(logRoot) && string.IsNullOrWhiteSpace(logDirectory))
            {
                failures.Add(new RuntimeConfigurationValidationFailure
                {
                    File = file,
                    Property = nameof(LoggingSettings.LogRoot),
                    Expected = "a non-empty log root directory (logRoot, or the legacy logDirectory alias)",
                    Actual = "<empty>",
                    SuggestedFix = $"Regenerate Runtime Configuration — 'logRoot' in '{file}' must be set."
                });
            }
        }

        return failures;
    }

    public static string FormatFailures(IReadOnlyList<RuntimeConfigurationValidationFailure> failures)
        => string.Join(
            " | ",
            failures.Select(f => $"[{f.File}] {f.Property}: expected {f.Expected}, got {f.Actual}. {f.SuggestedFix}"));
}
