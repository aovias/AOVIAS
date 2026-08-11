using FluentValidation;
using Gateway.DTOs.Setup;

namespace Gateway.Application.Validators;

public sealed class SetupConfigureRequestValidator : AbstractValidator<SetupConfigureRequest>
{
    public SetupConfigureRequestValidator()
    {
        RuleFor(x => x)
            .Must(HasAtLeastOneSection)
            .WithMessage("At least one configuration section (Database, Smtp, Storage, Ai, Auth) is required.");

        When(x => x.Database is not null, () =>
        {
            RuleFor(x => x.Database!.ConnectionString)
                .NotEmpty()
                .WithMessage("Database.ConnectionString is required when Database section is provided.");
        });

        When(x => x.Smtp is not null, () =>
        {
            RuleFor(x => x.Smtp!.Host)
                .NotEmpty()
                .WithMessage("Smtp.Host is required when Smtp section is provided.");
            RuleFor(x => x.Smtp!.Port)
                .InclusiveBetween(1, 65535)
                .When(x => x.Smtp!.Port.HasValue);
            RuleFor(x => x.Smtp!.FromAddress)
                .EmailAddress()
                .When(x => !string.IsNullOrWhiteSpace(x.Smtp!.FromAddress));
        });

        When(x => x.Storage is not null, () =>
        {
            RuleFor(x => x.Storage!.Provider)
                .NotEmpty()
                .WithMessage("Storage.Provider is required when Storage section is provided.");
        });

        When(x => x.Ai is not null && x.Ai.Enabled, () =>
        {
            RuleFor(x => x.Ai!.Endpoint)
                .NotEmpty()
                .WithMessage("Ai.Endpoint is required when AI is enabled.");
        });

        When(x => x.Auth is not null, () =>
        {
            RuleFor(x => x.Auth!.JwtSecretKey)
                .MinimumLength(32)
                .When(x => !string.IsNullOrWhiteSpace(x.Auth!.JwtSecretKey))
                .WithMessage("Auth.JwtSecretKey must be at least 32 characters when provided.");
            RuleFor(x => x.Auth!.ExpirationMinutes)
                .InclusiveBetween(5, 1440)
                .When(x => x.Auth!.ExpirationMinutes.HasValue);
        });
    }

    private static bool HasAtLeastOneSection(SetupConfigureRequest request)
        => request.Database is not null
           || request.Smtp is not null
           || request.Storage is not null
           || request.Ai is not null
           || request.Auth is not null;
}
