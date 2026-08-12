using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ComplianceServiceManager.Models.RuntimeProfiles;

/// <summary>UI-facing runtime profile with derived validation and session status.</summary>
public sealed partial class RuntimeProfileItem : ObservableObject
{
    [ObservableProperty]
    private string id = string.Empty;

    [ObservableProperty]
    private string name = string.Empty;

    [ObservableProperty]
    private string description = string.Empty;

    [ObservableProperty]
    private bool enabled = true;

    [ObservableProperty]
    private int maxParallelStarts = 2;

    [ObservableProperty]
    private int serviceCount;

    [ObservableProperty]
    private RuntimeProfileStatus status = RuntimeProfileStatus.Stopped;

    [ObservableProperty]
    private string statusDisplay = "STOPPED";

    [ObservableProperty]
    private bool isValid = true;

    [ObservableProperty]
    private string validationDisplay = "Dependencies validated";

    [ObservableProperty]
    private string? progressMessage;

    public ObservableCollection<RuntimeProfileMemberItem> Members { get; } = [];

    public ObservableCollection<string> ValidationIssues { get; } = [];

    public List<RuntimeProfileServiceEntry> Services { get; set; } = [];

    /// <summary>Service names started by the current profile session (safe to stop).</summary>
    public HashSet<string> OwnedServiceNames { get; } = new(StringComparer.OrdinalIgnoreCase);

    public void ApplyStatus(RuntimeProfileStatus next, string? progress = null)
    {
        Status = next;
        StatusDisplay = next switch
        {
            RuntimeProfileStatus.Stopped => "STOPPED",
            RuntimeProfileStatus.Starting => "STARTING",
            RuntimeProfileStatus.Running => "RUNNING",
            RuntimeProfileStatus.Stopping => "STOPPING",
            RuntimeProfileStatus.Failed => "FAILED",
            RuntimeProfileStatus.PartiallyRunning => "PARTIALLY RUNNING",
            RuntimeProfileStatus.Invalid => "INVALID",
            _ => next.ToString().ToUpperInvariant()
        };
        ProgressMessage = progress;
    }

    public void ApplyValidation(ProfileValidationResult result)
    {
        IsValid = result.IsValid;
        ValidationIssues.Clear();
        foreach (var issue in result.Issues)
        {
            ValidationIssues.Add(issue.Message);
        }

        ValidationDisplay = result.IsValid
            ? "Dependencies validated"
            : $"{result.Issues.Count} validation issue(s)";

        if (!result.IsValid && Status is RuntimeProfileStatus.Stopped or RuntimeProfileStatus.Invalid)
        {
            ApplyStatus(RuntimeProfileStatus.Invalid);
        }
        else if (result.IsValid && Status == RuntimeProfileStatus.Invalid)
        {
            ApplyStatus(RuntimeProfileStatus.Stopped);
        }
    }

    public RuntimeProfileDefinition ToDefinition()
        => new()
        {
            Id = Id,
            Name = Name,
            Description = Description,
            Enabled = Enabled,
            MaxParallelStarts = MaxParallelStarts <= 0 ? 2 : MaxParallelStarts,
            Services = Services.Select(CloneEntry).ToList()
        };

    public static RuntimeProfileItem FromDefinition(RuntimeProfileDefinition definition)
    {
        var item = new RuntimeProfileItem
        {
            Id = definition.Id,
            Name = definition.Name,
            Description = definition.Description,
            Enabled = definition.Enabled,
            MaxParallelStarts = definition.MaxParallelStarts <= 0 ? 2 : definition.MaxParallelStarts,
            Services = definition.Services.Select(CloneEntry).ToList(),
            ServiceCount = definition.Services.Count(entry => entry.Enabled)
        };
        return item;
    }

    private static RuntimeProfileServiceEntry CloneEntry(RuntimeProfileServiceEntry entry)
        => new()
        {
            ServiceName = entry.ServiceName,
            Enabled = entry.Enabled,
            DependsOn = entry.DependsOn.ToList()
        };
}
