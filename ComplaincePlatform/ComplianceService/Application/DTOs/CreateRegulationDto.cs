namespace ComplianceService.Application.DTOs
{
    public class CreateRegulationDto
    {
        public Guid OrganizationId { get; set; }
        public string Name { get; set; }
        public string Authority { get; set; }  // NABH, ISO, etc.
        public string Version { get; set; }
    }
}
