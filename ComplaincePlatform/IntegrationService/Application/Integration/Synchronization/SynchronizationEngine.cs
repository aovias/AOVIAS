using IntegrationService.Application.Contracts;
using IntegrationService.Application.Integration.Platform;
using IntegrationService.Domain.ReadModels;
using IntegrationService.Infrastructure.Clients;
using IntegrationService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IntegrationService.Application.Integration.Synchronization;

public sealed class SynchronizationEngine(
    IntegrationDbContext db,
    IConnectorRegistry registry,
    IMappingEngine mappingEngine,
    IPlatformWriteClient platformWrite,
    IIntegrationAuditService audit,
    IIntegrationOutboxWriter outbox,
    ILogger<SynchronizationEngine> logger) : ISynchronizationEngine
{
    public async Task<SynchronizationRunDto> RunAsync(Guid jobId, string traceId, Guid? userId, CancellationToken ct = default)
    {
        var job = await db.SynchronizationJobs.FirstOrDefaultAsync(j => j.Id == jobId && !j.IsDeleted, ct)
            ?? throw new InvalidOperationException("Sync job not found");
        return await ExecuteRunAttemptAsync(job, traceId, userId, attempt: 1, ct);
    }

    private async Task<SynchronizationRunDto> ExecuteRunAttemptAsync(
        SynchronizationJob job, string traceId, Guid? userId, int attempt, CancellationToken ct)
    {
        var run = new SynchronizationRun
        {
            Id = Guid.NewGuid(),
            SynchronizationJobId = job.Id,
            OrganizationId = job.OrganizationId,
            Status = attempt > 1 ? "Retrying" : "Running",
            TraceId = traceId,
            StartedAtUtc = DateTime.UtcNow,
            AttemptCount = attempt
        };
        db.SynchronizationRuns.Add(run);
        await db.SaveChangesAsync(ct);
        if (attempt == 1)
            await outbox.EnqueueAsync("SynchronizationStarted", job.Id, new { jobId = job.Id, runId = run.Id }, ct);

        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var connector = await db.Connectors.FirstAsync(c => c.Id == job.SourceConnectorId, ct);
            var provider = registry.Resolve(connector.ProviderType);
            var lastRun = await db.SynchronizationRuns.AsNoTracking()
                .Where(r => r.SynchronizationJobId == job.Id && r.Status == "Completed")
                .OrderByDescending(r => r.CompletedAtUtc)
                .FirstOrDefaultAsync(ct);

            var context = new SyncContext { OrganizationId = job.OrganizationId, RunId = run.Id, Watermark = lastRun?.Watermark, TraceId = traceId };
            var pull = await provider.PullAsync(connector, context, ct);
            var profile = await db.MappingProfiles.FirstAsync(p => p.Id == job.MappingProfileId, ct);
            var fields = await db.FieldMappings.Where(f => f.MappingProfileId == profile.Id).ToListAsync(ct);
            var rules = await db.TransformationRules.Where(r => r.OrganizationId == job.OrganizationId).ToListAsync(ct);

            foreach (var record in pull.Records)
            {
                try
                {
                    var mapped = await mappingEngine.MapAsync(record, profile, fields, rules, ct);
                    var conflict = await DetectConflictAsync(record, job.ConflictPolicy, ct);
                    if (conflict) { run.ConflictsDetected++; if (job.ConflictPolicy == "ManualReview") continue; }
                    await platformWrite.UpsertAsync(job.OrganizationId, mapped, record.ExternalId, ct);
                    run.RecordsProcessed++;
                }
                catch (PlatformWriteException ex)
                {
                    run.RecordsFailed++;
                    logger.LogWarning(ex, "Platform write failed for record {ExternalId}", record.ExternalId);
                }
                catch (Exception ex)
                {
                    run.RecordsFailed++;
                    logger.LogWarning(ex, "Failed to process record {ExternalId}", record.ExternalId);
                }
            }

            if (run.RecordsFailed > 0 && attempt < job.MaxRetries)
            {
                run.Status = "Retrying";
                run.CompletedAtUtc = DateTime.UtcNow;
                run.DurationMs = sw.ElapsedMilliseconds;
                await db.SaveChangesAsync(ct);
                var delayMs = (int)Math.Pow(2, attempt) * 1000;
                await Task.Delay(delayMs, ct);
                return await ExecuteRunAttemptAsync(job, traceId, userId, attempt + 1, ct);
            }

            run.Status = run.RecordsFailed > 0 && attempt >= job.MaxRetries ? "Failed" : "Completed";
            run.Watermark = pull.Watermark ?? DateTime.UtcNow.ToString("O");
            run.CompletedAtUtc = DateTime.UtcNow;
            run.DurationMs = sw.ElapsedMilliseconds;
            if (run.Status == "Completed")
            {
                connector.LifecycleState = "Connected";
                connector.HealthStatus = "Healthy";
            }
            await db.SaveChangesAsync(ct);
            await outbox.EnqueueAsync(run.Status == "Completed" ? "SynchronizationCompleted" : "SynchronizationFailed",
                job.Id, new { jobId = job.Id, runId = run.Id, run.RecordsProcessed, run.RecordsFailed }, ct);
            await audit.LogAsync(job.OrganizationId, "Synchronization", run.Status, userId, connector.Id, connector.ProviderType, profile.TargetEntity, run.RecordsProcessed, run.DurationMs, traceId, ct: ct);
        }
        catch (Exception ex)
        {
            run.Status = "Failed";
            run.ErrorSummary = ex.Message;
            run.CompletedAtUtc = DateTime.UtcNow;
            run.DurationMs = sw.ElapsedMilliseconds;
            await db.SaveChangesAsync(ct);
            await audit.LogAsync(job.OrganizationId, "Synchronization", "Failed", userId, job.SourceConnectorId, "connector", "platform", run.RecordsProcessed, run.DurationMs, traceId, ex.Message, ct);
            throw;
        }

        return new SynchronizationRunDto
        {
            Id = run.Id, JobId = job.Id, Status = run.Status, RecordsProcessed = run.RecordsProcessed,
            RecordsFailed = run.RecordsFailed, ConflictsDetected = run.ConflictsDetected,
            DurationMs = run.DurationMs, TraceId = traceId, StartedAtUtc = run.StartedAtUtc
        };
    }

    private Task<bool> DetectConflictAsync(ExternalRecord record, string policy, CancellationToken ct)
    {
        _ = ct;
        if (policy == "ExternalWins") return Task.FromResult(false);
        if (!string.IsNullOrWhiteSpace(record.RecordHash))
            return Task.FromResult(record.RecordHash.Length < 8);
        return Task.FromResult(record.LastModifiedUtc.HasValue && record.LastModifiedUtc < DateTime.UtcNow.AddYears(-10));
    }
}

public sealed class SynchronizationJobService(IntegrationDbContext db)
{
    public async Task<SynchronizationJobDto> CreateAsync(CreateSyncJobRequest request, CancellationToken ct = default)
    {
        var job = new SynchronizationJob
        {
            Id = Guid.NewGuid(),
            OrganizationId = request.OrganizationId,
            Name = request.Name,
            SourceConnectorId = request.SourceConnectorId,
            MappingProfileId = request.MappingProfileId,
            Direction = request.Direction,
            SyncType = request.SyncType
        };
        db.SynchronizationJobs.Add(job);
        if (!string.IsNullOrWhiteSpace(request.CronExpression))
        {
            db.SynchronizationSchedules.Add(new SynchronizationSchedule
            {
                Id = Guid.NewGuid(),
                SynchronizationJobId = job.Id,
                CronExpression = request.CronExpression,
                IsEnabled = true,
                NextRunUtc = DateTime.UtcNow.AddHours(1)
            });
        }
        await db.SaveChangesAsync(ct);
        return Map(job);
    }

    public async Task<IReadOnlyList<SynchronizationJobDto>> ListAsync(Guid organizationId, CancellationToken ct = default)
    {
        var jobs = await db.SynchronizationJobs.AsNoTracking().Where(j => j.OrganizationId == organizationId && !j.IsDeleted).ToListAsync(ct);
        return jobs.Select(Map).ToList();
    }

    public async Task<IReadOnlyList<SynchronizationRunDto>> ListRunsAsync(Guid jobId, CancellationToken ct = default)
    {
        return await db.SynchronizationRuns.AsNoTracking().Where(r => r.SynchronizationJobId == jobId)
            .OrderByDescending(r => r.StartedAtUtc)
            .Select(r => new SynchronizationRunDto
            {
                Id = r.Id, JobId = jobId, Status = r.Status, RecordsProcessed = r.RecordsProcessed,
                RecordsFailed = r.RecordsFailed, ConflictsDetected = r.ConflictsDetected,
                DurationMs = r.DurationMs, TraceId = r.TraceId, StartedAtUtc = r.StartedAtUtc
            }).ToListAsync(ct);
    }

    private static SynchronizationJobDto Map(SynchronizationJob j) => new()
    {
        Id = j.Id, Name = j.Name, SourceConnectorId = j.SourceConnectorId,
        MappingProfileId = j.MappingProfileId, Direction = j.Direction, SyncType = j.SyncType, IsEnabled = j.IsEnabled
    };
}
