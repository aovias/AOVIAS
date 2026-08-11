using FluentValidation;
using Gateway.Application.Queries;

namespace Gateway.Application.Validators;

/// <summary>
/// Validator for GetOrganizationDetailQuery using FluentValidation.
/// </summary>
public class GetOrganizationDetailQueryValidator : AbstractValidator<GetOrganizationDetailQuery>
{
    public GetOrganizationDetailQueryValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty()
            .WithMessage("TenantId is required.")
            .NotEqual(Guid.Empty)
            .WithMessage("TenantId cannot be an empty GUID.");
    }
}
