using ComplianceService.Application.Aggregation.Models;
using ComplianceService.Domain;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ComplianceService.Application.Aggregation
{
    public class AggregationService : IAggregationService
    {
        private readonly ComplianceDbContext _db;

        public AggregationService(ComplianceDbContext db)
        {
            _db = db;
        }

        public async Task<FrameworkAggregateResult> ComputeFrameworkAsync(
            Guid frameworkId,
            Guid journeyId,
            IReadOnlyCollection<ObjectiveElementClassification> includedClassifications,
            CancellationToken cancellationToken = default)
        {
            // Legacy overload: keep loading static catalog from DB for non-workspace callers.
            var chapters = await _db.ComplianceChapters
                .AsNoTracking()
                .Where(c => c.FrameworkId == frameworkId)
                .ToListAsync(cancellationToken);

            var chapterIds = chapters.Select(c => c.Id).ToList();

            var standards = await _db.ComplianceStandards
                .AsNoTracking()
                .Where(s => chapterIds.Contains(s.ChapterId))
                .ToListAsync(cancellationToken);

            var standardIds = standards.Select(s => s.Id).ToList();

            var objectiveElements = await _db.ObjectiveElements
                .AsNoTracking()
                .Where(oe => standardIds.Contains(oe.ComplianceStandardId)
                             && includedClassifications.Contains(oe.Classification))
                .ToListAsync(cancellationToken);

            return await ComputeFrameworkFromCatalogAsync(
                frameworkId,
                journeyId,
                chapters,
                standards,
                objectiveElements,
                cancellationToken);
        }

        public async Task<FrameworkAggregateResult> ComputeFrameworkAsync(
            Guid frameworkId,
            Guid journeyId,
            IReadOnlyList<ComplianceChapter> preloadedChapters,
            IReadOnlyList<ComplianceStandard> preloadedStandards,
            IReadOnlyList<ObjectiveElement> preloadedObjectiveElements,
            CancellationToken cancellationToken = default)
        {
            return await ComputeFrameworkFromCatalogAsync(
                frameworkId,
                journeyId,
                preloadedChapters,
                preloadedStandards,
                preloadedObjectiveElements,
                cancellationToken);
        }

        private async Task<FrameworkAggregateResult> ComputeFrameworkFromCatalogAsync(
            Guid frameworkId,
            Guid journeyId,
            IReadOnlyList<ComplianceChapter> preloadedChapters,
            IReadOnlyList<ComplianceStandard> preloadedStandards,
            IReadOnlyList<ObjectiveElement> preloadedObjectiveElements,
            CancellationToken cancellationToken)
        {
            var chapters = preloadedChapters.ToList();
            var standards = preloadedStandards.ToList();
            var objectiveElements = preloadedObjectiveElements.ToList();

            var oeIds = objectiveElements.Select(oe => oe.Id).ToList();

            // Get latest score per OE for this journey, excluding NA for aggregation
            var scores = await _db.ObjectiveElementScores
                .AsNoTracking()
                .Where(s => s.JourneyId == journeyId && oeIds.Contains(s.ObjectiveElementId))
                .OrderBy(s => s.ObjectiveElementId)
                .ThenByDescending(s => s.AssessedAt)
                .ToListAsync(cancellationToken);

            var latestByOe = scores
                .GroupBy(s => s.ObjectiveElementId)
                .Select(g => g.First())
                .ToDictionary(s => s.ObjectiveElementId, s => s);

            // Build standard aggregates
            var oesByStandard = objectiveElements.GroupBy(oe => oe.ComplianceStandardId);

            var standardResults = new List<ComplianceStandardAggregateResult>();

            foreach (var group in oesByStandard)
            {
                var standardId = group.Key;
                var oes = group.ToList();

                var included = new List<(decimal weight, int score)>();
                var naCount = 0;
                foreach (var oe in oes)
                {
                    if (!latestByOe.TryGetValue(oe.Id, out var sc))
                    {
                        // No score yet → exclude from numerator/denominator
                        continue;
                    }
                    if (!sc.IsApplicable)
                    {
                        naCount++;
                        continue;
                    }
                    included.Add((oe.Weight <= 0 ? 1.0m : oe.Weight, sc.Score));
                }

                var (avgScore, percent) = ComputeAverages(included);

                standardResults.Add(new ComplianceStandardAggregateResult
                {
                    ComplianceStandardId = standardId,
                    AverageScore = avgScore,
                    CompliancePercent = percent,
                    ObjectiveElementCount = oes.Count,
                    NaExcludedCount = naCount
                });
            }

            // Build chapter aggregates from standards
            var standardsByChapter = standards.GroupBy(s => s.ChapterId).ToDictionary(g => g.Key, g => g.ToList());
            var standardResultsById = standardResults.ToDictionary(s => s.ComplianceStandardId);

            var chapterResults = new List<ChapterAggregateResult>();
            foreach (var ch in chapters)
            {
                if (!standardsByChapter.TryGetValue(ch.Id, out var stList))
                {
                    chapterResults.Add(new ChapterAggregateResult
                    {
                        ChapterId = ch.Id,
                        AverageScore = 0,
                        CompliancePercent = 0,
                        StandardCount = 0,
                        NaExcludedCount = 0,
                        Standards = new List<ComplianceStandardAggregateResult>()
                    });
                    continue;
                }

                var childStandards = new List<ComplianceStandardAggregateResult>();
                var included = new List<(decimal weight, decimal score)>();
                var naSum = 0;
                foreach (var st in stList)
                {
                    if (standardResultsById.TryGetValue(st.Id, out var stAgg))
                    {
                        childStandards.Add(stAgg);
                        var weight = st.Weight <= 0 ? 1.0m : st.Weight;
                        included.Add((weight, stAgg.AverageScore));
                        naSum += stAgg.NaExcludedCount;
                    }
                }

                var (avgScore, percent) = ComputeAveragesDecimal(included);

                chapterResults.Add(new ChapterAggregateResult
                {
                    ChapterId = ch.Id,
                    AverageScore = avgScore,
                    CompliancePercent = percent,
                    StandardCount = childStandards.Count,
                    NaExcludedCount = naSum,
                    Standards = childStandards
                });
            }

            // Framework aggregate from chapters
            var includedCh = new List<(decimal weight, decimal score)>();
            var naTotal = 0;
            foreach (var ch in chapters)
            {
                var chAgg = chapterResults.FirstOrDefault(x => x.ChapterId == ch.Id);
                if (chAgg == null)
                {
                    continue;
                }
                var weight = ch.Weight <= 0 ? 1.0m : ch.Weight;
                includedCh.Add((weight, chAgg.AverageScore));
                naTotal += chAgg.NaExcludedCount;
            }
            var (fwAvg, fwPercent) = ComputeAveragesDecimal(includedCh);

            return new FrameworkAggregateResult
            {
                FrameworkId = frameworkId,
                AverageScore = fwAvg,
                CompliancePercent = fwPercent,
                ChapterCount = chapterResults.Count,
                NaExcludedCount = naTotal,
                Chapters = chapterResults
            };
        }

        private static (decimal avgScore, decimal percent) ComputeAverages(List<(decimal weight, int score)> included)
        {
            if (included.Count == 0)
            {
                return (0m, 0m);
            }
            decimal weightedSum = 0m;
            decimal weightTotal = 0m;
            foreach (var (w, s) in included)
            {
                weightedSum += w * s;
                weightTotal += w;
            }
            var avg = weightTotal == 0 ? 0m : weightedSum / weightTotal;
            // Percent relative to max 5
            var percent = avg <= 0 ? 0m : Math.Round((avg / 5m) * 100m, 2);
            return (Math.Round(avg, 2), percent);
        }

        private static (decimal avgScore, decimal percent) ComputeAveragesDecimal(List<(decimal weight, decimal score)> included)
        {
            if (included.Count == 0)
            {
                return (0m, 0m);
            }
            decimal weightedSum = 0m;
            decimal weightTotal = 0m;
            foreach (var (w, s) in included)
            {
                weightedSum += w * s;
                weightTotal += w;
            }
            var avg = weightTotal == 0 ? 0m : weightedSum / weightTotal;
            var percent = avg <= 0 ? 0m : Math.Round((avg / 5m) * 100m, 2);
            return (Math.Round(avg, 2), percent);
        }
    }
}

