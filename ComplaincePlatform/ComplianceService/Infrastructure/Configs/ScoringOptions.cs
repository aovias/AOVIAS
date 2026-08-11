namespace ComplianceService.Infrastructure.Configs
{
    public class ScoringOptions
    {
        public bool DowngradeEnabled { get; set; } = true;
        public int DowngradeStep { get; set; } = 1; // subtract 1 when documentation inadequate
        public int RequireRationaleThreshold { get; set; } = 3; // require rationale when score <= 3
        public bool RequireNaApproval { get; set; } = false; // if true, NaApprovedBy must be provided
    }
}

