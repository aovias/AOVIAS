using Aovias.Configuration.Settings;
using Aovias.Orchestration.Contracts.Providers;
using Microsoft.Extensions.Options;

namespace Aovias.Runtime.Agent.Infrastructure.Providers;

/// <summary>
/// Certificate provider architecture stub.
/// </summary>
public sealed class OrchestrationCertificateProvider : ICertificateProvider
{
    private readonly IOptionsMonitor<ProviderSettings> providerSettings;

    public OrchestrationCertificateProvider(IOptionsMonitor<ProviderSettings> providerSettings)
    {
        this.providerSettings = providerSettings;
    }

    /// <inheritdoc />
    public string ProviderName => "Certificate";

    /// <inheritdoc />
    public bool IsAvailable => providerSettings.CurrentValue.CertificateEnabled;

    /// <inheritdoc />
    public Task<string> GetStatusAsync(CancellationToken cancellationToken = default)
        => Task.FromResult("NotInstalled");
}
