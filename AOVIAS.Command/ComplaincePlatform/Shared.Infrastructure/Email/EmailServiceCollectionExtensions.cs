using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Shared.Infrastructure.Email;

public static class EmailServiceCollectionExtensions
{
    public static IServiceCollection AddConfigurableEmailService(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment? environment = null)
    {
        services.Configure<SmtpOptions>(configuration.GetSection(SmtpOptions.SectionName));
        services.Configure<EmailProviderOptions>(configuration.GetSection(EmailProviderOptions.SectionName));

        var provider = configuration[$"{EmailProviderOptions.SectionName}:Provider"]
            ?? configuration["Email:Provider"]
            ?? "Console";

        // Development defaults to Console unless explicitly set to Smtp.
        if (environment?.IsDevelopment() == true
            && string.Equals(provider, "Console", StringComparison.OrdinalIgnoreCase))
        {
            services.RemoveAll<IEmailService>();
            services.AddSingleton<IEmailService, ConsoleEmailService>();
            return services;
        }

        if (string.Equals(provider, "Smtp", StringComparison.OrdinalIgnoreCase))
        {
            services.RemoveAll<IEmailService>();
            services.AddSingleton<IEmailService, SmtpEmailService>();
            return services;
        }

        services.RemoveAll<IEmailService>();
        services.AddSingleton<IEmailService, ConsoleEmailService>();
        return services;
    }
}
