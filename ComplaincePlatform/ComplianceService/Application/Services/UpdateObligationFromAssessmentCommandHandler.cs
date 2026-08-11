using ComplianceService.Application.Commands;
using ComplianceService.Application.Contracts;
using ComplianceService.Domain.Enums;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ComplianceService.Application.Services;

/// <summary>
/// Updates an OrganizationComplianceObligation from a KnowledgeBase assessment result.
///
/// Mapping (CoreAnswerMapper vocabulary):
///   compliant           → ImplementationStatus = Implemented,          AssessmentStatus = Passed
///   partially_compliant → ImplementationStatus = PartiallyImplemented, AssessmentStatus = PartiallyPassed
///   non_compliant       → ImplementationStatus = NotImplemented,       AssessmentStatus = Failed
///   not_applicable      → no update (obligation state unchanged)
///
/// Assessment fields are written here; all derived operational fields are recomputed by
/// <see cref="IObligationProjectionService"/>.
/// </summary>
public class UpdateObligationFromAssessmentCommandHandler
{
    private readonly ComplianceDbContext _db;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IObligationProjectionService _projectionService;
    private readonly ILogger<UpdateObligationFromAssessmentCommandHandler> _logger;

    public UpdateObligationFromAssessmentCommandHandler(
        ComplianceDbContext db,
        IUnitOfWork unitOfWork,
        IObligationProjectionService projectionService,
        ILogger<UpdateObligationFromAssessmentCommandHandler> logger)
    {
        _db = db;
        _unitOfWork = unitOfWork;
        _projectionService = projectionService;
        _logger = logger;
    }

    public async Task<Guid?> HandleAsync(
        UpdateObligationFromAssessmentCommand command,
        CancellationToken ct = default)
    {
        var (implementationStatus, assessmentStatus, shouldUpdate) =
            MapComplianceStatus(command.ComplianceStatus);

        if (!shouldUpdate)
        {
            _logger.LogDebug(
                "ObligationAssessmentUpdate: Skipped for OeId={OeId} — status '{Status}' requires no change.",
                command.ObjectiveElementId, command.ComplianceStatus);
            return null;
        }

        var obligation = await _db.OrganizationComplianceObligations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                o => o.OrganizationId == command.OrganizationId
                  && o.ObjectiveElementId == command.ObjectiveElementId,
                ct);

        if (obligation == null && !string.IsNullOrWhiteSpace(command.ObjectiveElementCode))
        {
            obligation = await _db.OrganizationComplianceObligations
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(
                    o => o.OrganizationId == command.OrganizationId
                      && o.Code == command.ObjectiveElementCode,
                    ct);

            if (obligation != null)
            {
                _logger.LogDebug(
                    "ObligationAssessmentUpdate: Resolved obligation via Code='{Code}' for OrgId={OrgId} (GUID lookup missed — KB cross-domain ID).",
                    command.ObjectiveElementCode, command.OrganizationId);
            }
        }

        if (obligation == null)
        {
            _logger.LogWarning(
                "ObligationAssessmentUpdate: No obligation found for OrganizationId={OrgId} OeId={OeId} OeCode={OeCode}. " +
                "Run SyncObligationsForOrganization first.",
                command.OrganizationId, command.ObjectiveElementId, command.ObjectiveElementCode);
            return null;
        }

        obligation.ImplementationStatus = implementationStatus;
        obligation.AssessmentStatus = assessmentStatus;
        obligation.LastAssessmentDate = command.AssessedAt;
        obligation.UpdatedAt = DateTime.UtcNow;
        obligation.UpdatedBy = command.AssessedBy ?? "assessment-engine";

        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation(
            "ObligationAssessmentUpdate: Updated obligation {ObligationId} → Implementation={Impl} Assessment={Assess}",
            obligation.Id, implementationStatus, assessmentStatus);

        await _projectionService.ProjectAsync(
            new ObligationProjectionRequest(
                obligation.Id,
                ObligationProjectionTriggers.AssessmentCompleted,
                EvaluateDeficiencies: command.EvaluateDeficiencies),
            ct);

        return obligation.Id;
    }

    private static (ImplementationStatus impl, ObligationAssessmentStatus assess, bool shouldUpdate)
        MapComplianceStatus(string complianceStatus) =>
        complianceStatus?.ToLowerInvariant() switch
        {
            "compliant"           => (ImplementationStatus.Implemented,          ObligationAssessmentStatus.Passed,          true),
            "partially_compliant" => (ImplementationStatus.PartiallyImplemented, ObligationAssessmentStatus.PartiallyPassed, true),
            "non_compliant"       => (ImplementationStatus.NotImplemented,       ObligationAssessmentStatus.Failed,          true),
            _                     => (ImplementationStatus.Unknown,              ObligationAssessmentStatus.NotAssessed,     false)
        };
}
