namespace ComplianceService.Application.DTOs
{
    public class EnrollRegulationRequest
    {
        public Guid? HospitalId { get; set; } // Optional - will use token hospital_id if not provided
        public Guid ComplianceFrameworkId { get; set; }
        public DateTime? TargetDate { get; set; }
    }
}
