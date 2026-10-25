namespace Aovias.Deployment.Contracts.Infrastructure.Providers;

/// <summary>
/// Pluggable secret resolution backend declared by deployment contracts.
/// </summary>
public interface ISecretProvider
{
    string ProviderName { get; }

    bool CanResolve(SecretProviderRequest request);

    Task<string?> ResolveAsync(SecretProviderRequest request, CancellationToken cancellationToken = default);
}

public sealed class SecretProviderRequest
{
    public required string SecretName { get; init; }

    public required string Source { get; init; }

    public required string ConfigRoot { get; init; }
}

/// <summary>
/// Pluggable certificate resolution backend declared by deployment contracts.
/// </summary>
public interface ICertificateProvider
{
    string ProviderName { get; }

    bool CanLoad(CertificateProviderRequest request);

    Task<CertificateProviderResult> LoadAsync(
        CertificateProviderRequest request,
        CancellationToken cancellationToken = default);
}

public sealed class CertificateProviderRequest
{
    public required string CertificatePath { get; init; }

    public string? PasswordSecret { get; init; }

    public required string Type { get; init; }

    public required string ConfigRoot { get; init; }
}

public sealed class CertificateProviderResult
{
    public bool Success { get; init; }

    public string? Thumbprint { get; init; }

    public IReadOnlyList<string> Errors { get; init; } = [];

    public static CertificateProviderResult Ok(string thumbprint)
        => new() { Success = true, Thumbprint = thumbprint };

    public static CertificateProviderResult Fail(params string[] errors)
        => new() { Success = false, Errors = errors };
}

/// <summary>
/// Resolves secrets from settings.secrets.json files (default provider).
/// </summary>
public sealed class FileSecretProvider : ISecretProvider
{
    public string ProviderName => "File";

    public bool CanResolve(SecretProviderRequest request)
        => request.Source.Contains("settings.secrets.json", StringComparison.OrdinalIgnoreCase);

    public Task<string?> ResolveAsync(SecretProviderRequest request, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        Provisioning.ContractSecretValidator.TryResolveSecretValue(request.ConfigRoot, request.SecretName, out var value);
        return Task.FromResult<string?>(value);
    }
}
