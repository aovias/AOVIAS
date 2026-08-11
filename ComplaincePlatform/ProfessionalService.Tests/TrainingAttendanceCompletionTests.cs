using ProfessionalService.Application.Training;
using Xunit;

namespace ProfessionalService.Tests;

public class TrainingAttendanceCompletionTests
{
    [Fact]
    public void CalculatePercent_Empty_ReturnsZero()
    {
        Assert.Equal(0m, TrainingAttendanceCompletion.CalculatePercent(0, 0));
        Assert.Equal(0m, TrainingAttendanceCompletion.CalculatePercent(Array.Empty<(string, bool?)>()));
    }

    [Fact]
    public void CalculatePercent_AllAttended_Returns100()
    {
        var rows = new[]
        {
            (TrainingAttendanceCompletion.StatusAttended, (bool?)null),
            (TrainingAttendanceCompletion.StatusAttended, (bool?)null),
        };
        Assert.Equal(100m, TrainingAttendanceCompletion.CalculatePercent(rows));
        Assert.Equal(100m, TrainingAttendanceCompletion.CalculatePercent(2, 2));
    }

    [Fact]
    public void CalculatePercent_HalfAttended_Returns50()
    {
        var rows = new[]
        {
            (TrainingAttendanceCompletion.StatusAttended, (bool?)null),
            (TrainingAttendanceCompletion.StatusAssigned, (bool?)null),
            (TrainingAttendanceCompletion.StatusAbsent, (bool?)null),
            (TrainingAttendanceCompletion.StatusExcused, (bool?)null),
        };
        Assert.Equal(25m, TrainingAttendanceCompletion.CalculatePercent(rows));
        Assert.Equal(50m, TrainingAttendanceCompletion.CalculatePercent(4, 2));
    }

    [Fact]
    public void CalculatePercent_RequirePass_CountsOnlyPassedAttendances()
    {
        var rows = new[]
        {
            (TrainingAttendanceCompletion.StatusAttended, (bool?)true),
            (TrainingAttendanceCompletion.StatusAttended, (bool?)false),
            (TrainingAttendanceCompletion.StatusAttended, (bool?)null),
            (TrainingAttendanceCompletion.StatusAssigned, (bool?)null),
        };
        Assert.Equal(25m, TrainingAttendanceCompletion.CalculatePercent(rows, requirePass: true));
    }

    [Theory]
    [InlineData("Attended", null, false, true)]
    [InlineData("attended", null, false, true)]
    [InlineData("Assigned", null, false, false)]
    [InlineData("Attended", false, true, false)]
    [InlineData("Attended", true, true, true)]
    public void IsCompleted_RespectsStatusAndPassRequirement(string status, bool? passed, bool requirePass, bool expected)
    {
        Assert.Equal(expected, TrainingAttendanceCompletion.IsCompleted(status, passed, requirePass));
    }
}
