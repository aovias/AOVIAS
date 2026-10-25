using KnowledgeBase.Application.AssessmentSummary;

using KnowledgeBase.Application.Contracts;

using KnowledgeBase.Application.DTOs;

using KnowledgeBase.Domain.Entities;

using KnowledgeBase.Domain.Services;



namespace KnowledgeBase.Application.Services;




public sealed class AssessmentAppService

{

    private readonly IAssessmentRepository _assessments;

    private readonly INodeRepository _nodes;

    private readonly IRequirementRepository _requirements;

    private readonly AssessmentContextBuilder _contextBuilder;

    private readonly IAssessmentSummaryCalculator _calculator;

    private readonly MaturityProgressionService _progressionService;



    public AssessmentAppService(

        IAssessmentRepository assessments,

        INodeRepository nodes,

        IRequirementRepository requirements,

        AssessmentContextBuilder contextBuilder,

        IAssessmentSummaryCalculator calculator,

        MaturityProgressionService progressionService)

    {

        _assessments = assessments;

        _nodes = nodes;

        _requirements = requirements;

        _contextBuilder = contextBuilder;

        _calculator = calculator;

        _progressionService = progressionService;

    }



    public async Task<AssessmentDto> CreateAsync(CreateAssessmentRequestDto dto, CancellationToken cancellationToken = default)

    {

        var a = new Assessment

        {

            Id = Guid.NewGuid(),

            FacilityId = dto.FacilityId,

            FrameworkId = dto.FrameworkId,

            Status = "Open",

            CreatedAt = DateTime.UtcNow

        };

        await _assessments.AddAsync(a, cancellationToken);

        await _assessments.SaveChangesAsync(cancellationToken);

        return new AssessmentDto(a.Id, a.FacilityId, a.FrameworkId, a.Status, a.CreatedAt);

    }



    public async Task<SubmitWizardAssessmentResponseDto> SubmitWizardAnswersAsync(

        SubmitWizardAssessmentRequestDto request,

        CancellationToken cancellationToken = default)

    {

        if (request.FacilityId == Guid.Empty || request.FrameworkId == Guid.Empty)

            throw new ArgumentException("FacilityId and FrameworkId are required.");



        if (request.Answers.Count == 0)

            throw new ArgumentException("At least one answer is required.");



        var startOfUtcDay = DateTime.UtcNow.Date;

        var existing = await _assessments.GetLatestForFacilityFrameworkSinceAsync(

            request.FacilityId,

            request.FrameworkId,

            startOfUtcDay,

            cancellationToken);



        Guid assessmentId;

        if (existing is not null)

        {

            assessmentId = existing.Id;

        }

        else

        {

            var created = await CreateAsync(

                new CreateAssessmentRequestDto(request.FacilityId, request.FrameworkId),

                cancellationToken);

            assessmentId = created.Id;

        }



        var frameworkNodes = await _nodes.ListByFrameworkAsync(request.FrameworkId, cancellationToken);

        var nodeIds = frameworkNodes.Select(n => n.Id).ToHashSet();



        foreach (var answer in request.Answers)

        {

            if (!Guid.TryParse(answer.QuestionId, out var nodeId) || !nodeIds.Contains(nodeId))

                continue;



            var itemDto = WizardAssessmentAnswerMapper.MapToUpdateItemDto(answer);

            await ApplyItemUpdateAsync(assessmentId, nodeId, itemDto, cancellationToken);

        }



        await _assessments.SaveChangesAsync(cancellationToken);



        var summary = await GetSummaryAsync(assessmentId, cancellationToken);

        return new SubmitWizardAssessmentResponseDto(assessmentId, summary);

    }



    public async Task UpsertItemAsync(Guid assessmentId, Guid nodeId, UpdateAssessmentItemRequestDto dto, CancellationToken cancellationToken = default)

    {

        await ApplyItemUpdateAsync(assessmentId, nodeId, dto, cancellationToken);

        await _assessments.SaveChangesAsync(cancellationToken);

    }



    public async Task<AssessmentSummaryResponseDto> GetSummaryAsync(Guid assessmentId, CancellationToken cancellationToken = default)

    {

        var context = await _contextBuilder.BuildAsync(assessmentId, cancellationToken);

        var result = _calculator.Calculate(context.ToSummaryInput());

        return new AssessmentSummaryResponseDto(

            result.OverallPercent,

            result.MeetsOverallPercent,

            result.CoreRulePassed,

            result.CoreFailureNodeIds,

            result.ChapterAverages,

            result.StandardAverages,

            result.ChapterAveragesPassed,

            result.StandardAveragesPassed,

            result.CommitmentLevelPassed,

            result.TooManyLowScoresInStandard,

            result.StandardsWithMultipleLowScores,

            result.ActionPlanViolationNodeIds);

    }



    /// <summary>
    /// Processes a Core wizard submission: stores assessment items using ObjectiveElementId as NodeId,
    /// runs the Core scoring formula, evaluates maturity progression eligibility, and returns the summary.
    /// The existing 1–5 scale path (SubmitWizardAnswersAsync) is not modified.
    /// </summary>
    public async Task<SubmitCoreWizardResponseDto> SubmitCoreWizardAnswersAsync(
        SubmitCoreWizardRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (request.FacilityId == Guid.Empty || request.FrameworkId == Guid.Empty)
            throw new ArgumentException("FacilityId and FrameworkId are required.");

        if (request.Answers.Count == 0)
            throw new ArgumentException("At least one answer is required.");

        // Reuse or create today's assessment
        var startOfDay = DateTime.UtcNow.Date;
        var existing = await _assessments.GetLatestForFacilityFrameworkSinceAsync(
            request.FacilityId, request.FrameworkId, startOfDay, cancellationToken);

        Guid assessmentId;
        if (existing is not null)
        {
            assessmentId = existing.Id;
        }
        else
        {
            var created = await CreateAsync(
                new CreateAssessmentRequestDto(request.FacilityId, request.FrameworkId), cancellationToken);
            assessmentId = created.Id;
        }

        // Persist each answer as an AssessmentItem (ComplianceStatus + score on 0/50/100 scale)
        foreach (var answer in request.Answers)
        {
            var score = CoreAnswerMapper.GetScore(answer.Answer) ?? 0m;
            var complianceStatus = answer.Answer switch
            {
                CoreAnswerMapper.Compliant          => "Compliant",
                CoreAnswerMapper.PartiallyCompliant  => "PartiallyCompliant",
                CoreAnswerMapper.NonCompliant        => "NonCompliant",
                CoreAnswerMapper.NotApplicable       => "NotApplicable",
                _                                    => "NotApplicable"
            };

            var item = await _assessments.GetItemAsync(assessmentId, answer.ObjectiveElementId, cancellationToken)
                       ?? new AssessmentItem { Id = Guid.NewGuid(), AssessmentId = assessmentId, NodeId = answer.ObjectiveElementId };

            item.ComplianceStatus = complianceStatus;
            item.Score = (int)Math.Round(score / 20m);  // map 0/50/100 → 0/3/5 for legacy field
            item.HasActionPlan = score == 0m;            // flag non-compliant for action plan tracking
            item.EvaluatedAt = DateTime.UtcNow;

            await _assessments.UpsertItemAsync(item, cancellationToken);
        }

        await _assessments.SaveChangesAsync(cancellationToken);

        // Score using Core formula
        var summaryInput = new CoreAssessmentSummaryInput { Answers = request.Answers };
        var result = CoreAssessmentCalculator.Calculate(summaryInput);

        // Evaluate progression
        var progressionResult = _progressionService.EvaluateProgression(
            "Core", result.CoreReadinessScore, result.CriticalFailureCount);

        var highRiskAreaNames = result.HighRiskAreas.Select(h => h.Category).ToList();

        var gapSummary = new CoreGapSummaryDto(
            CoreReadinessScore:            result.CoreReadinessScore,
            Status:                        result.Status,
            CriticalFailureCount:          result.CriticalFailureCount,
            HighRiskAreas:                 result.HighRiskAreas,
            RecommendedNextActions:        result.RecommendedNextActions,
            CommitmentAssessmentEligible:  progressionResult.CanProgress,
            BlockingReason:                progressionResult.BlockingReason);

        return new SubmitCoreWizardResponseDto
        {
            AssessmentId         = assessmentId,
            CoreReadinessScore   = result.CoreReadinessScore,
            Status               = result.Status,
            CriticalFailureCount = result.CriticalFailureCount,
            CanUnlockCommitment  = progressionResult.CanProgress,
            HighRiskAreas        = highRiskAreaNames,
            GapSummary           = gapSummary
        };
    }

    private async Task ApplyItemUpdateAsync(

        Guid assessmentId,

        Guid nodeId,

        UpdateAssessmentItemRequestDto dto,

        CancellationToken cancellationToken)

    {

        _ = await _assessments.GetByIdAsync(assessmentId, cancellationToken)

            ?? throw new InvalidOperationException("Assessment not found.");



        _ = await _nodes.GetByIdAsync(nodeId, cancellationToken)

            ?? throw new InvalidOperationException("Node not found.");



        var req = await _requirements.GetByNodeAsync(nodeId, cancellationToken);

        var raw = dto.Score;

        var adjusted = req is { RequiresDocumentation: true }

            ? DocumentationScoreAdjuster.Apply(raw, true, dto.DocumentationAdequate)

            : raw;



        // Onboarding wizard sets HasActionPlan for low/uncertain scores (no action-plan UI).

        if (adjusted <= 3 && !dto.HasActionPlan)

            throw new InvalidOperationException("Action plan is required when score is 3 or below.");



        var existing = await _assessments.GetItemAsync(assessmentId, nodeId, cancellationToken);

        var item = existing ?? new AssessmentItem { Id = Guid.NewGuid(), AssessmentId = assessmentId, NodeId = nodeId };

        item.ComplianceStatus = dto.ComplianceStatus;

        item.Score = adjusted;

        item.Remarks = dto.Remarks;

        item.HasActionPlan = dto.HasActionPlan;

        item.EvaluatedAt = DateTime.UtcNow;



        await _assessments.UpsertItemAsync(item, cancellationToken);

    }

}


