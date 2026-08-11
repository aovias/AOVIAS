using KnowledgeBase.Application.AssessmentSummary;
using KnowledgeBase.Application.DTOs;
using Xunit;

namespace Knowledgebase.Tests;

public sealed class WizardAssessmentAnswerMapperTests
{
    [Theory]
    [InlineData("yes", 5, "Met", false, true)]
    [InlineData("no", 1, "NotMet", true, false)]
    [InlineData("not_sure", 3, "Partial", true, false)]
    public void MapToUpdateItemDto_maps_wizard_answers(
        string answer,
        int expectedScore,
        string expectedStatus,
        bool expectedActionPlan,
        bool expectedDocumentationAdequate)
    {
        var dto = WizardAssessmentAnswerMapper.MapToUpdateItemDto(
            new WizardAssessmentAnswerDto("node-id", "Sample question?", answer));

        Assert.Equal(expectedScore, dto.Score);
        Assert.Equal(expectedStatus, dto.ComplianceStatus);
        Assert.Equal(expectedActionPlan, dto.HasActionPlan);
        Assert.Equal(expectedDocumentationAdequate, dto.DocumentationAdequate);
        Assert.Equal("Sample question?", dto.Remarks);
    }
}
