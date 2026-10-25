using FluentValidation;
using WorkforceService.Application.Commands;

namespace WorkforceService.Application.Validators;

public class CreateEmploymentCommandValidator : AbstractValidator<CreateEmploymentCommand>
{
    public CreateEmploymentCommandValidator()
    {
        RuleFor(x => x.Request.PersonId).NotEmpty();
        RuleFor(x => x.Request.OrganizationId).NotEmpty();
        RuleFor(x => x.Request.HospitalId).NotEmpty();
    }
}
