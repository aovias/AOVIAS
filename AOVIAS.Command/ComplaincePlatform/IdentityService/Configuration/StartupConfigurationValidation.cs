namespace IdentityService.Configuration;

/// <summary>
/// Validates configuration that must be correct before serving traffic (especially refresh/login flows).
/// </summary>
public static class StartupConfigurationValidation
{
    /// <summary>
    /// Validates optional URLs when present. JWT signing settings are enforced in <see cref="IdentityService.Extensions.ServiceCollectionExtensions.AddJwtAuthentication(Microsoft.Extensions.DependencyInjection.IServiceCollection,Microsoft.Extensions.Configuration.IConfiguration)"/>.
    /// IdentityServer signing credentials are validated in <see cref="IdentitySigningCredentialConfigurer"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">When a configured URL is present but invalid.</exception>
    public static void Validate(IConfiguration configuration, IHostEnvironment environment)
    {
        var orgUrl = configuration["OrganizationService:BaseUrl"];
        if (!string.IsNullOrWhiteSpace(orgUrl) && !Uri.TryCreate(orgUrl, UriKind.Absolute, out _))
        {
            throw new InvalidOperationException(
                "OrganizationService:BaseUrl must be an absolute URI (e.g. https://organization:8081) when set.");
        }

        IdentitySigningCredentialConfigurer.Validate(configuration, environment);
    }
}
