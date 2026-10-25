using FluentValidation;
using Gateway.Application.Commands;
using Gateway.Application.Queries;

namespace Gateway.Application.Validators;

/// <summary>
/// Validator for GetRegulatoryOverviewQuery.
/// </summary>
public class GetRegulatoryOverviewQueryValidator : AbstractValidator<GetRegulatoryOverviewQuery>
{
    public GetRegulatoryOverviewQueryValidator()
    {
        RuleFor(x => x.OrgId)
            .NotEmpty()
            .WithMessage("OrganizationId is required.");
    }
}

/// <summary>
/// Validator for UpdateJurisdictionCommand.
/// Enforces that country is provided; semantic rules are in ComplianceService.
/// </summary>
public class UpdateJurisdictionCommandValidator : AbstractValidator<UpdateJurisdictionCommand>
{
    public UpdateJurisdictionCommandValidator()
    {
        RuleFor(x => x.OrgId)
            .NotEmpty()
            .WithMessage("OrganizationId is required.");

        RuleFor(x => x.Request)
            .NotNull()
            .WithMessage("Request body is required.");

        RuleFor(x => x.Request.Country)
            .NotEmpty()
            .WithMessage("Country is required.");
    }
}

/// <summary>
/// Validator for GetAccreditationsQuery.
/// </summary>
public class GetAccreditationsQueryValidator : AbstractValidator<GetAccreditationsQuery>
{
    public GetAccreditationsQueryValidator()
    {
        RuleFor(x => x.OrgId)
            .NotEmpty()
            .WithMessage("OrganizationId is required.");
    }
}

/// <summary>
/// Validator for GetRenewalsQuery.
/// </summary>
public class GetRenewalsQueryValidator : AbstractValidator<GetRenewalsQuery>
{
    public GetRenewalsQueryValidator()
    {
        RuleFor(x => x.OrgId)
            .NotEmpty()
            .WithMessage("OrganizationId is required.");

        RuleFor(x => x.WithinDays)
            .GreaterThan(0)
            .WithMessage("withinDays must be greater than zero.")
            .LessThanOrEqualTo(365)
            .WithMessage("withinDays cannot be greater than 365.");
    }
}

/// <summary>
/// Validator for GetFrameworkCatalogQuery.
/// </summary>
public class GetFrameworkCatalogQueryValidator : AbstractValidator<GetFrameworkCatalogQuery>
{
    public GetFrameworkCatalogQueryValidator()
    {
        RuleFor(x => x.OrgId)
            .NotEmpty()
            .WithMessage("OrganizationId is required.");

        RuleFor(x => x.Page)
            .GreaterThan(0)
            .WithMessage("Page must be greater than zero.");

        // Enforce maximum pageSize to prevent performance degradation
        // Server-side cap: 100 items per page maximum
        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("PageSize must be between 1 and 100. Large page sizes can cause performance issues.");
    }
}

/// <summary>
/// Validator for GetActivationMetadataQuery.
/// </summary>
public class GetActivationMetadataQueryValidator : AbstractValidator<GetActivationMetadataQuery>
{
    public GetActivationMetadataQueryValidator()
    {
        RuleFor(x => x.OrgId)
            .NotEmpty()
            .WithMessage("OrganizationId is required.");

        RuleFor(x => x.FrameworkId)
            .NotEmpty()
            .WithMessage("FrameworkId is required.");
    }
}

/// <summary>
/// Validator for ActivateAccreditationCommand.
/// </summary>
public class ActivateAccreditationCommandValidator : AbstractValidator<ActivateAccreditationCommand>
{
    public ActivateAccreditationCommandValidator()
    {
        RuleFor(x => x.OrgId)
            .NotEmpty()
            .WithMessage("OrganizationId is required.");

        RuleFor(x => x.Request)
            .NotNull()
            .WithMessage("Request body is required.");

        RuleFor(x => x.Request.FrameworkId)
            .NotEmpty()
            .WithMessage("FrameworkId is required.");

        RuleFor(x => x.Request.ComplianceOfficer)
            .NotEmpty()
            .WithMessage("ComplianceOfficer is required.");

        RuleFor(x => x.Request.Departments)
            .NotNull()
            .WithMessage("Scope departments are required.")
            .Must(d => d != null && d.Any())
            .WithMessage("At least one department must be provided in scope.");
    }
}

/// <summary>
/// Validator for SuspendAccreditationCommand.
/// Enforces optimistic concurrency control via If-Match header (RowVersion/ETag).
/// Includes lifecycle transition guard to fail-fast on obviously invalid transitions.
/// </summary>
public class SuspendAccreditationCommandValidator : AbstractValidator<SuspendAccreditationCommand>
{
    public SuspendAccreditationCommandValidator()
    {
        RuleFor(x => x.OrgId)
            .NotEmpty()
            .WithMessage("OrganizationId is required.");

        RuleFor(x => x.AccreditationId)
            .NotEmpty()
            .WithMessage("AccreditationId is required.");

        RuleFor(x => x.Request)
            .NotNull()
            .WithMessage("Request body is required.");

        RuleFor(x => x.Request.Reason)
            .NotEmpty()
            .WithMessage("Reason is required to suspend an accreditation.");

        // Require If-Match header for optimistic concurrency control
        // This prevents race conditions when multiple admins modify the same accreditation
        RuleFor(x => x.IfMatch)
            .NotEmpty()
            .WithMessage("If-Match header (RowVersion/ETag) is required for optimistic concurrency control. Include the RowVersion from the last GET request to prevent race conditions.");

        // Lifecycle transition guard: fail-fast if client indicates invalid current status
        // This prevents unnecessary calls to ComplianceService for obviously invalid transitions.
        // Source of truth remains ComplianceService - this is a client-side optimization.
        When(x => !string.IsNullOrWhiteSpace(x.Request.CurrentStatus), () =>
        {
            RuleFor(x => x.Request.CurrentStatus!)
                .Must(s => !string.Equals(s, "Suspended", StringComparison.OrdinalIgnoreCase))
                .WithMessage("Accreditation is already suspended. Suspend operation is not allowed on a Suspended accreditation.")
                .Must(s => !string.Equals(s, "Archived", StringComparison.OrdinalIgnoreCase))
                .WithMessage("Accreditation is already archived. Suspend operation is not allowed on an Archived accreditation.");
        });
    }
}

/// <summary>
/// Validator for ArchiveAccreditationCommand.
/// Enforces optimistic concurrency control via If-Match header (RowVersion/ETag).
/// Includes lifecycle transition guard to fail-fast on obviously invalid transitions.
/// </summary>
public class ArchiveAccreditationCommandValidator : AbstractValidator<ArchiveAccreditationCommand>
{
    public ArchiveAccreditationCommandValidator()
    {
        RuleFor(x => x.OrgId)
            .NotEmpty()
            .WithMessage("OrganizationId is required.");

        RuleFor(x => x.AccreditationId)
            .NotEmpty()
            .WithMessage("AccreditationId is required.");

        // Require If-Match header for optimistic concurrency control
        // This prevents race conditions when multiple admins modify the same accreditation
        RuleFor(x => x.IfMatch)
            .NotEmpty()
            .WithMessage("If-Match header (RowVersion/ETag) is required for optimistic concurrency control. Include the RowVersion from the last GET request to prevent race conditions.");

        // Lifecycle transition guard: fail-fast if client indicates invalid current status
        // This prevents unnecessary calls to ComplianceService for obviously invalid transitions.
        // Source of truth remains ComplianceService - this is a client-side optimization.
        When(x => !string.IsNullOrWhiteSpace(x.CurrentStatus), () =>
        {
            RuleFor(x => x.CurrentStatus!)
                .Must(s => !string.Equals(s, "Archived", StringComparison.OrdinalIgnoreCase))
                .WithMessage("Accreditation is already archived. Archive operation is not allowed on an already Archived accreditation.");
        });
    }
}

/// <summary>
/// Validator for RecalculateRiskCommand.
/// </summary>
public class RecalculateRiskCommandValidator : AbstractValidator<RecalculateRiskCommand>
{
    public RecalculateRiskCommandValidator()
    {
        RuleFor(x => x.OrgId)
            .NotEmpty()
            .WithMessage("OrganizationId is required.");
    }
}

