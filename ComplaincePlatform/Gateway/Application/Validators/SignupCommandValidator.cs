using FluentValidation;
using Gateway.Application.Commands;

namespace Gateway.Application.Validators;

/// <summary>
/// Validator for SignupCommand using FluentValidation.
/// </summary>
public class SignupCommandValidator : AbstractValidator<SignupCommand>
{
    public SignupCommandValidator()
    {
        RuleFor(x => x.Request)
            .NotNull()
            .WithMessage("Signup request is required.");

        When(x => x.Request != null, () =>
        {
            RuleFor(x => x.Request.Organization)
                .NotNull()
                .WithMessage("Organization information is required.");

            RuleFor(x => x.Request.Organization.Name)
                .NotEmpty()
                .WithMessage("Organization name is required.")
                .MaximumLength(200)
                .WithMessage("Organization name must not exceed 200 characters.");

            RuleFor(x => x.Request.Organization.Type)
                .NotEmpty()
                .WithMessage("Organization type is required.");

            RuleFor(x => x.Request.Organization.Address)
                .NotEmpty()
                .WithMessage("Organization address is required.");

            RuleFor(x => x.Request.Hospital)
                .NotNull()
                .WithMessage("Hospital information is required.");

            RuleFor(x => x.Request.Hospital.Name)
                .NotEmpty()
                .WithMessage("Hospital name is required.")
                .MaximumLength(200)
                .WithMessage("Hospital name must not exceed 200 characters.");

            RuleFor(x => x.Request.Hospital.Code)
                .NotEmpty()
                .WithMessage("Hospital code is required.")
                .MaximumLength(20)
                .WithMessage("Hospital code must not exceed 20 characters.");

            RuleFor(x => x.Request.Hospital.Address)
                .NotEmpty()
                .WithMessage("Hospital address is required.");

            RuleFor(x => x.Request.AdminUser)
                .NotNull()
                .WithMessage("Admin user information is required.");

            RuleFor(x => x.Request.AdminUser.Email)
                .NotEmpty()
                .WithMessage("Email is required.")
                .EmailAddress()
                .WithMessage("Invalid email format.");

            RuleFor(x => x.Request.AdminUser.Password)
                .NotEmpty()
                .WithMessage("Password is required.")
                .MinimumLength(8)
                .WithMessage("Password must be at least 8 characters long.");
        });
    }
}
