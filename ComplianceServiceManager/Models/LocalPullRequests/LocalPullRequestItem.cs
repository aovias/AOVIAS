using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using ComplianceServiceManager.Models;

namespace ComplianceServiceManager.Models.LocalPullRequests;

/// <summary>UI-facing local PR with live Git comparison.</summary>
public sealed partial class LocalPullRequestItem : ObservableObject
{
    [ObservableProperty]
    private string id = string.Empty;

    [ObservableProperty]
    private string title = string.Empty;

    [ObservableProperty]
    private string description = string.Empty;

    [ObservableProperty]
    private string sourceBranch = string.Empty;

    [ObservableProperty]
    private string targetBranch = "main";

    [ObservableProperty]
    private string sourceWorktreePath = string.Empty;

    [ObservableProperty]
    private string targetWorktreePath = string.Empty;

    [ObservableProperty]
    private string authorAgent = string.Empty;

    [ObservableProperty]
    private LocalPullRequestStatus status = LocalPullRequestStatus.Draft;

    [ObservableProperty]
    private string statusDisplay = "DRAFT";

    [ObservableProperty]
    private string? blockReason;

    [ObservableProperty]
    private string summaryDisplay = string.Empty;

    [ObservableProperty]
    private string gitStateDisplay = string.Empty;

    [ObservableProperty]
    private string sourceShaDisplay = "—";

    [ObservableProperty]
    private string targetShaDisplay = "—";

    [ObservableProperty]
    private string validationDisplay = "UNKNOWN";

    [ObservableProperty]
    private string? mergeCommitSha;

    [ObservableProperty]
    private string? selectedFilePath;

    [ObservableProperty]
    private string selectedDiffText = string.Empty;

    public LocalPullRequestValidation Validation { get; set; } = new();

    public LocalPullRequestReviewRecord? LastReview { get; set; }

    public LocalPullRequestComparison? Comparison { get; set; }

    public ObservableCollection<LocalPullRequestCommitInfo> Commits { get; } = [];

    public ObservableCollection<LocalPullRequestFileChange> ChangedFiles { get; } = [];

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public void ApplyStatus(LocalPullRequestStatus next, string? block = null)
    {
        Status = next;
        BlockReason = block;
        StatusDisplay = next switch
        {
            LocalPullRequestStatus.Draft => "DRAFT",
            LocalPullRequestStatus.ReadyForReview => "READY FOR REVIEW",
            LocalPullRequestStatus.InReview => "IN REVIEW",
            LocalPullRequestStatus.ChangesRequested => "CHANGES REQUESTED",
            LocalPullRequestStatus.Approved => "APPROVED",
            LocalPullRequestStatus.Merging => "MERGING",
            LocalPullRequestStatus.Merged => "MERGED",
            LocalPullRequestStatus.Closed => "CLOSED",
            LocalPullRequestStatus.Blocked => "BLOCKED",
            LocalPullRequestStatus.Conflicted => "CONFLICTED",
            LocalPullRequestStatus.Failed => "FAILED",
            _ => next.ToString().ToUpperInvariant()
        };
    }

    public void ApplyComparison(LocalPullRequestComparison comparison)
    {
        Comparison = comparison;
        Commits.Clear();
        foreach (var commit in comparison.Commits)
        {
            Commits.Add(commit);
        }

        ChangedFiles.Clear();
        foreach (var file in comparison.ChangedFiles)
        {
            ChangedFiles.Add(file);
        }

        SummaryDisplay = $"{comparison.Commits.Count} commit(s) · {comparison.ChangedFiles.Count} file(s) · +{comparison.LinesAdded}/-{comparison.LinesRemoved}";
        SourceShaDisplay = OperatorLabels.ShortSha(comparison.SourceTipSha);
        TargetShaDisplay = OperatorLabels.ShortSha(comparison.TargetTipSha);
        GitStateDisplay =
            $"Source {(comparison.SourceClean ? "CLEAN" : "DIRTY")} · Target {(comparison.TargetClean ? "CLEAN" : "DIRTY")} · " +
            $"ahead {comparison.Ahead} / behind {comparison.Behind}" +
            (comparison.HasConflicts ? " · CONFLICTS" : string.Empty) +
            (comparison.ApprovalIsStale ? " · APPROVAL STALE" : string.Empty);

        ValidationDisplay = Validation.Overall switch
        {
            LocalPullRequestValidationResult.Passed => "PASSED",
            LocalPullRequestValidationResult.Failed => "FAILED",
            _ => "UNKNOWN"
        };
    }

    public LocalPullRequestDefinition ToDefinition()
        => new()
        {
            Id = Id,
            Title = Title,
            Description = Description,
            SourceWorktreePath = SourceWorktreePath,
            TargetWorktreePath = TargetWorktreePath,
            SourceBranch = SourceBranch,
            TargetBranch = TargetBranch,
            AuthorAgent = AuthorAgent,
            CreatedAt = CreatedAt,
            UpdatedAt = UpdatedAt,
            Status = Status,
            BlockReason = BlockReason,
            Validation = Validation,
            LastReview = LastReview,
            MergeCommitSha = MergeCommitSha,
            MergedAt = Status == LocalPullRequestStatus.Merged ? UpdatedAt : null
        };

    public static LocalPullRequestItem FromDefinition(LocalPullRequestDefinition definition)
    {
        var item = new LocalPullRequestItem
        {
            Id = definition.Id,
            Title = definition.Title,
            Description = definition.Description,
            SourceWorktreePath = definition.SourceWorktreePath,
            TargetWorktreePath = definition.TargetWorktreePath,
            SourceBranch = definition.SourceBranch,
            TargetBranch = definition.TargetBranch,
            AuthorAgent = definition.AuthorAgent,
            CreatedAt = definition.CreatedAt,
            UpdatedAt = definition.UpdatedAt,
            Validation = definition.Validation,
            LastReview = definition.LastReview,
            MergeCommitSha = definition.MergeCommitSha
        };
        item.ApplyStatus(definition.Status, definition.BlockReason);
        return item;
    }
}
