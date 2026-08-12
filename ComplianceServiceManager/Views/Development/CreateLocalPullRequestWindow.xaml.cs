using System.IO;
using System.Windows;
using ComplianceServiceManager.Services.LocalPullRequests;

namespace ComplianceServiceManager.Views.Development;

public partial class CreateLocalPullRequestWindow : Window
{
    private readonly ILocalPullRequestService pullRequestService;

    public CreateLocalPullRequestWindow(ILocalPullRequestService pullRequestService)
    {
        InitializeComponent();
        this.pullRequestService = pullRequestService;
        var candidates = pullRequestService.GetCandidateSourceBranches();
        SourceBranchBox.ItemsSource = candidates.Select(item => $"{item.Branch}  ({item.Agent})").ToList();
        SourceBranchBox.Tag = candidates;
        if (candidates.Count > 0)
        {
            SourceBranchBox.SelectedIndex = 0;
        }

        TargetBranchBox.Text = "main";
    }

    public LocalPullRequestCreateResult? Result { get; private set; }

    private void OnCreateClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TitleBox.Text))
        {
            MessageBox.Show(this, "Title is required.", "Local PR", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (SourceBranchBox.SelectedIndex < 0 || SourceBranchBox.Tag is not IReadOnlyList<(string Branch, string WorktreePath, string Agent)> candidates)
        {
            MessageBox.Show(this, "Select a source branch.", "Local PR", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var selected = candidates[SourceBranchBox.SelectedIndex];
        var targetBranch = string.IsNullOrWhiteSpace(TargetBranchBox.Text) ? "main" : TargetBranchBox.Text.Trim();
        if (string.Equals(selected.Branch, targetBranch, StringComparison.OrdinalIgnoreCase))
        {
            MessageBox.Show(this, "Source and target branches must differ.", "Local PR", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!TryResolveMainWorktree(selected.WorktreePath, out var targetWorktree, out var resolveError))
        {
            MessageBox.Show(this, resolveError, "Local PR", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        Result = new LocalPullRequestCreateResult
        {
            Title = TitleBox.Text.Trim(),
            Description = DescriptionBox.Text.Trim(),
            SourceBranch = selected.Branch,
            SourceWorktreePath = selected.WorktreePath,
            TargetBranch = targetBranch,
            TargetWorktreePath = targetWorktree,
            AuthorAgent = selected.Agent
        };
        DialogResult = true;
        Close();
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private static bool TryResolveMainWorktree(string sourceWorktreePath, out string targetWorktree, out string error)
    {
        targetWorktree = string.Empty;
        error = string.Empty;
        var parent = Path.GetDirectoryName(Path.GetFullPath(sourceWorktreePath));
        if (string.IsNullOrWhiteSpace(parent))
        {
            error = "Cannot resolve DevelopmentRepo parent from the source worktree.";
            return false;
        }

        var main = Path.Combine(parent, "AOVIAS");
        if (!Directory.Exists(main))
        {
            error = $"Main integration worktree not found at '{main}'. Local PR merge target must be AOVIAS.";
            return false;
        }

        targetWorktree = main;
        return true;
    }
}

public sealed class LocalPullRequestCreateResult
{
    public required string Title { get; init; }

    public required string Description { get; init; }

    public required string SourceBranch { get; init; }

    public required string SourceWorktreePath { get; init; }

    public required string TargetBranch { get; init; }

    public required string TargetWorktreePath { get; init; }

    public required string AuthorAgent { get; init; }
}
