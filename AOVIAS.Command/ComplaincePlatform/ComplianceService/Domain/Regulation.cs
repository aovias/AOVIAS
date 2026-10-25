using Shared.Infrastructure.Domain;

namespace ComplianceService.Domain
{
    public class Regulation : PlatformEntity
    {
        public Guid HospitalId { get; set; }
        public Guid ComplianceFrameworkId { get; set; }
        public string Status { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? TargetDate { get; set; }
        public DateTime? ValidUntil { get; set; }
    }
}
