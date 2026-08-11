using Compliance.MasterData.Contracts;
using Compliance.MasterData.DTOs;
using ComplianceService.Domain;
using ComplianceService.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Shared.Infrastructure.Services;

namespace ComplianceService.Infrastructure.Persistence
{
    public sealed class EfMasterDataPersistence : IMasterDataPersistence
    {
        private readonly ComplianceDbContext _db;
        private readonly IPlatformIdGenerator _platformIdGenerator;
        private IDbContextTransaction? _currentTransaction;

        public EfMasterDataPersistence(
            ComplianceDbContext db,
            IPlatformIdGenerator platformIdGenerator)
        {
            _db = db;
            _platformIdGenerator = platformIdGenerator;
        }

        public Task<bool> FrameworkExistsAsync(string name, string version, CancellationToken ct)
            => _db.ComplianceFrameworks
                  .AnyAsync(f => f.Name == name && f.Version == version, ct);

        public async Task<Guid> CreateFrameworkAsync(
            MasterFrameworkDto framework, CancellationToken ct)
        {
            // First, check if framework already exists (idempotent check)
            var existing = await _db.ComplianceFrameworks
                .FirstOrDefaultAsync(
                    f => f.Name == framework.Name && f.Version == framework.Version,
                    ct);

            if (existing != null)
            {
                return existing.Id; // Return existing framework ID
            }

            // Framework doesn't exist, create it
            var entity = new ComplianceFramework
            {
                PlatformId = await _platformIdGenerator.GenerateAsync("CFW"),
                Name = framework.Name,
                Version = framework.Version,
                Authority = framework.Authority,
                IsActive = true
            };

            try
            {
                _db.ComplianceFrameworks.Add(entity);
                await _db.SaveChangesAsync(ct);
                return entity.Id;
            }
            catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UNIQUE") == true ||
                                               ex.InnerException?.Message.Contains("duplicate") == true)
            {
                // Handle unique constraint violation (race condition protection)
                // Another thread/process may have inserted the framework between our check and insert
                _db.Entry(entity).State = EntityState.Detached; // Detach the failed entity

                // Query again to get the existing framework
                var existingAfterConflict = await _db.ComplianceFrameworks
                    .FirstOrDefaultAsync(
                        f => f.Name == framework.Name && f.Version == framework.Version,
                        ct);

                if (existingAfterConflict != null)
                {
                    return existingAfterConflict.Id; // Return existing framework ID
                }

                // If still not found, re-throw the exception
                throw;
            }
        }

        public async Task<Guid> CreateChapterAsync(
            Guid frameworkId, MasterChapterDto chapter, CancellationToken ct)
        {
            var entity = new ComplianceChapter
            {
                PlatformId = await _platformIdGenerator.GenerateAsync("CHP"),
                FrameworkId = frameworkId,
                Code = chapter.Code,
                Name = chapter.Name,
                Sequence = chapter.Sequence
            };

            _db.ComplianceChapters.Add(entity);
            await _db.SaveChangesAsync(ct);
            return entity.Id;
        }

        private async Task<Guid> GetOrCreateDefaultComplianceStandardIdAsync(Guid chapterId, CancellationToken ct)
        {
            const string defaultCode = "DEFAULT";
            var existing = await _db.ComplianceStandards
                .FirstOrDefaultAsync(s => s.ChapterId == chapterId && s.Code == defaultCode, ct);
            if (existing != null)
            {
                return existing.Id;
            }

            var std = new ComplianceStandard
            {
                PlatformId = await _platformIdGenerator.GenerateAsync("STD"),
                ChapterId = chapterId,
                Code = defaultCode,
                Name = "Default",
                Sequence = 0
            };
            _db.ComplianceStandards.Add(std);
            await _db.SaveChangesAsync(ct);
            return std.Id;
        }

        public async Task<Guid> CreateObjectiveElementAsync(
            Guid chapterId, MasterObjectiveElementSeedDto seed, CancellationToken ct)
        {
            var complianceStandardId = await GetOrCreateDefaultComplianceStandardIdAsync(chapterId, ct);
            var entity = new ObjectiveElement
            {
                PlatformId = await _platformIdGenerator.GenerateAsync("MOE"),
                ComplianceStandardId = complianceStandardId,
                Code = seed.Code,
                Title = seed.Description?.Length > 0 ? seed.Description : seed.Code,
                Description = seed.Description ?? string.Empty,
                Classification = ObjectiveElementClassification.Core,
                IsMandatory = true,
                IsScorable = true,
                IsApplicable = true,
                IsDocumentationRequired = false
            };

            _db.ObjectiveElements.Add(entity);
            await _db.SaveChangesAsync(ct);
            return entity.Id;
        }

        public async Task CreateRequirementAsync(
            Guid objectiveElementId, MasterRequirementDto req, CancellationToken ct)
        {
            var oe = await _db.ObjectiveElements.FirstOrDefaultAsync(x => x.Id == objectiveElementId, ct)
                ?? throw new InvalidOperationException($"ObjectiveElement {objectiveElementId} not found.");

            if (!string.IsNullOrWhiteSpace(req.Text))
            {
                oe.Description = req.Text;
            }

            oe.Department = req.Department;
            oe.ResponsibleRole = req.Role;
            oe.Frequency = req.Frequency;
            oe.EvidenceCategory = req.EvidenceType;

            await _db.SaveChangesAsync(ct);
        }

        public async Task BeginTransactionAsync(CancellationToken ct)
        {
            // Note: Transactions started here will be used with execution strategy
            // The actual transaction operations should be wrapped in execution strategy
            // by the caller, or we disable retry for transaction operations
            // For now, we'll start the transaction directly but note that queries
            // inside this transaction may fail if retry strategy is enabled
            _currentTransaction = await _db.Database.BeginTransactionAsync(ct);
        }

        public async Task CommitAsync(CancellationToken ct)
        {
            if (_currentTransaction != null)
            {
                await _currentTransaction.CommitAsync(ct);
                await _currentTransaction.DisposeAsync();
                _currentTransaction = null;
            }
        }

        public async Task RollbackAsync(CancellationToken ct)
        {
            if (_currentTransaction != null)
            {
                await _currentTransaction.RollbackAsync(ct);
                await _currentTransaction.DisposeAsync();
                _currentTransaction = null;
            }
        }
    }

}
