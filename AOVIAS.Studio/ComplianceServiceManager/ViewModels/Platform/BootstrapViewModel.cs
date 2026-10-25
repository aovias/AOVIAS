using System.Diagnostics;
using System.IO;
using System.Windows;
using Aovias.Bootstrap.Contracts;
using Aovias.Bootstrap.Execution;
using Aovias.Release.Contracts.RemoteDeployment;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ComplianceServiceManager.Contracts.Deployment;
using ComplianceServiceManager.Contracts.Publishing;
using ComplianceServiceManager.Models.Bootstrap;
using ComplianceServiceManager.Navigation;
using ComplianceServiceManager.Operations;
using ComplianceServiceManager.Services;
using ComplianceServiceManager.Services.Bootstrap;
using ComplianceServiceManager.ViewModels.Operations;

namespace ComplianceServiceManager.ViewModels.Platform;

/// <summary>
/// Platform → Bootstrap workspace: a 4-step autonomous provisioning wizard (Target Host →
/// Environment → Review → Execute) that turns a clean Ubuntu VPS into a registered AOVIAS Runtime
/// with one click, followed by an Environment Summary on success. The operator provides intent
/// (host, environment name/type); this workspace determines and executes the implementation
/// (Docker network name, config paths, latest Runtime Agent package) itself.
///
/// Execution itself runs through the generic <see cref="OperationEngine"/>
/// (<see cref="BootstrapOperationExecutor"/> drives <see cref="BootstrapRunner"/> — the exact same
/// composition root the Bootstrap CLI uses — in-process). There is no separate provisioning logic
/// here: this workspace only builds a <see cref="BootstrapTarget"/> from operator intent and renders
/// whatever the engine reports. See <see cref="TargetHostProbeService"/> for which parts of
/// "auto-discovery" are backed by genuine signals versus honest approximations — nothing here
/// fabricates data the backend doesn't actually provide.
/// </summary>
public sealed partial class BootstrapViewModel : ObservableObject
{
    private const int RuntimeAgentDefaultPort = 7080;
    private const string RuntimeAgentServiceId = "runtime-agent";

    private readonly TargetHostProbeService hostProbeService;
    private readonly OperationEngine operationEngine;
    private readonly IPublishingWorkspaceService publishingWorkspaceService;
    private readonly IEnvironmentManager environmentManager;
    private readonly IWorkspaceNavigator workspaceNavigator;

    public BootstrapViewModel(
        TargetHostProbeService hostProbeService,
        OperationEngine operationEngine,
        IPublishingWorkspaceService publishingWorkspaceService,
        IEnvironmentManager environmentManager,
        IWorkspaceNavigator workspaceNavigator)
    {
        this.hostProbeService = hostProbeService;
        this.operationEngine = operationEngine;
        this.publishingWorkspaceService = publishingWorkspaceService;
        this.environmentManager = environmentManager;
        this.workspaceNavigator = workspaceNavigator;

        Operation = new OperationViewModel(operationEngine);
        Operation.Completed += OnOperationCompleted;
        SudoElevationPrompt = new SudoElevationPromptViewModel();
    }

    // -----------------------------------------------------------------
    // Wizard shell
    // -----------------------------------------------------------------

    [ObservableProperty]
    private int wizardStep;

    [ObservableProperty]
    private bool isComplete;

    public IReadOnlyList<string> WizardStepNames { get; } = ["Target Host", "Environment", "Review", "Execute"];

    // -----------------------------------------------------------------
    // Step 0: Target Host
    // -----------------------------------------------------------------

    [ObservableProperty]
    private string host = string.Empty;

    [ObservableProperty]
    private string sshPort = "22";

    [ObservableProperty]
    private string username = "root";

    [ObservableProperty]
    private bool useKeyAuth = true;

    [ObservableProperty]
    private string privateKeyPath = string.Empty;

    /// <summary>Set from code-behind's PasswordBox.PasswordChanged — never bound directly in XAML.</summary>
    [ObservableProperty]
    private string password = string.Empty;

    [ObservableProperty]
    private bool isTestingConnection;

    [ObservableProperty]
    private bool isConnected;

    [ObservableProperty]
    private string connectionStatus = "Enter target host details, then Test Connection.";

    [ObservableProperty]
    private TargetHostFacts? facts;

    public bool HasExistingRuntime => Facts?.ExistingRuntimeDetected == true;

    /// <summary>Mirrors, for display only, what <c>BootstrapOrchestrator.DetectPrivilegeModeAsync</c> will authoritatively determine again during Preflight — "Detect Automatically" per the spec, never a control the operator configures (there is nothing to edit; sudoers is never touched here or anywhere in Bootstrap).</summary>
    public string PrivilegeStatusText => Facts?.PrivilegeMode switch
    {
        BootstrapPrivilegeMode.NotRequired => "Connected as root — no elevation needed.",
        BootstrapPrivilegeMode.Passwordless => "Passwordless sudo available.",
        BootstrapPrivilegeMode.PasswordRequired => "Sudo password required — Bootstrap will ask for it when needed.",
        BootstrapPrivilegeMode.Unavailable => "No path to root on this host — Bootstrap cannot proceed.",
        _ => string.Empty
    };

    public bool PrivilegeRequiresAttention => Facts?.PrivilegeMode is BootstrapPrivilegeMode.PasswordRequired or BootstrapPrivilegeMode.Unavailable;

    partial void OnFactsChanged(TargetHostFacts? value)
    {
        OnPropertyChanged(nameof(HasExistingRuntime));
        OnPropertyChanged(nameof(PrivilegeStatusText));
        OnPropertyChanged(nameof(PrivilegeRequiresAttention));
    }

    [ObservableProperty]
    private bool forceReapply;

    public bool CanContinueFromTargetHost => IsConnected && !IsTestingConnection;

    partial void OnHostChanged(string value)
    {
        OnPropertyChanged(nameof(AutoPublicUrl));
        OnPropertyChanged(nameof(ComposedCommand));
    }

    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        if (IsTestingConnection || string.IsNullOrWhiteSpace(Host) || string.IsNullOrWhiteSpace(Username))
        {
            ConnectionStatus = "Host and username are required.";
            return;
        }

        IsTestingConnection = true;
        IsConnected = false;
        Facts = null;
        ConnectionStatus = $"Connecting to {Host}…";

        try
        {
            var port = ParsePort();
            var (success, error) = await hostProbeService.TestConnectionAsync(
                Host, port, Username,
                UseKeyAuth ? PrivateKeyPath : null,
                UseKeyAuth ? null : Password,
                CancellationToken.None);

            if (!success)
            {
                ConnectionStatus = $"Connection failed: {error}";
                return;
            }

            IsConnected = true;
            ConnectionStatus = "Connected. Discovering host details…";

            Facts = await hostProbeService.DiscoverFactsAsync(
                Host, port, Username,
                UseKeyAuth ? PrivateKeyPath : null,
                UseKeyAuth ? null : Password,
                CancellationToken.None);

            ConnectionStatus = $"Connected to {Facts.Hostname} ({Facts.UbuntuVersion}).";
        }
        catch (Exception exception)
        {
            IsConnected = false;
            ConnectionStatus = $"Connection failed: {exception.Message}";
        }
        finally
        {
            IsTestingConnection = false;
            OnPropertyChanged(nameof(CanContinueFromTargetHost));
            ContinueToEnvironmentCommand.NotifyCanExecuteChanged();
        }
    }

    [RelayCommand]
    private void SelectExistingRuntimeAction(string action)
    {
        switch (action)
        {
            case "Repair":
            case "Upgrade":
                // The Bootstrap engine's only real repair mechanism is a forced re-run: every phase
                // is re-verified and re-applied rather than skipped because state says it already
                // succeeded (BootstrapOrchestrator honors --force this way). There is no separate
                // drift-repair capability for a bootstrap target — see BootstrapOperationExecutor's
                // remarks for why.
                ForceReapply = true;
                ConnectionStatus = $"{action} selected: the wizard will re-apply every provisioning step on this host.";
                break;
            case "Reconcile":
                // Drift reconciliation only exists for environments already registered with CSM
                // (IEnvironmentManager.DetectDriftAsync) — a bare bootstrap target isn't one yet.
                workspaceNavigator.NavigateTo(ShellViewModel.WorkspaceReconciliation);
                break;
        }
    }

    [RelayCommand(CanExecute = nameof(CanContinueFromTargetHost))]
    private void ContinueToEnvironment() => WizardStep = 1;

    // -----------------------------------------------------------------
    // Step 1: Environment
    // -----------------------------------------------------------------

    [ObservableProperty]
    private string environmentName = string.Empty;

    [ObservableProperty]
    private string environmentType = "Demo";

    [ObservableProperty]
    private string runtimeProfile = "demo";

    // Pre-filled from the platform's own recognized-token list (WellKnownRegistrationTokens — the
    // same source EnvironmentRegistrationStore seeds from) so the operator is never asked to guess
    // a value the Runtime Agent will only accept verbatim. Real-world defect: this was a free-text
    // field with no hint, and a mistyped token surfaced only at Stage 8 as an HTTP 500 on the VPS.
    [ObservableProperty]
    private string registrationToken = WellKnownRegistrationTokens.DemoVps;

    [ObservableProperty]
    private string commandCenterUrl = string.Empty;

    public IReadOnlyList<string> EnvironmentTypes { get; } = ["LocalDocker", "Demo", "Qa", "Uat", "Production", "Customer"];

    // Both are genuinely required, not optional — BootstrapOrchestrator.RunRuntimeAgentRegistrationAsync
    // always registers with the Fleet Registry at CommandCenterBaseUrl, and Preflight now validates
    // both up front (Stage 3) so a missing value blocks here, at Step 1, instead of surfacing as an
    // opaque Preflight or Stage-7 failure four steps later.
    public bool CanContinueFromEnvironment =>
        !string.IsNullOrWhiteSpace(EnvironmentName)
        && !string.IsNullOrWhiteSpace(RegistrationToken)
        && !string.IsNullOrWhiteSpace(CommandCenterUrl);

    partial void OnEnvironmentNameChanged(string value)
    {
        OnPropertyChanged(nameof(CanContinueFromEnvironment));
        ContinueToReviewCommand.NotifyCanExecuteChanged();
    }

    partial void OnRegistrationTokenChanged(string value)
    {
        OnPropertyChanged(nameof(CanContinueFromEnvironment));
        ContinueToReviewCommand.NotifyCanExecuteChanged();
    }

    partial void OnEnvironmentTypeChanged(string value)
    {
        // Keep the token in step with the selected environment type — but never clobber a value
        // the operator typed by hand: only an empty field or another well-known token (i.e. a
        // previous pre-fill) is replaced.
        if (string.IsNullOrWhiteSpace(RegistrationToken) || WellKnownRegistrationTokens.IsWellKnown(RegistrationToken))
        {
            var parsedType = Enum.TryParse<EnvironmentType>(value, out var parsed)
                ? parsed
                : Aovias.Release.Contracts.RemoteDeployment.EnvironmentType.Demo;
            RegistrationToken = WellKnownRegistrationTokens.ForEnvironmentType(parsedType);
        }
    }

    partial void OnCommandCenterUrlChanged(string value)
    {
        OnPropertyChanged(nameof(CanContinueFromEnvironment));
        ContinueToReviewCommand.NotifyCanExecuteChanged();
    }

    // -----------------------------------------------------------------
    // Auto-determined implementation details — never asked of the operator, only ever shown
    // (editable) inside the collapsed Advanced panel.
    // -----------------------------------------------------------------

    public string AutoDockerNetworkName => "aovias-net";

    public string AutoConfigRoot => "/opt/aovias/config";

    // Plain HTTP, not HTTPS: the Runtime Agent container listens directly on this port with no TLS
    // (ASPNETCORE_URLS is "http://+:7080" in its generated docker-compose.yml, and Bootstrap does
    // not provision an nginx/TLS-terminating reverse proxy in front of it — that exists only as an
    // optional, manually-configured host-level template today). An https:// URL here would make
    // every registration/heartbeat/SignalR call target a TLS handshake against a plain-HTTP listener.
    public string AutoPublicUrl => string.IsNullOrWhiteSpace(Host) ? string.Empty : $"http://{Host}:{RuntimeAgentDefaultPort}";

    [ObservableProperty]
    private string agentArtifactPathOverride = string.Empty;

    [RelayCommand(CanExecute = nameof(CanContinueFromEnvironment))]
    private async Task ContinueToReviewAsync()
    {
        WizardStep = 2;
        await DetectLatestRuntimeAgentAsync();
    }

    [RelayCommand]
    private void BackToTargetHost() => WizardStep = 0;

    [RelayCommand]
    private void BackToEnvironment() => WizardStep = 1;

    // -----------------------------------------------------------------
    // Step 2: Review
    // -----------------------------------------------------------------

    [ObservableProperty]
    private bool isDetectingAgent;

    [ObservableProperty]
    private string detectedAgentVersion = "Not detected";

    [ObservableProperty]
    private string? detectedAgentPackagePath;

    [ObservableProperty]
    private string? selectedRuntimeAgentVersion;

    public string EstimatedTime => "About 2 minutes";

    [RelayCommand]
    private async Task DetectLatestRuntimeAgentAsync()
    {
        IsDetectingAgent = true;
        try
        {
            var selection = await publishingWorkspaceService.GetLatestRuntimeAgentArtifactAsync();
            if (selection is null)
            {
                DetectedAgentVersion = "No Runtime Agent release found in the Release Library.";
                DetectedAgentPackagePath = null;
                SelectedRuntimeAgentVersion = null;
                return;
            }

            var latest = selection.Record;
            DetectedAgentVersion = latest.IsCertified ? $"{latest.Version} (certified)" : $"{latest.Version} (uncertified)";
            DetectedAgentPackagePath = selection.PackagePath;
            SelectedRuntimeAgentVersion = latest.Version;
        }
        catch (Exception exception)
        {
            DetectedAgentVersion = $"Could not query the Release Library: {exception.Message}";
            DetectedAgentPackagePath = null;
            SelectedRuntimeAgentVersion = null;
        }
        finally
        {
            IsDetectingAgent = false;
        }
    }

    [RelayCommand]
    private async Task StartExecutionAsync()
    {
        WizardStep = 3;
        await DetectLatestRuntimeAgentAsync();
        BeginBootstrapRun();
    }

    // -----------------------------------------------------------------
    // Step 3: Execute — live Operation Engine timeline
    // -----------------------------------------------------------------

    /// <summary>The single reusable Operation Engine view — the Execute step and the Advanced panel's
    /// log export both read from this rather than duplicating any progress/log tracking here.</summary>
    public OperationViewModel Operation { get; }

    /// <summary>The one place a sudo password is ever collected or held — see its own remarks. Bound
    /// by the Execute step to show the inline "Administrator Privileges Required" panel only when
    /// <see cref="Aovias.Bootstrap.Core.BootstrapOrchestrator"/> actually needs a password mid-run.</summary>
    public SudoElevationPromptViewModel SudoElevationPrompt { get; }

    [ObservableProperty]
    private string executionStartError = string.Empty;

    public bool HasExecutionStartError => !string.IsNullOrEmpty(ExecutionStartError);

    partial void OnExecutionStartErrorChanged(string value) => OnPropertyChanged(nameof(HasExecutionStartError));

    private void BeginBootstrapRun()
    {
        ExecutionStartError = string.Empty;
        SudoElevationPrompt.Reset();

        try
        {
            var target = BuildTarget();
            var workingDirectory = RootPathResolver.ResolveBootstrapWorkingDirectory();
            var executor = new BootstrapOperationExecutor(new BootstrapRunner(), target, ForceReapply, workingDirectory, SudoElevationPrompt);
            var instance = operationEngine.Start(executor);
            Operation.Attach(instance);
        }
        catch (InvalidOperationException exception)
        {
            // Engine enforces one active run per target — surface rather than crash; this is a
            // real, expected condition (e.g. a stray double-click), not a provisioning failure.
            ExecutionStartError = exception.Message;
        }
    }

    private void OnOperationCompleted()
    {
        if (Operation.StatusText == nameof(OperationStatus.Completed))
        {
            _ = CompleteSuccessfullyAsync();
        }
    }

    [RelayCommand]
    private void BackToReview() => WizardStep = 2;

    // -----------------------------------------------------------------
    // Advanced panel (collapsed by default)
    // -----------------------------------------------------------------

    [ObservableProperty]
    private bool isAdvancedExpanded;

    public string ComposedCommand => BootstrapCliCommandFormatter.Compose(BuildTarget(), ForceReapply, RootPathResolver.ResolveBootstrapWorkingDirectory());

    [RelayCommand]
    private void CopyCommand()
    {
        try
        {
            Clipboard.SetText(ComposedCommand);
        }
        catch
        {
            // Clipboard access can transiently fail on Windows (another process holding it) — not
            // worth surfacing as an operation failure; the operator can simply try again.
        }
    }

    [RelayCommand]
    private void ExportLogs()
    {
        try
        {
            var path = Path.Combine(Path.GetTempPath(), $"aovias-bootstrap-{Guid.NewGuid():N}.log");
            File.WriteAllLines(path, Operation.CombinedLog);
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }
        catch
        {
            // Best-effort convenience action; failure here must not be reported as a provisioning
            // failure, and there is nowhere on this panel to show a secondary error message.
        }
    }

    // -----------------------------------------------------------------
    // Success: Environment Summary
    // -----------------------------------------------------------------

    [ObservableProperty]
    private RemoteEnvironment? registeredEnvironment;

    [ObservableProperty]
    private EnvironmentHealthSnapshot? environmentHealth;

    [ObservableProperty]
    private string registrationNote = string.Empty;

    private async Task CompleteSuccessfullyAsync()
    {
        try
        {
            // The Bootstrap engine's own final phase already registered this environment with the
            // real Fleet Registry over HTTP — this call re-registers it (the same endpoint an
            // operator would hit manually via the Environments workspace's own "Register" action)
            // purely so CSM's local environment list picks it up. CSM's local store and the engine's
            // own registration are two independent, unsynced paths.
            var environmentTypeValue = Enum.TryParse<EnvironmentType>(EnvironmentType, out var parsed) ? parsed : Aovias.Release.Contracts.RemoteDeployment.EnvironmentType.Demo;
            RegisteredEnvironment = await environmentManager.RegisterEnvironmentAsync(new AgentRegistrationRequest
            {
                RegistrationToken = RegistrationToken,
                EnvironmentName = EnvironmentName,
                EnvironmentType = environmentTypeValue,
                RuntimeVersion = DetectedAgentVersion,
                Hostname = Facts?.Hostname ?? Host,
                PublicUrl = AutoPublicUrl
            });

            EnvironmentHealth = await environmentManager.RefreshHealthAsync(RegisteredEnvironment.Id);
            RegistrationNote = string.Empty;
        }
        catch (Exception exception)
        {
            // Bootstrap itself already succeeded — a registration-sync failure here must not be
            // reported as a provisioning failure. Fall back to a locally-known record so the
            // environment still shows up, and say plainly that live health isn't available.
            RegisteredEnvironment = new RemoteEnvironment
            {
                Id = Guid.NewGuid().ToString("N"),
                Name = EnvironmentName,
                BaseUrl = AutoPublicUrl,
                RuntimeAgentEndpoint = AutoPublicUrl,
                EnvironmentType = Enum.TryParse<EnvironmentType>(EnvironmentType, out var parsedType) ? parsedType : Aovias.Release.Contracts.RemoteDeployment.EnvironmentType.Demo,
                RuntimeVersion = DetectedAgentVersion,
                HealthStatus = "Unknown",
                IsRegistered = false
            };
            await environmentManager.SaveEnvironmentAsync(RegisteredEnvironment);
            RegistrationNote = $"Registered locally only — live health is unavailable ({exception.Message}).";
        }

        IsComplete = true;
    }

    [RelayCommand]
    private void OpenRuntime() => workspaceNavigator.NavigateTo(ShellViewModel.WorkspaceRuntime);

    [RelayCommand]
    private void OpenMonitoring() => workspaceNavigator.NavigateTo(ShellViewModel.WorkspaceMonitoring);

    [RelayCommand]
    private void DeployApplications() => workspaceNavigator.NavigateTo(ShellViewModel.WorkspaceDeployments);

    [RelayCommand]
    private void ViewLogs() => IsAdvancedExpanded = true;

    [RelayCommand]
    private async Task RepairEnvironmentAsync()
    {
        IsComplete = false;
        ForceReapply = true;
        WizardStep = 3;
        await DetectLatestRuntimeAgentAsync();
        BeginBootstrapRun();
    }

    [RelayCommand]
    private async Task UpgradeEnvironmentAsync()
    {
        IsComplete = false;
        ForceReapply = true;
        WizardStep = 3;
        await DetectLatestRuntimeAgentAsync();
        BeginBootstrapRun();
    }

    [RelayCommand]
    private void ReconcileEnvironment() => workspaceNavigator.NavigateTo(ShellViewModel.WorkspaceReconciliation);

    [RelayCommand]
    private void StartOver()
    {
        WizardStep = 0;
        IsComplete = false;
        IsConnected = false;
        Facts = null;
        ForceReapply = false;
        ExecutionStartError = string.Empty;
        RegisteredEnvironment = null;
        EnvironmentHealth = null;
        SudoElevationPrompt.Reset();
    }

    // -----------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------

    private BootstrapTarget BuildTarget() => new()
    {
        Host = Host,
        Port = ParsePort(),
        Username = Username,
        PrivateKeyPath = UseKeyAuth ? PrivateKeyPath : null,
        Password = UseKeyAuth ? null : Password,
        ProfileName = RuntimeProfile,
        EnvironmentName = EnvironmentName,
        EnvironmentType = Enum.TryParse<EnvironmentType>(EnvironmentType, out var parsedType) ? parsedType : Aovias.Release.Contracts.RemoteDeployment.EnvironmentType.Demo,
        RegistrationToken = RegistrationToken,
        PublicUrl = AutoPublicUrl,
        CommandCenterBaseUrl = CommandCenterUrl,
        RuntimeAgentArtifactPath = string.IsNullOrWhiteSpace(AgentArtifactPathOverride) ? DetectedAgentPackagePath ?? string.Empty : AgentArtifactPathOverride,
        RuntimeAgentArtifactVersion = string.IsNullOrWhiteSpace(AgentArtifactPathOverride) ? SelectedRuntimeAgentVersion : null,
        RemoteConfigRoot = AutoConfigRoot,
        DockerNetworkName = AutoDockerNetworkName
    };

    private int ParsePort() => int.TryParse(SshPort, out var port) && port > 0 ? port : 22;
}
