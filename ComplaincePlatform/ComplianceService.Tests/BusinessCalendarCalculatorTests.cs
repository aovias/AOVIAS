using ComplianceService.Application.Contracts;
using ComplianceService.Application.Scheduling;
using ComplianceService.Application.Services;
using ComplianceService.Domain.Enums;
using Xunit;

namespace ComplianceService.Tests;

public sealed class BusinessCalendarCalculatorTests
{
    [Fact]
    public void AddWorkingHours_skips_weekend_and_holiday()
    {
        // Friday 16:00 + 4 working hours with Mon–Fri 09:00–18:00 and Monday holiday
        // Remaining Friday: 2h → then skip Sat/Sun/Mon holiday → Tuesday 09:00 + 2h = Tuesday 11:00
        var start = new DateTime(2026, 7, 24, 16, 0, 0, DateTimeKind.Utc); // Friday
        var workingDays = BusinessCalendarCalculator.DefaultWorkingDays;
        var holidays = new HashSet<DateOnly> { new(2026, 7, 27) }; // Monday

        var due = BusinessCalendarCalculator.AddWorkingHours(
            start,
            workingHours: 4,
            workingDays,
            holidays,
            BusinessCalendarCalculator.DefaultWorkingHoursStart,
            BusinessCalendarCalculator.DefaultWorkingHoursEnd);

        Assert.Equal(new DateTime(2026, 7, 28, 11, 0, 0, DateTimeKind.Utc), due);
    }

    [Fact]
    public void CountWorkingHours_excludes_weekend()
    {
        var from = new DateTime(2026, 7, 24, 9, 0, 0, DateTimeKind.Utc); // Friday 09:00
        var to = new DateTime(2026, 7, 27, 12, 0, 0, DateTimeKind.Utc);   // Monday 12:00

        var hours = BusinessCalendarCalculator.CountWorkingHours(
            from,
            to,
            BusinessCalendarCalculator.DefaultWorkingDays,
            holidays: new HashSet<DateOnly>(),
            BusinessCalendarCalculator.DefaultWorkingHoursStart,
            BusinessCalendarCalculator.DefaultWorkingHoursEnd);

        // Friday 9h + Monday 3h = 12
        Assert.Equal(12, hours);
    }

    [Fact]
    public void CalculateStatusWithCalendar_uses_working_hours_percent()
    {
        var start = new DateTime(2026, 7, 20, 9, 0, 0, DateTimeKind.Utc); // Monday
        var due = new DateTime(2026, 7, 24, 18, 0, 0, DateTimeKind.Utc);   // Friday EOD (5 working days * 9h = 45h)
        var now = new DateTime(2026, 7, 23, 13, 30, 0, DateTimeKind.Utc);  // Thursday 13:30 → elapsed ~31.5h ≈ 70%
        var calendar = new BusinessCalendarSnapshot
        {
            WorkingDays = BusinessCalendarCalculator.DefaultWorkingDays,
            Holidays = [],
            WorkingHoursStart = BusinessCalendarCalculator.DefaultWorkingHoursStart,
            WorkingHoursEnd = BusinessCalendarCalculator.DefaultWorkingHoursEnd
        };

        var status = SlaEvaluationService.CalculateStatusWithCalendar(
            start, due, warningThresholdPercent: 80, now, calendar);

        Assert.Equal(SlaStatus.OnTrack, status);

        var late = new DateTime(2026, 7, 24, 12, 0, 0, DateTimeKind.Utc); // Friday noon → ~39/45 ≈ 86.7% → Warning
        var warning = SlaEvaluationService.CalculateStatusWithCalendar(
            start, due, warningThresholdPercent: 80, late, calendar);
        Assert.Equal(SlaStatus.Warning, warning);
    }

    [Fact]
    public void ParseWorkingDays_falls_back_to_weekdays()
    {
        var days = BusinessCalendarCalculator.ParseWorkingDays([]);
        Assert.Contains(DayOfWeek.Monday, days);
        Assert.DoesNotContain(DayOfWeek.Sunday, days);
    }
}
