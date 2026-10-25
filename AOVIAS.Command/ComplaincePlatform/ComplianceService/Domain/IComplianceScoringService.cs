namespace ComplianceService.Domain
{
    public interface IComplianceScoringService
    {
        ComplianceScoreResult Calculate(ComplianceScoringContext context);
    }
}
