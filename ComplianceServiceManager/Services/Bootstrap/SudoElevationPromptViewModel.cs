using Aovias.Bootstrap.Contracts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ComplianceServiceManager.Services.Bootstrap;

/// <summary>
/// The presentation-layer implementation of <see cref="IBootstrapElevationHandler"/> — the only
/// place in Bootstrap that shows UI for a sudo password, and the only place that ever holds one.
/// <see cref="RequestSudoPasswordAsync"/> is called by <see cref="Aovias.Bootstrap.Core.BootstrapOrchestrator"/>
/// from its background execution thread; it marshals onto the UI thread to show the inline
/// "Administrator Privileges Required" panel (see <c>BootstrapView.xaml</c>'s Execute step) and
/// suspends the caller on a <see cref="TaskCompletionSource{TResult}"/> until the operator submits
/// or cancels — this is what makes "pause execution... resume immediately after authentication"
/// work without polling.
///
/// The password never leaves this class as anything other than the one-shot
/// <see cref="SudoPasswordPromptResult"/> handed back to the orchestrator: it is never assigned to a
/// property that outlives one prompt, never logged, and the bound <see cref="Password"/> field is
/// cleared the instant it is submitted or the prompt is cancelled.
/// </summary>
public sealed partial class SudoElevationPromptViewModel : ObservableObject, IBootstrapElevationHandler
{
    private TaskCompletionSource<SudoPasswordPromptResult>? pendingRequest;

    [ObservableProperty]
    private bool isVisible;

    [ObservableProperty]
    private bool hasAuthenticationFailed;

    /// <summary>Bound to the Execute step's PasswordBox via code-behind's PasswordChanged handler — never bound directly in XAML (same convention as <c>BootstrapViewModel.Password</c>).</summary>
    [ObservableProperty]
    private string password = string.Empty;

    [ObservableProperty]
    private bool remember = true;

    public bool CanContinue => !string.IsNullOrEmpty(Password);

    partial void OnPasswordChanged(string value) => OnPropertyChanged(nameof(CanContinue));

    public Task<SudoPasswordPromptResult> RequestSudoPasswordAsync(bool isRetryAfterRejection, CancellationToken cancellationToken)
    {
        var request = new TaskCompletionSource<SudoPasswordPromptResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        pendingRequest = request;

        RunOnUiThread(() =>
        {
            HasAuthenticationFailed = isRetryAfterRejection;
            Password = string.Empty;
            IsVisible = true;
        });

        // Cancelling the whole bootstrap run (the Operation Engine's own Cancel button, not this
        // panel's Cancel button) must also unblock a pending password prompt rather than leaving it
        // waiting forever.
        cancellationToken.Register(() => request.TrySetResult(new SudoPasswordPromptResult { Password = null }));

        return request.Task;
    }

    [RelayCommand]
    private void Continue()
    {
        if (pendingRequest is null || string.IsNullOrEmpty(Password))
        {
            return;
        }

        var result = new SudoPasswordPromptResult { Password = Password, Remember = Remember };
        pendingRequest.TrySetResult(result);
        pendingRequest = null;
        IsVisible = false;
        HasAuthenticationFailed = false;
        Password = string.Empty;
    }

    [RelayCommand]
    private void Cancel()
    {
        pendingRequest?.TrySetResult(new SudoPasswordPromptResult { Password = null });
        pendingRequest = null;
        IsVisible = false;
        HasAuthenticationFailed = false;
        Password = string.Empty;
    }

    /// <summary>Resets all visible/pending state between bootstrap attempts (e.g. Retry, Start Over) so a stale prompt never reappears for a new run.</summary>
    public void Reset()
    {
        pendingRequest?.TrySetResult(new SudoPasswordPromptResult { Password = null });
        pendingRequest = null;
        IsVisible = false;
        HasAuthenticationFailed = false;
        Password = string.Empty;
        Remember = true;
    }

    private static void RunOnUiThread(Action action)
    {
        var dispatcher = System.Windows.Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.CheckAccess())
        {
            action();
        }
        else
        {
            dispatcher.Invoke(action);
        }
    }
}
