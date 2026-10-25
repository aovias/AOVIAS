using FluentValidation;
using Gateway.Application.Commands;
using Gateway.DTOs;

namespace Gateway.Application.Validators;

/// <summary>
/// Validates second-step onboarding wizard requests (finalized product rules, Phase 22.5).
/// </summary>
public sealed class OnboardWizardCommandValidator : AbstractValidator<OnboardWizardCommand>
{
    public OnboardWizardCommandValidator()
    {
        RuleFor(x => x.Request)
            .NotNull()
            .WithMessage("Request body is required.");

        When(x => x.Request != null, () =>
        {
            RuleFor(x => x.Request!.OrganizationId)
                .NotEmpty()
                .WithMessage("organizationId is required.");

            RuleFor(x => x.Request!.HospitalId)
                .NotEmpty()
                .WithMessage("hospitalId is required.");

            RuleFor(x => x.Request!.FrameworkCodes)
                .NotEmpty()
                .WithMessage("At least one framework must be selected.");

            RuleForEach(x => x.Request!.FrameworkCodes)
                .Must(c => !string.IsNullOrWhiteSpace(c))
                .WithMessage("Framework code cannot be empty.");

            RuleFor(x => x.Request!.HospitalProfile)
                .NotNull()
                .WithMessage("hospitalProfile is required.");

            RuleFor(x => x.Request!.HospitalProfile!.DepartmentIds)
                .NotEmpty()
                .WithMessage("At least one department must be selected in the hospital profile.");

            RuleFor(x => x.Request!)
                .Must(r => r.HospitalProfile.HospitalId == Guid.Empty || r.HospitalProfile.HospitalId == r.HospitalId)
                .WithMessage("hospitalProfile.hospitalId must match hospitalId.")
                .WithName(nameof(OnboardWizardRequestDto.HospitalProfile));

            RuleFor(x => x.Request!.TeamInvites)
                .Custom(ValidateInviteDepartmentsSubset);

            When(x => x.Request!.TeamInvites.Count > 0, () =>
            {
                RuleForEach(x => x.Request!.TeamInvites).ChildRules(invite =>
                {
                    invite.RuleFor(i => i.Name)
                        .NotEmpty()
                        .WithMessage("Invite name is required.");

                    invite.RuleFor(i => i.Email)
                        .NotEmpty()
                        .WithMessage("Invite email is required.")
                        .EmailAddress()
                        .WithMessage("Invite email is not a valid email address.");

                    invite.RuleFor(i => i.Role)
                        .NotEmpty()
                        .WithMessage("Invite role is required.");

                    invite.RuleFor(i => i.DepartmentId)
                        .Must(id => id.HasValue && id.Value != Guid.Empty)
                        .WithMessage("Invite department is required.");
                });
            });
        });
    }

    private static void ValidateInviteDepartmentsSubset(
        List<TeamInviteClientDto> invites,
        ValidationContext<OnboardWizardCommand> context)
    {
        var req = context.InstanceToValidate.Request;
        if (req == null || invites.Count == 0)
        {
            return;
        }

        var allowed = new HashSet<Guid>(req.HospitalProfile.DepartmentIds);
        for (var i = 0; i < invites.Count; i++)
        {
            var inv = invites[i];
            if (!inv.DepartmentId.HasValue || inv.DepartmentId.Value == Guid.Empty)
            {
                continue;
            }

            if (!allowed.Contains(inv.DepartmentId.Value))
            {
                context.AddFailure(
                    $"{nameof(OnboardWizardRequestDto.TeamInvites)}[{i}].{nameof(TeamInviteClientDto.DepartmentId)}",
                    "Invite department must be one of the departments selected in the hospital profile (step 1).");
            }
        }
    }
}
