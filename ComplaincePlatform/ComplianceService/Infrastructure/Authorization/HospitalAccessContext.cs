using ComplianceService.Application.Authorization;

namespace ComplianceService.Infrastructure.Authorization;

public sealed class HospitalAccessContext : IHospitalAccessContext
{
    public HospitalAccessResult? Current { get; set; }
}
