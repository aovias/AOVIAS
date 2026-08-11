using PersonService.Domain.ValueObjects;
using Xunit;

namespace PersonService.Tests;

public class PersonNameHelperTests
{
    [Fact]
    public void ComputeSearchName_NormalizesToUpper()
    {
        var result = PersonNameHelper.ComputeSearchName("John", "Doe", null, null);
        Assert.Equal("JOHN DOE", result);
    }

    [Fact]
    public void ComputeSearchName_IncludesDisplayAndPreferred()
    {
        var result = PersonNameHelper.ComputeSearchName("John", "Doe", "Johnny", "JD");
        Assert.Contains("JOHN", result);
        Assert.Contains("DOE", result);
        Assert.Contains("JOHNNY", result);
    }

    [Fact]
    public void NormalizeEmail_LowercasesAndTrims()
    {
        Assert.Equal("user@example.com", PersonNameHelper.NormalizeEmail("  User@Example.COM "));
    }

    [Fact]
    public void NormalizePhone_StripsNonDigits()
    {
        Assert.Equal("919876543210", PersonNameHelper.NormalizePhone("+91 98765-43210"));
    }
}
