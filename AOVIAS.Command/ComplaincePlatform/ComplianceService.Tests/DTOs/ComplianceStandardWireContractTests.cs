using System.Text.Json;
using ComplianceService.Application.Aggregation.Models;
using ComplianceService.Application.DTOs;
using Xunit;

namespace ComplianceService.Tests.DTOs;

/// <summary>
/// Guards JSON property names for clients after Standard → ComplianceStandard renames.
/// </summary>
public class ComplianceStandardWireContractTests
{
    [Fact]
    public void ComplianceStandardDto_serializes_chapterId_and_nested_standards()
    {
        var dto = new ChapterWithStandardsDto
        {
            Id = Guid.NewGuid(),
            Standards =
            [
                new ComplianceStandardDto
                {
                    Id = Guid.NewGuid(),
                    ChapterId = Guid.NewGuid(),
                    Code = "S1",
                    Name = "One",
                    Sequence = 1,
                    Weight = 1
                }
            ]
        };

        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var json = JsonSerializer.Serialize(dto, options);
        Assert.Contains("\"chapterId\"", json, StringComparison.Ordinal);
        Assert.Contains("\"standards\"", json, StringComparison.Ordinal);
    }

    [Fact]
    public void ComplianceStandardAggregateResult_serializes_compliance_id_as_standardId()
    {
        var agg = new ComplianceStandardAggregateResult
        {
            ComplianceStandardId = Guid.NewGuid(),
            AverageScore = 4m,
            CompliancePercent = 80m
        };

        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var json = JsonSerializer.Serialize(agg, options);
        Assert.Contains("\"standardId\"", json, StringComparison.Ordinal);
        Assert.DoesNotContain("complianceStandardId", json, StringComparison.OrdinalIgnoreCase);
    }
}
