using ComplianceService.Application.Services;
using Shared.Governance;
using Xunit;

namespace ComplianceService.Tests;

public class DepartmentOwnershipGovernanceTests
{
    private sealed class FakeGovernanceClient : IGovernanceClient
    {
        public OwnerLookupResult Result { get; set; } = new();
        public Task<IReadOnlyList<GovernanceTimelineEntryDto>> GetTimelineAsync(string resourceType, Guid resourceId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<GovernanceTimelineEntryDto>>(Array.Empty<GovernanceTimelineEntryDto>());
        public Task<OwnerLookupResult> GetOwnersAsync(OwnerLookupRequest request, CancellationToken ct = default)
            => Task.FromResult(Result);
    }

    [Fact]
    public async Task GetGovernanceOwners_returns_empty_when_client_unavailable()
    {
        var svc = new DepartmentOwnershipAppService(null!, null!, Microsoft.Extensions.Logging.Abstractions.NullLogger<DepartmentOwnershipAppService>.Instance);
        var owners = await svc.GetGovernanceOwnersAsync(Guid.NewGuid());
        Assert.Empty(owners);
    }

    [Fact]
    public async Task GetGovernanceOwners_delegates_to_client()
    {
        var deptId = Guid.NewGuid();
        var client = new FakeGovernanceClient
        {
            Result = new OwnerLookupResult
            {
                Owners = [new GovernanceOwnerDto { AssigneeType = "Position", AssigneeId = Guid.NewGuid(), Priority = "Primary" }]
            }
        };
        var svc = new DepartmentOwnershipAppService(null!, null!, Microsoft.Extensions.Logging.Abstractions.NullLogger<DepartmentOwnershipAppService>.Instance, client);
        var owners = await svc.GetGovernanceOwnersAsync(deptId);
        Assert.Single(owners);
        Assert.Equal("Primary", owners[0].Priority);
    }
}
