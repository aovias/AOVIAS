using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ComplianceService.Application.Contracts;
using ComplianceService.Application.DTOs;
using ComplianceService.Application.EvidenceReview;
using ComplianceService.Application.Events;
using ComplianceService.Application.Services;
using ComplianceService.Domain;
using ComplianceService.Domain.Enums;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ComplianceService.Tests;

public sealed class EvidenceServiceRemoveStorageTests
{
    [Fact]
    public async Task RemoveEvidenceFromObjectiveElement_last_link_deletes_distinct_local_urls()
    {
        var dbName = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<ComplianceDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        var hospitalId = Guid.NewGuid();
        var fwId = Guid.NewGuid();
        var chId = Guid.NewGuid();
        var stdId = Guid.NewGuid();
        var oeId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var evidenceId = Guid.NewGuid();
        var ver1Id = Guid.NewGuid();
        var ver2Id = Guid.NewGuid();
        var linkId = Guid.NewGuid();
        var urlCurrent = "local://App_Data/e/current.bin";
        var urlV1 = "local://App_Data/e/v1.bin";

        await using (var db = new ComplianceDbContext(options))
        {
            await db.Database.EnsureCreatedAsync();

            var now = DateTime.UtcNow;
            var rv = new byte[] { 0, 0, 0, 0, 0, 0, 0, 1 };

            db.ComplianceFrameworks.Add(new ComplianceFramework
            {
                Id = fwId,
                Name = "F",
                Authority = "A",
                Version = "1",
                IsActive = true,
                PlatformId = "pfw01",
                CreatedAt = now,
                RowVersion = rv
            });

            db.ComplianceChapters.Add(new ComplianceChapter
            {
                Id = chId,
                FrameworkId = fwId,
                Code = "CH",
                Name = "Chapter",
                Sequence = 1,
                PlatformId = "pch01",
                CreatedAt = now,
                RowVersion = rv
            });

            db.ComplianceStandards.Add(new ComplianceStandard
            {
                Id = stdId,
                ChapterId = chId,
                Code = "STD",
                Name = "Standard",
                Sequence = 1,
                PlatformId = "pst01",
                CreatedAt = now,
                RowVersion = rv
            });

            db.ObjectiveElements.Add(new ObjectiveElement
            {
                Id = oeId,
                ComplianceStandardId = stdId,
                Code = "OE1",
                Title = "OE",
                Description = "D",
                Classification = ObjectiveElementClassification.Core,
                PlatformId = "poe01",
                CreatedAt = now,
                RowVersion = rv
            });

            db.ComplianceTasks.Add(new ComplianceTask
            {
                Id = taskId,
                HospitalId = hospitalId,
                ComplianceStandardId = stdId,
                ObjectiveElementId = oeId,
                AssignedToUserId = "user",
                DueDate = now.AddDays(1),
                Status = ComplianceTaskStatus.Pending,
                Priority = ComplianceTaskPriority.Medium,
                CreatedAt = now,
                RowVersion = rv
            });

            db.Evidences.Add(new Evidence
            {
                Id = evidenceId,
                TaskId = taskId,
                HospitalId = hospitalId,
                FrameworkId = fwId,
                EvidenceType = "Doc",
                FileUrl = urlCurrent,
                Hash = new string('0', 64),
                UploadedBy = "u",
                UploadedOn = now,
                CurrentVersion = 2,
                Metadata = "{}",
                CreatedAt = now,
                RowVersion = rv
            });

            db.EvidenceVersions.Add(new EvidenceVersion
            {
                Id = ver1Id,
                EvidenceId = evidenceId,
                VersionNumber = 1,
                FileUrl = urlV1,
                Hash = new string('0', 64),
                CreatedBy = Guid.Empty,
                CreatedAt = now,
                Notes = "v1",
                RowVersion = rv
            });

            db.EvidenceVersions.Add(new EvidenceVersion
            {
                Id = ver2Id,
                EvidenceId = evidenceId,
                VersionNumber = 2,
                FileUrl = urlCurrent,
                Hash = new string('0', 64),
                CreatedBy = Guid.Empty,
                CreatedAt = now,
                Notes = "v2",
                RowVersion = rv
            });

            db.ObjectiveElementEvidences.Add(new ObjectiveElementEvidence
            {
                Id = linkId,
                ObjectiveElementId = oeId,
                EvidenceId = evidenceId,
                LinkedBy = Guid.NewGuid(),
                LinkedAt = now,
                VerificationStatus = EvidenceVerificationStatus.Pending,
                CreatedAt = now,
                RowVersion = rv
            });

            await db.SaveChangesAsync();
        }

        var storage = new CapturingFileStorageService();
        var accessor = new HttpContextAccessor();
        await using (var db = new ComplianceDbContext(options))
        {
            var sut = new EvidenceService(
                db,
                storage,
                accessor,
                new NoOpObligationProjectionService(),
                new NoOpDeficiencyTaskLifecycleService(),
                new NoOpComplianceEventEmitter(),
                new NoOpEvidenceReviewService(),
                NullLogger<EvidenceService>.Instance);
            await sut.RemoveEvidenceFromObjectiveElementAsync(oeId, evidenceId);
        }

        Assert.Equal(2, storage.DeletedPaths.Count);
        Assert.Contains(urlV1, storage.DeletedPaths);
        Assert.Contains(urlCurrent, storage.DeletedPaths);

        await using (var db = new ComplianceDbContext(options))
        {
            Assert.False(await db.Evidences.AnyAsync(e => e.Id == evidenceId));
            Assert.False(await db.EvidenceVersions.AnyAsync(v => v.EvidenceId == evidenceId));
        }
    }

    private sealed class CapturingFileStorageService : IFileStorageService
    {
        public List<string> DeletedPaths { get; } = new();

        public Task<string> SaveAsync(
            Stream stream,
            string fileName,
            string contentType,
            string relativePath,
            CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<Stream> OpenReadAsync(string path, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task DeleteAsync(string path, CancellationToken cancellationToken = default)
        {
            DeletedPaths.Add(path);
            return Task.CompletedTask;
        }
    }

    private sealed class NoOpObligationProjectionService : IObligationProjectionService
    {
        public Task<ObligationProjectionResult?> ProjectAsync(
            ObligationProjectionRequest request,
            CancellationToken cancellationToken = default)
            => Task.FromResult<ObligationProjectionResult?>(null);

        public Task<ObligationProjectionResult?> ProjectByObjectiveElementAsync(
            Guid hospitalId,
            Guid objectiveElementId,
            string triggerSource,
            CancellationToken cancellationToken = default)
            => Task.FromResult<ObligationProjectionResult?>(null);
    }

    private sealed class NoOpDeficiencyTaskLifecycleService : IDeficiencyTaskLifecycleService
    {
        public Task SyncFromTaskAsync(
            ComplianceTask task,
            ComplianceTaskStatus? previousStatus,
            CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task SyncFromEvidenceAsync(
            Guid taskId,
            EvidenceLifecycleTrigger trigger,
            CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class NoOpComplianceEventEmitter : IComplianceEventEmitter
    {
        public Task EmitAsync(
            ComplianceEventType eventType,
            Guid entityId,
            string entityType,
            Guid tenantId,
            Guid? userId,
            object payload,
            CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class NoOpEvidenceReviewService : IEvidenceReviewService
    {
        public Task<EvidenceApprovalResultDto> ApplyReviewAsync(
            Guid evidenceId,
            Guid reviewerId,
            EvidenceReviewDecision decision,
            string? comment,
            string? recommendation,
            string? reviewerRole,
            string? expectedRowVersionBase64 = null,
            Guid? objectiveElementId = null,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new EvidenceApprovalResultDto { EvidenceId = evidenceId });

        public Task<PagedResultDto<EvidenceReviewQueueItemDto>> GetReviewQueueAsync(
            Guid hospitalId,
            Guid organizationId,
            Guid frameworkId,
            EvidenceReviewQueueQuery query,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new PagedResultDto<EvidenceReviewQueueItemDto>());

        public Task<IReadOnlyList<EvidenceReviewAuditEntryDto>> GetAuditTrailAsync(
            Guid hospitalId,
            Guid frameworkId,
            Guid evidenceId,
            Guid? objectiveElementId = null,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<EvidenceReviewAuditEntryDto>>(Array.Empty<EvidenceReviewAuditEntryDto>());
    }
}
