using FluentValidation;
using OrganizationService.Application.Commands;

namespace OrganizationService.Application.Validators;

public class CreateMembershipCommandValidator : AbstractValidator<CreateMembershipCommand>
{
    public CreateMembershipCommandValidator()
    {
        RuleFor(x => x.Request.OrganizationId).NotEmpty();
        RuleFor(x => x.Request.UserId).NotEmpty();
        RuleFor(x => x.Request.Role).NotEmpty();
    }
}

public class CreateInvitationCommandValidator : AbstractValidator<CreateInvitationCommand>
{
    public CreateInvitationCommandValidator()
    {
        RuleFor(x => x.Request.OrganizationId).NotEmpty();
        RuleFor(x => x.Request.HospitalId).NotEmpty();
        RuleFor(x => x.Request.Email).NotEmpty().EmailAddress();
    }
}
