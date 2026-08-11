using Compliance.MasterData.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace Compliance.MasterData.Contracts
{
    public interface IMasterDataPersistence
    {
        Task<bool> FrameworkExistsAsync(string name, string version, CancellationToken ct);

        Task<Guid> CreateFrameworkAsync(MasterFrameworkDto framework, CancellationToken ct);

        Task<Guid> CreateChapterAsync(Guid frameworkId, MasterChapterDto chapter, CancellationToken ct);

        /// <summary>
        /// Creates an ObjectiveElement under a default ComplianceStandard for the chapter (NABH master-data import).
        /// </summary>
        Task<Guid> CreateObjectiveElementAsync(Guid chapterId, MasterObjectiveElementSeedDto seed, CancellationToken ct);

        Task CreateRequirementAsync(Guid objectiveElementId, MasterRequirementDto requirement, CancellationToken ct);

        Task BeginTransactionAsync(CancellationToken ct);
        Task CommitAsync(CancellationToken ct);
        Task RollbackAsync(CancellationToken ct);
    }

}
