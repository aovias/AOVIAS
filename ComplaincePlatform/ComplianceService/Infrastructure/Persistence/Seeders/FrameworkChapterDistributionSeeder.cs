using ComplianceService.Domain;
using Microsoft.EntityFrameworkCore;

namespace ComplianceService.Infrastructure.Persistence.Seeders
{
    public class FrameworkChapterDistributionSeeder
    {
        private readonly ComplianceDbContext _db;
        private readonly ILogger<FrameworkChapterDistributionSeeder> _logger;

        public FrameworkChapterDistributionSeeder(ComplianceDbContext db, ILogger<FrameworkChapterDistributionSeeder> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task SeedAsync(Guid frameworkId, string version = "v1", CancellationToken ct = default)
        {
            var chapters = await _db.ComplianceChapters
                .AsNoTracking()
                .Where(c => c.FrameworkId == frameworkId)
                .ToListAsync(ct);

            var chapterIds = chapters.Select(c => c.Id).ToList();
            var standards = await _db.ComplianceStandards
                .AsNoTracking()
                .Where(s => chapterIds.Contains(s.ChapterId))
                .ToListAsync(ct);
            var standardIdsByChapter = standards.GroupBy(s => s.ChapterId)
                .ToDictionary(g => g.Key, g => g.Select(s => s.Id).ToList());

            var allStandardIds = standards.Select(s => s.Id).ToList();
            var oes = await _db.ObjectiveElements
                .AsNoTracking()
                .Where(oe => allStandardIds.Contains(oe.ComplianceStandardId))
                .ToListAsync(ct);

            var oesByStandardId = oes.GroupBy(oe => oe.ComplianceStandardId)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var chapter in chapters)
            {
                var stdIds = standardIdsByChapter.TryGetValue(chapter.Id, out var ids) ? ids : new List<Guid>();
                var totalStandards = stdIds.Count;

                var chapterOes = stdIds.SelectMany(id => oesByStandardId.TryGetValue(id, out var list) ? list : new List<ObjectiveElement>()).ToList();
                var totalOe = chapterOes.Count;

                var core = chapterOes.Count(oe => oe.Classification == ObjectiveElementClassification.Core);
                var commit = chapterOes.Count(oe => oe.Classification == ObjectiveElementClassification.Commitment);
                var achieve = chapterOes.Count(oe => oe.Classification == ObjectiveElementClassification.Achievement);
                var excel = chapterOes.Count(oe => oe.Classification == ObjectiveElementClassification.Excellence);

                if (core + commit + achieve + excel != totalOe)
                {
                    _logger.LogWarning("Distribution mismatch in chapter {ChapterId}: core({Core})+commit({Commit})+achieve({Achieve})+excel({Excel}) != total({Total})",
                        chapter.Id, core, commit, achieve, excel, totalOe);
                }

                var exists = await _db.FrameworkChapterDistributions
                    .AsNoTracking()
                    .FirstOrDefaultAsync(d => d.FrameworkId == frameworkId && d.ChapterId == chapter.Id && d.Version == version, ct);

                if (exists == null)
                {
                    var row = new FrameworkChapterDistribution
                    {
                        Id = Guid.NewGuid(),
                        FrameworkId = frameworkId,
                        ChapterId = chapter.Id,
                        TotalStandards = totalStandards,
                        TotalObjectiveElements = totalOe,
                        CoreCount = core,
                        CommitmentCount = commit,
                        AchievementCount = achieve,
                        ExcellenceCount = excel,
                        Source = DistributionSource.Computed,
                        Version = version,
                        CalculatedAt = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow
                    };
                    _db.FrameworkChapterDistributions.Add(row);
                }
            }

            await _db.SaveChangesAsync(ct);
        }
    }
}

