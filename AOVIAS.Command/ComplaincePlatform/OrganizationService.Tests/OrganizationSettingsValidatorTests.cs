using FluentValidation.TestHelper;
using OrganizationService.Application.Commands;
using OrganizationService.Application.DTOs;
using OrganizationService.Application.Validators;
using OrganizationService.Domain.Entities;
using Xunit;

namespace OrganizationService.Tests;

public class OrganizationSettingsValidatorTests
{
    private readonly UpdateOrganizationSettingsCommandValidator _validator = new();

    private static UpdateOrganizationSettingsRequest ValidRequest() => new()
    {
        OrganizationName = "Acme Hospital",
        OrganizationCode = "ACME_HOSP",
        TimeZoneId = "Asia/Kolkata",
        LanguageCode = "en",
        WorkingDays = ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday"],
        WorkingHours = new WorkingHoursDto { Start = "09:00", End = "18:00" }
    };

    [Fact]
    public void Valid_request_passes()
    {
        var result = _validator.TestValidate(new UpdateOrganizationSettingsCommand(Guid.NewGuid(), ValidRequest(), Guid.NewGuid()));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Missing_name_fails()
    {
        var request = ValidRequest();
        request.OrganizationName = "";
        var result = _validator.TestValidate(new UpdateOrganizationSettingsCommand(Guid.NewGuid(), request, Guid.NewGuid()));
        result.ShouldHaveValidationErrorFor(x => x.Request.OrganizationName);
    }

    [Fact]
    public void Invalid_code_format_fails()
    {
        var request = ValidRequest();
        request.OrganizationCode = "bad-code";
        var result = _validator.TestValidate(new UpdateOrganizationSettingsCommand(Guid.NewGuid(), request, Guid.NewGuid()));
        result.ShouldHaveValidationErrorFor(x => x.Request.OrganizationCode);
    }

    [Fact]
    public void Invalid_timezone_fails()
    {
        var request = ValidRequest();
        request.TimeZoneId = "Not/AZone";
        var result = _validator.TestValidate(new UpdateOrganizationSettingsCommand(Guid.NewGuid(), request, Guid.NewGuid()));
        result.ShouldHaveValidationErrorFor(x => x.Request.TimeZoneId);
    }

    [Fact]
    public void Invalid_working_hours_fails()
    {
        var request = ValidRequest();
        request.WorkingHours = new WorkingHoursDto { Start = "18:00", End = "09:00" };
        var result = _validator.TestValidate(new UpdateOrganizationSettingsCommand(Guid.NewGuid(), request, Guid.NewGuid()));
        result.ShouldHaveValidationErrorFor(x => x.Request.WorkingHours);
    }

    [Fact]
    public void Empty_working_days_fails()
    {
        var request = ValidRequest();
        request.WorkingDays = [];
        var result = _validator.TestValidate(new UpdateOrganizationSettingsCommand(Guid.NewGuid(), request, Guid.NewGuid()));
        result.ShouldHaveValidationErrorFor(x => x.Request.WorkingDays);
    }

    [Theory]
    [InlineData("Monday")]
    [InlineData("Sunday")]
    public void Working_day_validation_accepts_weekdays(string day)
    {
        Assert.True(OrganizationSettingsValidationRules.AreValidWorkingDays([day]));
    }

    [Fact]
    public void Organization_code_helper_normalizes_uppercase()
    {
        Assert.Equal("ACME", OrganizationCodeHelper.Normalize("acme"));
    }
}
