using FluentValidation;
using PersonService.Application.Commands;

namespace PersonService.Application.Validators;

public class CreatePersonCommandValidator : AbstractValidator<CreatePersonCommand>
{
    public CreatePersonCommandValidator()
    {
        RuleFor(x => x.Request)
            .NotNull()
            .ChildRules(r =>
            {
                r.RuleFor(x => x!.Email)
                    .EmailAddress()
                    .When(x => !string.IsNullOrWhiteSpace(x!.Email));
                r.RuleFor(x => x!.LegalFirstName)
                    .MaximumLength(200);
                r.RuleFor(x => x!.LegalLastName)
                    .MaximumLength(200);
            });
    }
}
