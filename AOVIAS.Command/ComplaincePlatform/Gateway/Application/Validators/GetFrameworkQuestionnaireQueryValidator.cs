using FluentValidation;
using Gateway.Application.Queries;

namespace Gateway.Application.Validators;

public sealed class GetFrameworkQuestionnaireQueryValidator : AbstractValidator<GetFrameworkQuestionnaireQuery>
{
    public GetFrameworkQuestionnaireQueryValidator()
    {
        RuleFor(x => x.FrameworkId)
            .NotEmpty()
            .WithMessage("FrameworkId is required.");
    }
}
