using System.Text.Json.Serialization;
using ComplianceService.Domain;

namespace ComplianceService.Application.ComplianceScoring;

/// <summary>
/// Optional enrichment describing compliance standard context for an Objective Element in scoring payloads.
/// </summary>
public class ObjectiveElementContextDto
{
    public Guid ObjectiveElementId { get; set; }

    public string? ObjectiveElementCode { get; set; }

    public ObjectiveElementClassification? Classification { get; set; }

    public bool? IsMandatory { get; set; }

    [JsonPropertyName("standardId")]
    public Guid? ComplianceStandardId { get; set; }

    [JsonPropertyName("standardCode")]
    public string? StandardCode { get; set; }
}
