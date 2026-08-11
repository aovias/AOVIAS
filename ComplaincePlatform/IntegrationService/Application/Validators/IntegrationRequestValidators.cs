using FluentValidation;
using IntegrationService.Application.Contracts;

namespace IntegrationService.Application.Validators;

public sealed class CreateConnectorRequestValidator : AbstractValidator<CreateConnectorRequest>
{
    public CreateConnectorRequestValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ProviderType).NotEmpty().MaximumLength(100);
    }
}

public sealed class CreateSyncJobRequestValidator : AbstractValidator<CreateSyncJobRequest>
{
    public CreateSyncJobRequestValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.SourceConnectorId).NotEmpty();
        RuleFor(x => x.MappingProfileId).NotEmpty();
    }
}

public sealed class CreateImportRequestValidator : AbstractValidator<CreateImportRequest>
{
    public CreateImportRequestValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();
        RuleFor(x => x.Content).NotEmpty();
        RuleFor(x => x.FileFormat).NotEmpty();
    }
}
