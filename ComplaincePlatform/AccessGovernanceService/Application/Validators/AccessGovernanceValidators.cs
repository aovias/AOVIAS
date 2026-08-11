using AccessGovernanceService.Application.Commands;
using FluentValidation;

namespace AccessGovernanceService.Application.Validators;

public class CreateRoleCommandValidator : AbstractValidator<CreateRoleCommand>
{
    public CreateRoleCommandValidator()
    {
        RuleFor(x => x.Request.RoleCode).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Request.RoleName).NotEmpty().MaximumLength(128);
    }
}

public class CreatePermissionCommandValidator : AbstractValidator<CreatePermissionCommand>
{
    public CreatePermissionCommandValidator() => RuleFor(x => x.Request.Code).NotEmpty().Matches(@"^[A-Za-z0-9_.]+$");
}

public class CreateDelegationCommandValidator : AbstractValidator<CreateDelegationCommand>
{
    public CreateDelegationCommandValidator()
    {
        RuleFor(x => x.Request.FromUserId).NotEmpty();
        RuleFor(x => x.Request.ToUserId).NotEmpty();
        RuleFor(x => x).Must(x => x.Request.FromUserId != x.Request.ToUserId).WithMessage("Cannot delegate to self.");
    }
}

public class EvaluateAuthorizationCommandValidator : AbstractValidator<EvaluateAuthorizationCommand>
{
    public EvaluateAuthorizationCommandValidator()
    {
        RuleFor(x => x.Request.SubjectId).NotEmpty();
        RuleFor(x => x.Request.Resource).NotEmpty();
        RuleFor(x => x.Request.Action).NotEmpty();
    }
}
