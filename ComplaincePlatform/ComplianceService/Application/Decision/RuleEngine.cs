using System.Text.Json;
using ComplianceService.Application.Aggregation;
using ComplianceService.Application.Decision.Models;
using ComplianceService.Domain;
using ComplianceService.Domain.Policies;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Domain.Enums;

namespace ComplianceService.Application.Decision
{
    internal class RuleSet
    {
        public decimal MinOverallPercent { get; set; } = 80m;
        public int MinCoreOeScore { get; set; } = 4;
        public decimal MinChapterAverage { get; set; } = 4m;
        public decimal MinStandardAverage { get; set; } = 4m;
        public int MaxOeWithScoreLe2 { get; set; } = 1;
        public bool RequireActionPlansForLe3 { get; set; } = true;
    }

    public class RuleEngine : IRuleEngine
    {
        private readonly ComplianceDbContext _db;
        private readonly IAggregationService _aggregation;
        private readonly IOperationalModeClassificationPolicy _operationalModeClassification;

        public RuleEngine(
            ComplianceDbContext db,
            IAggregationService aggregation,
            IOperationalModeClassificationPolicy operationalModeClassification)
        {
            _db = db;
            _aggregation = aggregation;
            _operationalModeClassification = operationalModeClassification;
        }

        public async Task<DecisionResult> EvaluateAsync(Guid frameworkId, Guid journeyId, OperationalMode operationalMode, CancellationToken cancellationToken = default)
        {
            // Load active rules for framework+mode or use defaults
            var cfg = await _db.RuleConfigs
                .AsNoTracking()
                .Where(r => r.FrameworkId == frameworkId && r.IsActive && r.OperationalMode == operationalMode)
                .OrderByDescending(r => r.ValidFrom)
                .FirstOrDefaultAsync(cancellationToken);

            var ruleSet = cfg != null && !string.IsNullOrWhiteSpace(cfg.RulesJson)
                ? JsonSerializer.Deserialize<RuleSet>(cfg.RulesJson) ?? new RuleSet()
                : new RuleSet();

            // Copy for EF translation (Contains on classification set); policy returns shared static arrays.
            var included = _operationalModeClassification.GetClassificationsForDecisionRules(operationalMode).ToArray();

            var agg = await _aggregation.ComputeFrameworkAsync(frameworkId, journeyId, included, cancellationToken);
            var result = new DecisionResult
            {
                OverallCompliancePercent = agg.CompliancePercent
            };

            // Rule 1: Overall ≥ threshold
            var r1 = agg.CompliancePercent >= ruleSet.MinOverallPercent;
            result.Evaluations.Add(new RuleEvaluation { Rule = $"Overall ≥ {ruleSet.MinOverallPercent}%", Passed = r1, Detail = $"Overall={agg.CompliancePercent}%" });

            // Load all OEs and latest scores (used for core OE min score and low-score counts)
            var oeQuery = _db.ObjectiveElements
                .AsNoTracking()
                .Where(oe => oe.ComplianceStandard.Chapter.FrameworkId == frameworkId && included.Contains(oe.Classification));
            var oes = await oeQuery.Select(oe => new { oe.Id, oe.Classification }).ToListAsync(cancellationToken);
            var oeIds = oes.Select(x => x.Id).ToList();

            var scores = await _db.ObjectiveElementScores
                .AsNoTracking()
                .Where(s => s.JourneyId == journeyId && oeIds.Contains(s.ObjectiveElementId))
                .OrderBy(s => s.ObjectiveElementId)
                .ThenByDescending(s => s.AssessedAt)
                .ToListAsync(cancellationToken);
            var latest = scores.GroupBy(s => s.ObjectiveElementId).Select(g => g.First()).ToList();

            // Rule 2: Each Core OE score ≥ 4
            var coreOeIds = oes.Where(x => x.Classification == ObjectiveElementClassification.Core).Select(x => x.Id).ToHashSet();
            var allCoreOk = latest.Where(s => coreOeIds.Contains(s.ObjectiveElementId) && s.IsApplicable)
                                  .All(s => s.Score >= ruleSet.MinCoreOeScore);
            result.Evaluations.Add(new RuleEvaluation { Rule = $"All Core OEs ≥ {ruleSet.MinCoreOeScore}", Passed = allCoreOk });

            // Rule 3: Chapter average ≥ 4
            var chaptersOk = agg.Chapters.All(c => c.AverageScore >= ruleSet.MinChapterAverage);
            result.Evaluations.Add(new RuleEvaluation { Rule = $"Each Chapter avg ≥ {ruleSet.MinChapterAverage}", Passed = chaptersOk });

            // Rule 4: Standard average ≥ 4
            var standardsOk = agg.Chapters.SelectMany(c => c.Standards).All(s => s.AverageScore >= ruleSet.MinStandardAverage);
            result.Evaluations.Add(new RuleEvaluation { Rule = $"Each Standard avg ≥ {ruleSet.MinStandardAverage}", Passed = standardsOk });

            // Rule 5: Max 1 OE with score ≤ 2
            var low2Count = latest.Where(s => s.IsApplicable && s.Score <= 2).Count();
            var low2Ok = low2Count <= ruleSet.MaxOeWithScoreLe2;
            result.Evaluations.Add(new RuleEvaluation { Rule = $"Max {ruleSet.MaxOeWithScoreLe2} OE with score ≤ 2", Passed = low2Ok, Detail = $"Count={low2Count}" });

            // Rule 6: OE with score ≤ 3 must have action plan
            var low3Ids = latest.Where(s => s.IsApplicable && s.Score <= 3).Select(s => s.ObjectiveElementId).ToList();
            var plans = await _db.ActionPlans.AsNoTracking()
                .Where(ap => low3Ids.Contains(ap.ObjectiveElementId) && (ap.Status == ActionPlanStatus.Open || ap.Status == ActionPlanStatus.InProgress || ap.Status == ActionPlanStatus.Completed))
                .ToListAsync(cancellationToken);
            var hasAllPlans = !low3Ids.Except(plans.Select(p => p.ObjectiveElementId)).Any();
            var plansOk = !ruleSet.RequireActionPlansForLe3 || hasAllPlans;
            result.Evaluations.Add(new RuleEvaluation { Rule = "All OE ≤ 3 have Action Plans", Passed = plansOk, Detail = $"OE<=3={low3Ids.Count}, Plans={plans.Count}" });

            var allPassed = result.Evaluations.All(e => e.Passed);
            result.Outcome = allPassed ? "PASS" : "FAIL";

            return result;
        }
    }
}

