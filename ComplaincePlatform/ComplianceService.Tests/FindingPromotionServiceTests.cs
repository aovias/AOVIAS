using ComplianceService.Application.Services;

using ComplianceService.Domain.Enums;

using Shared.Infrastructure.Domain.Enums;

using Xunit;



namespace ComplianceService.Tests;



public sealed class FindingPromotionServiceTests

{

    [Theory]

    [InlineData(FindingClassification.AuditFinding, true)]

    [InlineData(FindingClassification.Observation, true)]

    [InlineData(FindingClassification.NonConformity, true)]

    [InlineData(FindingClassification.OpportunityForImprovement, true)]

    [InlineData(FindingClassification.GoodPractice, false)]

    public void IsClassificationEligible_returns_expected(FindingClassification classification, bool expected)

    {

        var messages = new List<string>();

        Assert.Equal(expected, FindingPromotionEligibilityService.IsClassificationEligible(classification, messages));

        if (!expected)

        {

            Assert.NotEmpty(messages);

        }

    }



    [Theory]

    [InlineData(FindingClassification.NonConformity, DeficiencyType.Assessment)]

    [InlineData(FindingClassification.OpportunityForImprovement, DeficiencyType.Maturity)]

    public void MapDeficiencyType_maps_classification(FindingClassification input, DeficiencyType expected)

    {

        Assert.Equal(expected, FindingPromotionEligibilityService.MapDeficiencyType(input));

    }



    [Theory]

    [InlineData(FindingSeverity.Critical, DeficiencySeverity.Critical)]

    [InlineData(FindingSeverity.Major, DeficiencySeverity.Major)]

    [InlineData(FindingSeverity.Moderate, DeficiencySeverity.Moderate)]

    [InlineData(FindingSeverity.Informational, DeficiencySeverity.Minor)]

    public void MapSeverity_maps_finding_to_deficiency(FindingSeverity input, DeficiencySeverity expected)

    {

        Assert.Equal(expected, FindingPromotionEligibilityService.MapSeverity(input));

    }

}


