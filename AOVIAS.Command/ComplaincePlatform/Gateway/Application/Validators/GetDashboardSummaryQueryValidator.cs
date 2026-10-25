using FluentValidation;
using Gateway.Application.Queries;

namespace Gateway.Application.Validators;

/// <summary>
/// Validator for GetDashboardSummaryQuery using FluentValidation.
/// </summary>
public class GetDashboardSummaryQueryValidator : AbstractValidator<GetDashboardSummaryQuery>
{
    public GetDashboardSummaryQueryValidator()
    {
        RuleFor(x => x.HospitalId)
            .NotEmpty()
            .WithMessage("HospitalId is required.")
            .NotEqual(Guid.Empty)
            .WithMessage("HospitalId cannot be an empty GUID.");
    }
}
