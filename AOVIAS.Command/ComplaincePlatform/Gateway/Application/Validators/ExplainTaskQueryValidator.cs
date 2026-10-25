using FluentValidation;
using Gateway.Application.Queries;

namespace Gateway.Application.Validators;

/// <summary>
/// Validator for ExplainTaskQuery using FluentValidation.
/// </summary>
public class ExplainTaskQueryValidator : AbstractValidator<ExplainTaskQuery>
{
    public ExplainTaskQueryValidator()
    {
        RuleFor(x => x.RequestBody)
            .NotNull()
            .WithMessage("Request body is required.");

        RuleFor(x => x.HospitalId)
            .NotEmpty()
            .WithMessage("HospitalId is required.")
            .NotEqual(Guid.Empty)
            .WithMessage("HospitalId cannot be an empty GUID.");
    }
}
