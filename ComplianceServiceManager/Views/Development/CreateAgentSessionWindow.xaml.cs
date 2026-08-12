using System.Windows;
using ComplianceServiceManager.Models.AgentSessions;
using ComplianceServiceManager.Services.AgentSessions;

namespace ComplianceServiceManager.Views.Development;

public partial class CreateAgentSessionWindow : Window
{
    private readonly IAgentSessionService sessionService;
    private readonly IReadOnlyList<(string Id, string DisplayName, string Path, DevelopmentAgentType? PreferredAgent)> workspaces;
    private readonly IReadOnlyList<(string Id, string Name)> profiles;
    private readonly IReadOnlyList<(string Id, string Title)> pullRequests;

    public CreateAgentSessionWindow(IAgentSessionService sessionService)
    {
        InitializeComponent();
        this.sessionService = sessionService;
        workspaces = sessionService.GetWorkspaceChoices();
        profiles = sessionService.GetRuntimeProfileChoices();
        pullRequests = sessionService.GetPullRequestChoices();

        AgentTypeBox.ItemsSource = Enum.GetValues<DevelopmentAgentType>();
        AgentTypeBox.SelectedItem = DevelopmentAgentType.Cursor;
        WorkspaceBox.ItemsSource = workspaces.Select(item => $"{item.DisplayName} ({item.Id})").ToList();
        if (workspaces.Count > 0)
        {
            WorkspaceBox.SelectedIndex = 0;
        }

        ProfileBox.ItemsSource = new[] { "(none)" }.Concat(profiles.Select(item => item.Name)).ToList();
        ProfileBox.SelectedIndex = 0;
        PullRequestBox.ItemsSource = new[] { "(none)" }.Concat(pullRequests.Select(item => item.Title)).ToList();
        PullRequestBox.SelectedIndex = 0;
        AgentTypeBox.SelectionChanged += (_, _) => SuggestWorkspaceForAgent();
        SuggestWorkspaceForAgent();
    }

    public AgentSessionCreateResult? Result { get; private set; }

    private void SuggestWorkspaceForAgent()
    {
        if (AgentTypeBox.SelectedItem is not DevelopmentAgentType agent)
        {
            return;
        }

        var index = workspaces.ToList().FindIndex(item => item.PreferredAgent == agent);
        if (index >= 0)
        {
            WorkspaceBox.SelectedIndex = index;
        }
    }

    private void OnCreateClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TitleBox.Text))
        {
            MessageBox.Show(this, "Title is required.", "Agent Session", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (WorkspaceBox.SelectedIndex < 0 || WorkspaceBox.SelectedIndex >= workspaces.Count)
        {
            MessageBox.Show(this, "Select a workspace.", "Agent Session", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(BranchBox.Text))
        {
            MessageBox.Show(this, "Expected branch is required.", "Agent Session", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var workspace = workspaces[WorkspaceBox.SelectedIndex];
        string? profileId = null;
        if (ProfileBox.SelectedIndex > 0)
        {
            profileId = profiles[ProfileBox.SelectedIndex - 1].Id;
        }

        string? prId = null;
        if (PullRequestBox.SelectedIndex > 0)
        {
            prId = pullRequests[PullRequestBox.SelectedIndex - 1].Id;
        }

        Result = new AgentSessionCreateResult
        {
            AgentType = (DevelopmentAgentType)AgentTypeBox.SelectedItem!,
            Title = TitleBox.Text.Trim(),
            Description = DescriptionBox.Text.Trim(),
            TaskId = string.IsNullOrWhiteSpace(TaskIdBox.Text) ? null : TaskIdBox.Text.Trim(),
            WorkspaceId = workspace.Id,
            ExpectedBranch = BranchBox.Text.Trim(),
            RuntimeProfileId = profileId,
            PullRequestId = prId
        };
        DialogResult = true;
        Close();
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}

public sealed class AgentSessionCreateResult
{
    public required DevelopmentAgentType AgentType { get; init; }

    public required string Title { get; init; }

    public required string Description { get; init; }

    public string? TaskId { get; init; }

    public required string WorkspaceId { get; init; }

    public required string ExpectedBranch { get; init; }

    public string? RuntimeProfileId { get; init; }

    public string? PullRequestId { get; init; }
}
