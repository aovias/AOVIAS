using FluentValidation;
using Gateway.Application.Queries;

namespace Gateway.Application.Validators;

public sealed class GetOnboardingQuestionnaireQueryValidator : AbstractValidator<GetOnboardingQuestionnaireQuery>
{
    public GetOnboardingQuestionnaireQueryValidator()
    {
        RuleFor(x => x.FrameworkId)
            .NotEmpty()
            .WithMessage("FrameworkId is required.");
    }
}
