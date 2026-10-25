namespace Aovias.Deployment.Contracts.Provisioning;

public sealed class CertificateValidationReport
{
    public bool Passed { get; init; }

    public IReadOnlyList<CertificateValidationItemResult> Results { get; init; } = [];

    public static CertificateValidationReport Success(IReadOnlyList<CertificateValidationItemResult> results)
        => new() { Passed = true, Results = results };

    public static CertificateValidationReport Failure(IReadOnlyList<CertificateValidationItemResult> results)
        => new() { Passed = false, Results = results };
}

public sealed class CertificateValidationItemResult
{
    public required string CertificateId { get; init; }

    public bool Passed { get; init; }

    public IReadOnlyList<string> Errors { get; init; } = [];

    public string? ResolvedPath { get; init; }

    public string? Thumbprint { get; init; }

    public DateTimeOffset? NotAfterUtc { get; init; }
}

public sealed class SecretValidationReport
{
    public bool Passed { get; init; }

    public IReadOnlyList<string> Errors { get; init; } = [];

    public static SecretValidationReport Success() => new() { Passed = true };

    public static SecretValidationReport Failure(IReadOnlyList<string> errors)
        => new() { Passed = false, Errors = errors };
}
