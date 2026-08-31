using ComplianceService.Application.Authorization;
using ComplianceService.Application.Commands;
using ComplianceService.Application.Contracts;
using ComplianceService.Application.Services;
using ComplianceService.Domain;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Shared.Infrastructure.DTO;
using Xunit;

namespace ComplianceService.Tests.Authorization;

public sealed class EnrollHospitalAccessTests : IDisposable
{
    private static readonly Guid OrgA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid HospitalA = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid FrameworkId = Guid.Parse("0fe8a9c0-9ea0-4970-9097-d4f73d45dca7");

    private readonly ComplianceDbContext _db;
    private readonly RecordingObligationSync _sync = new();
    private readonly RecordingHospitalRepo _hospitals = new();
    private readonly StubFrameworkRepository _frameworks = new();

    public EnrollHospitalAccessTests()
    {
        var options = new DbContextOptionsBuilder<ComplianceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new ComplianceDbContext(options, userContext: null);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task Denied_hospital_does_not_create_compliance_records()
    {
        var sut = CreateHandler(HospitalAccessResult.Deny(HospitalAccessStatus.HospitalNotFound, Guid.NewGuid()));

        await Assert.ThrowsAsync<HospitalAccessException>(() =>
            sut.HandleAsync(new EnrollHospitalInFrameworkCommand(Guid.NewGuid(), FrameworkId, Guid.NewGuid()), CancellationToken.None));

        Assert.False(_hospitals.SaveCalled);
        Assert.Empty(_sync.Calls);
        Assert.Equal(0, await _db.OrganizationComplianceObligations.CountAsync());
    }

    [Fact]
    public async Task Allowed_enrollment_syncs_organization_id_from_directory_not_hospital_id()
    {
        _frameworks.Active.Add(new FrameworkDto
        {
            FrameworkId = FrameworkId,
            Name = "NABH",
            IsActive = true,
            Status = "Active"
        });

        var hospital = new OrganizationHospitalRecord(HospitalA, OrgA, "Active", false, true);
        var sut = CreateHandler(HospitalAccessResult.Allowed(HospitalA, hospital));

        await sut.HandleAsync(new EnrollHospitalInFrameworkCommand(HospitalA, FrameworkId, Guid.NewGuid()), CancellationToken.None);

        Assert.True(_hospitals.SaveCalled);
        var call = Assert.Single(_sync.Calls);
        Assert.Equal(OrgA, call.OrganizationId);
        Assert.Equal(OrgA, call.TenantId);
        Assert.Equal(HospitalA, call.HospitalId);
        Assert.NotEqual(call.HospitalId, call.OrganizationId);
    }

    [Fact]
    public async Task Already_enrolled_is_still_conflict_after_access_is_allowed()
    {
        _frameworks.Active.Add(new FrameworkDto
        {
            FrameworkId = FrameworkId,
            Name = "NABH",
            IsActive = true,
            Status = "Active"
        });
        _frameworks.Enrolled.Add(new HospitalFrameworkDto
        {
            FrameworkId = FrameworkId,
            FrameworkName = "NABH",
            Status = "InProgress"
        });

        var hospital = new OrganizationHospitalRecord(HospitalA, OrgA, "Active", false, true);
        var sut = CreateHandler(HospitalAccessResult.Allowed(HospitalA, hospital));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.HandleAsync(new EnrollHospitalInFrameworkCommand(HospitalA, FrameworkId, Guid.NewGuid()), CancellationToken.None));

        Assert.Contains("already enrolled", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(_hospitals.SaveCalled);
        Assert.Empty(_sync.Calls);
    }

    private EnrollHospitalInFrameworkCommandHandler CreateHandler(HospitalAccessResult access)
        => new(
            _hospitals,
            _frameworks,
            new NoopDispatcher(),
            _db,
            new NoopUnitOfWork(),
            _sync,
            new StubAuthorizer(access),
            new StubCallerResolver(),
            new HospitalAccessContextStub(),
            NullLogger<EnrollHospitalInFrameworkCommandHandler>.Instance);

    private sealed class StubAuthorizer : IHospitalAccessAuthorizer
    {
        private readonly HospitalAccessResult _result;
        public StubAuthorizer(HospitalAccessResult result) => _result = result;
        public Task<HospitalAccessResult> AuthorizeAsync(Guid requestedHospitalId, HospitalAccessCaller caller, CancellationToken cancellationToken = default)
            => Task.FromResult(_result);
    }

    private sealed class StubCallerResolver : IHospitalAccessCallerResolver
    {
        public HospitalAccessCaller Resolve()
            => new(HospitalAccessCallerKind.User, OrgA, HospitalA, ["ORG_ADMIN"]);
    }

    private sealed class HospitalAccessContextStub : IHospitalAccessContext
    {
        public HospitalAccessResult? Current { get; set; }
    }

    private sealed class RecordingObligationSync : IObligationSynchronizationService
    {
        public List<(Guid OrganizationId, Guid HospitalId, Guid TenantId, Guid FrameworkId)> Calls { get; } = [];

        public Task SyncForOrganizationAsync(Guid organizationId, Guid hospitalId, Guid tenantId, Guid frameworkId, CancellationToken ct = default)
        {
            Calls.Add((organizationId, hospitalId, tenantId, frameworkId));
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingHospitalRepo : IHospitalComplianceRepository
    {
        public bool SaveCalled { get; private set; }

        public Task<HospitalComplianceAggregate> LoadAsync(Guid hospitalId, Guid frameworkId, CancellationToken ct)
            => throw new InvalidOperationException("No aggregate");

        public Task SaveAsync(HospitalComplianceAggregate aggregate, CancellationToken ct)
        {
            SaveCalled = true;
            return Task.CompletedTask;
        }
    }

    private sealed class StubFrameworkRepository : IFrameworkRepository
    {
        public List<FrameworkDto> Active { get; } = [];
        public List<HospitalFrameworkDto> Enrolled { get; } = [];

        public Task<List<FrameworkDto>> GetAllActiveFrameworksAsync(CancellationToken cancellationToken)
            => Task.FromResult(Active);

        public Task<List<HospitalFrameworkDto>> GetHospitalFrameworksAsync(Guid hospitalId, CancellationToken cancellationToken)
            => Task.FromResult(Enrolled);
    }

    private sealed class NoopDispatcher : IDomainEventDispatcher
    {
        public Task DispatchAsync(IEnumerable<IDomainEvent> events, CancellationToken ct)
            => Task.CompletedTask;
    }

    private sealed class NoopUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
    }
}
