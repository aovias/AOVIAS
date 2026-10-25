using ComplianceService.Application.Commands;
using ComplianceService.Application.Contracts;
using ComplianceService.Application.DTOs;
using ComplianceService.Domain.Enums;
using ComplianceService.Infrastructure.Persistence;
using ComplianceService.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ComplianceService.Application.Services;

/// <summary>
/// Bridges onboarding wizard assessment answers into obligation state and deficiency evaluation.
/// </summary>
public sealed class OnboardingObligationAssessmentService : IOnboardingObligationAssessmentService
{
    private readonly ComplianceDbContext _db;
    private readonly WizardOnboardingAssessmentService _wizardAssessmentService;
    private readonly IObligationSynchronizationService _obligationSync;
    private readonly UpdateObligationFromAssessmentCommandHandler _assessmentHandler;
    private readonly EvaluateObligationDeficienciesCommandHandler _evaluateDeficienciesHandler;
    private readonly IKnowledgeBaseServiceClient _kbClient;
    private readonly ILogger<OnboardingObligationAssessmentService> _logger;

    public OnboardingObligationAssessmentService(
        ComplianceDbContext db,
        WizardOnboardingAssessmentService wizardAssessmentService,
        IObligationSynchronizationService obligationSync,
        UpdateObligationFromAssessmentCommandHandler assessmentHandler,
        EvaluateObligationDeficienciesCommandHandler evaluateDeficienciesHandler,
        IKnowledgeBaseServiceClient kbClient,
        ILogger<OnboardingObligationAssessmentService> logger)
    {
        _db = db;
        _wizardAssessmentService = wizardAssessmentService;
        _obligationSync = obligationSync;
        _assessmentHandler = assessmentHandler;
        _evaluateDeficienciesHandler = evaluateDeficienciesHandler;
        _kbClient = kbClient;
        _logger = logger;
    }

    public async Task<int> ApplyAsync(
        Guid organizationId,
        Guid hospitalId,
        Guid frameworkId,
        IReadOnlyList<WizardAssessmentAnswerItemDto>? answers = null,
        CancellationToken ct = default)
    {
        var resolvedAnswers = answers?.ToList() ?? await LoadLatestAnswersAsync(hospitalId, ct);
        if (resolvedAnswers.Count == 0)
        {
            _logger.LogDebug(
                "OnboardingObligationApply: No assessment answers for HospitalId={HospitalId}. Skipping.",
                hospitalId);
            return 0;
        }

        await _obligationSync.SyncForOrganizationAsync(
            organizationId, hospitalId, organizationId, frameworkId, ct);

        // Build a fallback map of KB ComplianceNode.Id → OE Code for answers that do not
        // carry an ObjectiveElementCode (legacy data saved before this field was added).
        // A single HTTP call fetches all OE nodes for the framework; failures are non-fatal.
        var legacyNodeCodeMap = await BuildNodeCodeMapAsync(resolvedAnswers, frameworkId, ct);

        var assessedAt = DateTime.UtcNow;
        var updatedObligationIds = new List<Guid>();

        foreach (var answer in resolvedAnswers)
        {
            if (!Guid.TryParse(answer.QuestionId, out var oeId))
            {
                continue;
            }

            var complianceStatus = NormalizeAnswer(answer.Answer);
            if (complianceStatus == null)
            {
                continue;
            }

            // Prefer the code explicitly provided by the caller (new data path).
            // Fall back to the runtime-resolved map for legacy answers that lacked a code.
            var oeCode = answer.ObjectiveElementCode;
            if (string.IsNullOrWhiteSpace(oeCode) && legacyNodeCodeMap.TryGetValue(oeId, out var resolvedCode))
            {
                oeCode = resolvedCode;
            }

            var obligationId = await _assessmentHandler.HandleAsync(
                new UpdateObligationFromAssessmentCommand(
                    organizationId,
                    oeId,
                    complianceStatus,
                    assessedAt,
                    "onboarding-assessment")
                {
                    EvaluateDeficiencies = false,
                    ObjectiveElementCode = oeCode,
                },
                ct);

            if (obligationId.HasValue)
            {
                updatedObligationIds.Add(obligationId.Value);
            }
        }

        // Batch deficiency evaluation — single outbox flush at the end.
        foreach (var obligationId in updatedObligationIds)
        {
            await _evaluateDeficienciesHandler.HandleAsync(
                new EvaluateObligationDeficienciesCommand(
                    obligationId,
                    hospitalId,
                    frameworkId,
                    "OnboardingAssessment",
                    FlushOutbox: false),
                ct);
        }

        if (updatedObligationIds.Count > 0)
        {
            await _evaluateDeficienciesHandler.FlushOutboxAsync(ct);
        }

        _logger.LogInformation(
            "OnboardingObligationApply: Applied {Count}/{Total} assessment answers for HospitalId={HospitalId} FrameworkId={FrameworkId}.",
            updatedObligationIds.Count, resolvedAnswers.Count, hospitalId, frameworkId);

        return updatedObligationIds.Count;
    }

    public async Task ApplyIfPendingAsync(
        Guid hospitalId,
        Guid frameworkId,
        CancellationToken ct = default)
    {
        var wizard = await _wizardAssessmentService.GetLatestForHospitalAsync(hospitalId, ct);
        if (wizard?.AssessmentAnswers == null || wizard.AssessmentAnswers.Count == 0)
        {
            return;
        }

        var oeIds = wizard.AssessmentAnswers
            .Select(a => a.QuestionId)
            .Where(id => Guid.TryParse(id, out _))
            .Select(Guid.Parse)
            .ToList();

        if (oeIds.Count == 0)
        {
            return;
        }

        // Skip when any answered OE for this framework already has assessment data.
        var hasAssessed = await _db.OrganizationComplianceObligations
            .IgnoreQueryFilters()
            .AnyAsync(
                o => o.HospitalId == hospitalId
                  && o.FrameworkId == frameworkId
                  && oeIds.Contains(o.ObjectiveElementId)
                  && o.AssessmentStatus != ObligationAssessmentStatus.NotAssessed,
                ct);

        if (hasAssessed)
        {
            return;
        }

        await ApplyAsync(
            wizard.OrganizationId,
            hospitalId,
            frameworkId,
            wizard.AssessmentAnswers,
            ct);
    }

    /// <summary>
    /// Builds a KB ComplianceNode.Id → OE Code map by calling KnowledgeBase Service once.
    /// Only invoked when at least one answer is missing an <see cref="WizardAssessmentAnswerItemDto.ObjectiveElementCode"/>
    /// (i.e., the answer was saved before the code field was introduced).
    /// </summary>
    private async Task<IReadOnlyDictionary<Guid, string>> BuildNodeCodeMapAsync(
        IReadOnlyList<WizardAssessmentAnswerItemDto> answers,
        Guid frameworkId,
        CancellationToken ct)
    {
        var needsResolution = answers.Any(a => string.IsNullOrWhiteSpace(a.ObjectiveElementCode)
                                            && Guid.TryParse(a.QuestionId, out _));
        if (!needsResolution)
            return new Dictionary<Guid, string>();

        _logger.LogDebug(
            "OnboardingObligationApply: Resolving KB node codes for FrameworkId={FrameworkId} (legacy answers without ObjectiveElementCode).",
            frameworkId);

        try
        {
            return await _kbClient.GetObjectiveElementNodeCodesAsync(frameworkId, ct);
        }
        catch (Exception ex)
        {
            // Documented as non-fatal: missing codes only skip legacy answer mapping.
            _logger.LogWarning(
                ex,
                "OnboardingObligationApply: KB node-code lookup failed for FrameworkId={FrameworkId}; continuing without legacy codes.",
                frameworkId);
            return new Dictionary<Guid, string>();
        }
    }

    private async Task<List<WizardAssessmentAnswerItemDto>> LoadLatestAnswersAsync(
        Guid hospitalId,
        CancellationToken ct)
    {
        var wizard = await _wizardAssessmentService.GetLatestForHospitalAsync(hospitalId, ct);
        return wizard?.AssessmentAnswers?.ToList() ?? new List<WizardAssessmentAnswerItemDto>();
    }

    /// <summary>
    /// Normalizes wizard answer vocabulary to the compliance-status strings expected by
    /// <see cref="UpdateObligationFromAssessmentCommandHandler"/>.
    /// </summary>
    internal static string? NormalizeAnswer(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        return raw.Trim().ToLowerInvariant() switch
        {
            "compliant" or "yes" => "compliant",
            "partially_compliant" or "partial" or "not_sure" => "partially_compliant",
            "non_compliant" or "no" => "non_compliant",
            "not_applicable" => "not_applicable",
            _ => null,
        };
    }
}
