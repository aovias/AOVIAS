using FluentValidation;
using Gateway.Application.Queries;

namespace Gateway.Application.Validators;

/// <summary>
/// Validator for LoginQuery using FluentValidation.
/// </summary>
public class LoginQueryValidator : AbstractValidator<LoginQuery>
{
    public LoginQueryValidator()
    {
        RuleFor(x => x.RequestBody)
            .NotNull()
            .WithMessage("Request body is required.");
    }
}
