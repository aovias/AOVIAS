using FluentValidation;
using OrganizationService.Application.Commands;
using OrganizationService.Domain.Entities;

namespace OrganizationService.Application.Validators;

public class UpdateOrganizationBrandingCommandValidator : AbstractValidator<UpdateOrganizationBrandingCommand>
{
    public UpdateOrganizationBrandingCommandValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();
        RuleFor(x => x.Request.Config.Colors.Primary).Must(BeHexColor).WithMessage("Primary color must be a valid hex color.");
        RuleFor(x => x.Request.Config.Colors.Secondary).Must(BeHexColor).WithMessage("Secondary color must be a valid hex color.");
        RuleFor(x => x.Request.Config.Colors.Accent).Must(BeHexColor).WithMessage("Accent color must be a valid hex color.");
    }

    private static bool BeHexColor(string value) =>
        !string.IsNullOrWhiteSpace(value) && System.Text.RegularExpressions.Regex.IsMatch(value, "^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$");
}

public class UploadBrandingAssetCommandValidator : AbstractValidator<UploadBrandingAssetCommand>
{
    public UploadBrandingAssetCommandValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();
        RuleFor(x => x.AssetKey).Must(k => OrganizationSettingsDefaults.BrandingAssetKeys.Contains(k, StringComparer.OrdinalIgnoreCase));
        RuleFor(x => x.Length).GreaterThan(0);
        RuleFor(x => x.FileName).NotEmpty();
    }
}

public class RemoveBrandingAssetCommandValidator : AbstractValidator<RemoveBrandingAssetCommand>
{
    public RemoveBrandingAssetCommandValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();
        RuleFor(x => x.AssetKey).Must(k => OrganizationSettingsDefaults.BrandingAssetKeys.Contains(k, StringComparer.OrdinalIgnoreCase));
    }
}
