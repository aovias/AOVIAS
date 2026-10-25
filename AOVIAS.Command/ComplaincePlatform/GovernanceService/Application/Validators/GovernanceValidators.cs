using GovernanceService.Application.Commands;
using FluentValidation;

namespace GovernanceService.Application.Validators;

public class CreateResponsibilityCommandValidator : AbstractValidator<CreateResponsibilityCommand>
{
    public CreateResponsibilityCommandValidator()
    {
        RuleFor(x => x.Request.ResponsibilityCode).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Request.ResponsibilityTypeValueId).NotEmpty();
        RuleFor(x => x.Request.GovernedResourceType).NotEmpty();
    }
}

public class CreateCommitteeCommandValidator : AbstractValidator<CreateCommitteeCommand>
{
    public CreateCommitteeCommandValidator()
    {
        RuleFor(x => x.Request.CommitteeCode).NotEmpty();
        RuleFor(x => x.Request.CommitteeName).NotEmpty();
    }
}

public class CreateEscalationChainCommandValidator : AbstractValidator<CreateEscalationChainCommand>
{
    public CreateEscalationChainCommandValidator() => RuleFor(x => x.Request.ChainCode).NotEmpty();
}
