namespace ComplianceService.Application.Decision.Models
{
    public class RuleEvaluation
    {
        public string Rule { get; set; }
        public bool Passed { get; set; }
        public string? Detail { get; set; }
    }

    public class DecisionResult
    {
        public string Outcome { get; set; } // PASS | FAIL | CONDITIONAL
        public decimal OverallCompliancePercent { get; set; }
        public List<RuleEvaluation> Evaluations { get; set; } = new();
    }
}

