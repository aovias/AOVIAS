using KnowledgeBase.Application.Contracts;
using KnowledgeBase.Domain.Entities;
using KnowledgeBase.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeBase.Infrastructure.Repositories;

public sealed class GapAnalysisRepository : IGapAnalysisRepository
{
    private readonly KnowledgeBaseContext _db;

    public GapAnalysisRepository(KnowledgeBaseContext db) => _db = db;

    public async Task<GapAnalysisReport?> GetByAssessmentIdAsync(
        Guid assessmentId,
        CancellationToken cancellationToken = default)
    {
        return await _db.GapAnalysisReports.AsNoTracking()
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.AssessmentId == assessmentId, cancellationToken);
    }

    public async Task<GapAnalysisReport?> GetByIdWithItemsAsync(
        Guid reportId,
        CancellationToken cancellationToken = default)
    {
        return await _db.GapAnalysisReports.AsNoTracking()
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == reportId, cancellationToken);
    }

    public async Task UpsertReportAsync(GapAnalysisReport report, CancellationToken cancellationToken = default)
    {
        var existing = await _db.GapAnalysisReports
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.AssessmentId == report.AssessmentId, cancellationToken);

        if (existing is null)
        {
            await _db.GapAnalysisReports.AddAsync(report, cancellationToken);
            return;
        }

        existing.GeneratedAt = report.GeneratedAt;
        existing.OverallPercent = report.OverallPercent;
        existing.MeetsOverallPercent = report.MeetsOverallPercent;
        existing.CoreRulePassed = report.CoreRulePassed;
        existing.ChapterAveragesPassed = report.ChapterAveragesPassed;
        existing.StandardAveragesPassed = report.StandardAveragesPassed;
        existing.CommitmentLevelPassed = report.CommitmentLevelPassed;
        existing.CriticalCount = report.CriticalCount;
        existing.MajorCount = report.MajorCount;
        existing.MinorCount = report.MinorCount;
        existing.CompliantCount = report.CompliantCount;

        _db.GapAnalysisItems.RemoveRange(existing.Items);
        foreach (var item in report.Items)
        {
            item.ReportId = existing.Id;
            await _db.GapAnalysisItems.AddAsync(item, cancellationToken);
        }

        report.Id = existing.Id;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _db.SaveChangesAsync(cancellationToken);
    }
}
