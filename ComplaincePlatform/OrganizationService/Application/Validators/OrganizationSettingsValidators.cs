using FluentValidation;
using OrganizationService.Application.Commands;
using OrganizationService.Application.DTOs;
using OrganizationService.Application.Queries;
using OrganizationService.Domain.Entities;
using TimeZoneConverter;

namespace OrganizationService.Application.Validators;

public static class OrganizationSettingsValidationRules
{
    public static bool IsValidOrganizationCode(string code) =>
        !string.IsNullOrWhiteSpace(code)
        && code.Length <= 20
        && System.Text.RegularExpressions.Regex.IsMatch(code, "^[A-Z0-9_]+$");

    public static bool IsValidIanaTimeZone(string timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId)) return false;
        try
        {
            _ = TZConvert.GetTimeZoneInfo(timeZoneId.Trim());
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static bool AreValidWorkingDays(IReadOnlyList<string>? days)
    {
        if (days == null || days.Count == 0) return false;
        return days.All(d => OrganizationSettingsDefaults.Weekdays.Contains(d, StringComparer.Ordinal));
    }

    public static bool AreValidWorkingHours(string start, string end)
    {
        if (!TimeSpan.TryParse(start, out var startTime) || !TimeSpan.TryParse(end, out var endTime))
            return false;
        return startTime < endTime;
    }
}

public class UpdateOrganizationSettingsCommandValidator : AbstractValidator<UpdateOrganizationSettingsCommand>
{
    public UpdateOrganizationSettingsCommandValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();
        RuleFor(x => x.Request.OrganizationName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Request.OrganizationCode)
            .NotEmpty()
            .MaximumLength(20)
            .Must(code => OrganizationSettingsValidationRules.IsValidOrganizationCode(OrganizationCodeHelper.Normalize(code)))
            .WithMessage("Organization code must be uppercase letters, numbers, or underscores.");
        RuleFor(x => x.Request.TimeZoneId)
            .Must(OrganizationSettingsValidationRules.IsValidIanaTimeZone)
            .WithMessage("Time zone must be a valid IANA identifier.");
        RuleFor(x => x.Request.LanguageCode)
            .Must(code => OrganizationSettingsDefaults.SupportedLanguages.Contains(code))
            .WithMessage("Language is not supported.");
        RuleFor(x => x.Request.WorkingDays)
            .Must(OrganizationSettingsValidationRules.AreValidWorkingDays!)
            .WithMessage("Select at least one valid working day.");
        RuleFor(x => x.Request.WorkingHours)
            .Must(h => OrganizationSettingsValidationRules.AreValidWorkingHours(h.Start, h.End))
            .WithMessage("Working hours end time must be after start time.");
    }
}

public class UploadOrganizationLogoCommandValidator : AbstractValidator<UploadOrganizationLogoCommand>
{
    public UploadOrganizationLogoCommandValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();
        RuleFor(x => x.Length).GreaterThan(0);
        RuleFor(x => x.FileName).NotEmpty();
    }
}

public class RemoveOrganizationLogoCommandValidator : AbstractValidator<RemoveOrganizationLogoCommand>
{
    public RemoveOrganizationLogoCommandValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();
    }
}

public class GetOrganizationSettingsQueryValidator : AbstractValidator<GetOrganizationSettingsQuery>
{
    public GetOrganizationSettingsQueryValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();
    }
}
