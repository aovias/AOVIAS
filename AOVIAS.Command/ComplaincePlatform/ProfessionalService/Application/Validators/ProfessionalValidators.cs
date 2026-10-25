using FluentValidation;
using ProfessionalService.Application.Commands;
using ProfessionalService.Application.DTOs;

namespace ProfessionalService.Application.Validators;

public class CreateProfessionalProfileCommandValidator : AbstractValidator<CreateProfessionalProfileCommand>
{
    public CreateProfessionalProfileCommandValidator()
    {
        RuleFor(x => x.Request.EmploymentId).NotEmpty();
    }
}

public class AddRegistrationCommandValidator : AbstractValidator<AddRegistrationCommand>
{
    public AddRegistrationCommandValidator()
    {
        RuleFor(x => x.Request.RegistrationNumber).NotEmpty();
        RuleFor(x => x.Request).Must(r => !r.IssueDate.HasValue || !r.ExpiryDate.HasValue || r.ExpiryDate > r.IssueDate)
            .WithMessage("Expiry date must be after issue date.");
    }
}

public class AddLicenseCommandValidator : AbstractValidator<AddLicenseCommand>
{
    public AddLicenseCommandValidator()
    {
        RuleFor(x => x.Request.LicenseNumber).NotEmpty();
        RuleFor(x => x.Request).Must(r => !r.IssueDate.HasValue || !r.ExpiryDate.HasValue || r.ExpiryDate > r.IssueDate)
            .WithMessage("Expiry date must be after issue date.");
    }
}
