using System;
using System.Threading;
using System.Threading.Tasks;
using ComplianceService.Application.DTOs;

namespace ComplianceService.Application.Contracts;

public interface IDashboardSnapshotService
{
    Task<DashboardSnapshotDto> GetDashboardSnapshotAsync(
        Guid? hospitalId,
        Guid? frameworkId,
        CancellationToken cancellationToken = default);
}
