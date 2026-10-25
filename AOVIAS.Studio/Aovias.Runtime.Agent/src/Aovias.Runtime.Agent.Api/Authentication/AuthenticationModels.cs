namespace Aovias.Runtime.Agent.Api.Authentication;

/// <summary>
/// Represents an access token model for future JWT authentication.
/// </summary>
public sealed class RuntimeAgentToken
{
    /// <summary>
    /// Gets or sets the token type.
    /// </summary>
    public string TokenType { get; init; } = "Bearer";

    /// <summary>
    /// Gets or sets the access token value.
    /// </summary>
    public string AccessToken { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the token expiration timestamp.
    /// </summary>
    public DateTimeOffset ExpiresAt { get; init; }
}

/// <summary>
/// Authentication configuration for the runtime agent API.
/// </summary>
public sealed class AuthenticationOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "Authentication";

    /// <summary>
    /// Gets or sets a value indicating whether authentication is enabled.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets the authority URL for future JWT validation.
    /// </summary>
    public string Authority { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the expected audience for future JWT validation.
    /// </summary>
    public string Audience { get; set; } = "aovias-runtime-agent";
}

/// <summary>
/// Defines future token validation behavior for the runtime agent API.
/// </summary>
public interface ITokenValidator
{
    /// <summary>
    /// Validates an access token.
    /// </summary>
    /// <param name="accessToken">The access token.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns><see langword="true"/> when valid; otherwise <see langword="false"/>.</returns>
    Task<bool> ValidateAsync(string accessToken, CancellationToken cancellationToken = default);
}

/// <summary>
/// Placeholder token validator used until JWT authentication is implemented.
/// </summary>
public sealed class NoOpTokenValidator : ITokenValidator
{
    /// <inheritdoc />
    public Task<bool> ValidateAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(!string.IsNullOrWhiteSpace(accessToken));
    }
}
