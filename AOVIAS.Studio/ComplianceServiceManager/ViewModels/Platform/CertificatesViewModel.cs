using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ComplianceServiceManager.Services;

namespace ComplianceServiceManager.ViewModels.Platform;

/// <summary>
/// Platform → Certificates workspace: local HTTPS development certificate state plus the
/// platform's certificate conventions (contract-declared certificates provisioned per
/// environment at deploy time).
/// </summary>
public sealed partial class CertificatesViewModel : ObservableObject
{
    public sealed record CertificateConvention(string Area, string Detail);

    private readonly ProcessService processService;

    [ObservableProperty]
    private string devCertStatus = "Not checked";

    [ObservableProperty]
    private string devCertDetail = "Run the check to inspect the ASP.NET Core HTTPS development certificate.";

    [ObservableProperty]
    private bool isBusy;

    public ObservableCollection<CertificateConvention> Conventions { get; } =
    [
        new("Identity signing", "The identity service's JWT signing certificate is declared in its infrastructure contract and provisioned per environment."),
        new("TLS termination", "Environment TLS certificates live under the runtime config root (config/certificates) on each host."),
        new("Deploy-time provisioning", "Certificates are validated during admission; a missing or unreadable certificate blocks deployment before any mutation."),
        new("Local development", "Local services use the ASP.NET Core HTTPS developer certificate — trust it once per machine below.")
    ];

    public CertificatesViewModel(ProcessService processService)
    {
        this.processService = processService;
    }

    [RelayCommand]
    private async Task CheckDevCertificateAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        DevCertStatus = "Checking…";
        try
        {
            var result = await processService.RunCommandAsync(
                "dotnet",
                "dev-certs https --check --trust",
                Environment.CurrentDirectory,
                "Certificates",
                Models.LogLevel.Info,
                CancellationToken.None);
            DevCertStatus = result.Succeeded ? "Valid & trusted" : "Attention required";
            DevCertDetail = result.Succeeded
                ? "A valid, trusted HTTPS development certificate is present."
                : "The development certificate is missing or untrusted. Copy the trust command below and run it in a terminal.";
        }
        catch (Exception exception)
        {
            DevCertStatus = "Check failed";
            DevCertDetail = exception.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void CopyTrustCommand()
    {
        try
        {
            Clipboard.SetText("dotnet dev-certs https --trust");
        }
        catch
        {
            // Clipboard access is best-effort.
        }
    }
}
