using System.Text.Json.Serialization;

namespace ComplianceService.Application.Aggregation.Models;

public class ComplianceStandardAggregateResult
{
    [JsonPropertyName("standardId")]
    public Guid ComplianceStandardId { get; set; }

    public decimal AverageScore { get; set; }

    public decimal CompliancePercent { get; set; }

    public int ObjectiveElementCount { get; set; }

    public int NaExcludedCount { get; set; }
}
