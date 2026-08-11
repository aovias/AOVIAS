using Gateway.Application.Commands;
using Gateway.Application.Validators;
using Gateway.DTOs;
using Xunit;

namespace Gateway.UnitTests;

public sealed class OnboardWizardCommandValidatorTests
{
    private readonly OnboardWizardCommandValidator _validator = new();

    private static OnboardWizardRequestDto ValidRequest()
    {
        var deptId = Guid.NewGuid();
        return new OnboardWizardRequestDto
        {
            OrganizationId = Guid.NewGuid(),
            HospitalId = Guid.NewGuid(),
            HospitalProfile = new HospitalOnboardingProfileDto
            {
                HospitalId = Guid.Empty,
                DepartmentIds = new List<Guid> { deptId }
            },
            FrameworkCodes = new List<string> { "NABH" },
            TeamInvites = new List<TeamInviteClientDto>(),
            WizardVersion = 1
        };
    }

    [Fact]
    public void Empty_framework_codes_fails()
    {
        var req = ValidRequest();
        req.FrameworkCodes.Clear();
        var result = _validator.Validate(new OnboardWizardCommand(req));
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("framework", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Empty_department_ids_fails()
    {
        var req = ValidRequest();
        req.HospitalProfile.DepartmentIds.Clear();
        var result = _validator.Validate(new OnboardWizardCommand(req));
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("department", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Invite_department_not_in_profile_fails()
    {
        var req = ValidRequest();
        var otherDept = Guid.NewGuid();
        req.TeamInvites.Add(new TeamInviteClientDto
        {
            Name = "A",
            Email = "a@example.com",
            Role = "Member",
            DepartmentId = otherDept
        });
        var result = _validator.Validate(new OnboardWizardCommand(req));
        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            e => e.ErrorMessage.Contains("departments selected", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Empty_team_invites_passes()
    {
        var req = ValidRequest();
        req.TeamInvites.Clear();
        var result = _validator.Validate(new OnboardWizardCommand(req));
        Assert.True(result.IsValid);
    }
}
