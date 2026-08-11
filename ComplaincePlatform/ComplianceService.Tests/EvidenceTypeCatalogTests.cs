using ComplianceService.Application.EvidenceRegistration;
using Xunit;

namespace ComplianceService.Tests;

public sealed class EvidenceTypeCatalogTests
{
    [Theory]
    [InlineData("document", "Document")]
    [InlineData("SOP", "SOP")]
    [InlineData("Policy", "Policy")]
    [InlineData("training record", "Training Record")]
    public void Normalize_maps_aliases(string input, string expected)
    {
        Assert.Equal(expected, EvidenceTypeCatalog.Normalize(input));
    }

    [Fact]
    public void Normalize_rejects_unknown_type()
    {
        Assert.Throws<ArgumentException>(() => EvidenceTypeCatalog.Normalize("not-a-real-type"));
    }
}
