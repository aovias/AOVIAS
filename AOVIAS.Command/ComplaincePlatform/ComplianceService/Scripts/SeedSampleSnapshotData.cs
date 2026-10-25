using ComplianceService.Domain;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ComplianceService.Scripts;

/// <summary>
/// Script to insert sample ComplianceSnapshot data for testing.
/// Run this from Program.cs during development or as a one-time migration.
/// </summary>
public class SeedSampleSnapshotData
{
    private readonly ComplianceDbContext _context;
    private readonly ILogger<SeedSampleSnapshotData> _logger;

    public SeedSampleSnapshotData(
        ComplianceDbContext context,
        ILogger<SeedSampleSnapshotData> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Seeds sample snapshot data for the specified hospital.
    /// </summary>
    public async Task SeedAsync(Guid hospitalId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Seeding sample snapshot data for hospital {HospitalId}", hospitalId);

        // Check if snapshot already exists
        var existingSnapshot = await _context.ComplianceSnapshots
            .FirstOrDefaultAsync(s => s.HospitalId == hospitalId, cancellationToken);

        if (existingSnapshot != null)
        {
            _logger.LogInformation("Snapshot already exists for hospital {HospitalId}. Updating...", hospitalId);
            
            // Update existing snapshot
            existingSnapshot.OverallScore = 78.5m;
            existingSnapshot.AuditConfidenceScore = 82.0m;
            existingSnapshot.CriticalCount = 2;
            existingSnapshot.MajorCount = 5;
            existingSnapshot.MinorCount = 12;
            existingSnapshot.ObservationCount = 3;
            existingSnapshot.OFICount = 1;
            existingSnapshot.MajorNCCount = 2;
            existingSnapshot.MinorNCCount = 3;
            existingSnapshot.ChapterBreakdownJson = GetSampleChapterBreakdownJson();
            existingSnapshot.HighRiskChaptersJson = GetSampleHighRiskChaptersJson();
            existingSnapshot.CalculatedAt = DateTime.UtcNow;

            _context.ComplianceSnapshots.Update(existingSnapshot);
        }
        else
        {
            _logger.LogInformation("Creating new snapshot for hospital {HospitalId}", hospitalId);
            
            // Create new snapshot
            var snapshot = new ComplianceSnapshot
            {
                Id = Guid.NewGuid(),
                HospitalId = hospitalId,
                OverallScore = 78.5m,
                AuditConfidenceScore = 82.0m,
                CriticalCount = 2,
                MajorCount = 5,
                MinorCount = 12,
                ObservationCount = 3,
                OFICount = 1,
                MajorNCCount = 2,
                MinorNCCount = 3,
                ChapterBreakdownJson = GetSampleChapterBreakdownJson(),
                HighRiskChaptersJson = GetSampleHighRiskChaptersJson(),
                CalculatedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            await _context.ComplianceSnapshots.AddAsync(snapshot, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Sample snapshot data seeded successfully for hospital {HospitalId}", hospitalId);
    }

    /// <summary>
    /// Gets sample chapter breakdown JSON matching ChapterScoreDto structure.
    /// </summary>
    private static string GetSampleChapterBreakdownJson()
    {
        var chapterScores = new[]
        {
            new
            {
                chapterId = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                weightedScore = 85.5m,
                completionPercentage = 85.5m,
                riskPenalty = 0.0m
            },
            new
            {
                chapterId = Guid.Parse("00000000-0000-0000-0000-000000000002"),
                weightedScore = 72.0m,
                completionPercentage = 72.0m,
                riskPenalty = 0.0m
            },
            new
            {
                chapterId = Guid.Parse("00000000-0000-0000-0000-000000000003"),
                weightedScore = 60.0m,
                completionPercentage = 65.0m,
                riskPenalty = 5.0m
            },
            new
            {
                chapterId = Guid.Parse("00000000-0000-0000-0000-000000000004"),
                weightedScore = 90.0m,
                completionPercentage = 90.0m,
                riskPenalty = 0.0m
            }
        };

        return JsonSerializer.Serialize(chapterScores, new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

    /// <summary>
    /// Gets sample high-risk chapters JSON matching HighRiskChapter structure.
    /// </summary>
    private static string GetSampleHighRiskChaptersJson()
    {
        var highRiskChapters = new[]
        {
            new
            {
                chapterId = Guid.Parse("00000000-0000-0000-0000-000000000003"),
                chapterCode = "HRM",
                chapterName = "Human Resource Management",
                completionPercentage = 45.0m,
                totalObjectiveElements = 10,
                completedObjectiveElements = 4
            }
        };

        return JsonSerializer.Serialize(highRiskChapters, new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }
}
