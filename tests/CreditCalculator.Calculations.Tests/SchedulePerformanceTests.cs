using System.Diagnostics;
using CreditCalculator.Calculations.Schedules;
using FluentAssertions;

namespace CreditCalculator.Calculations.Tests;

public class SchedulePerformanceTests
{
    [Fact]
    public void BuildSchedule_360Months_CompletesUnderOneHundredMilliseconds()
    {
        var stopwatch = Stopwatch.StartNew();

        AnnuityScheduleCalculator.BuildSchedule(150000m, 12m, 360, new DateOnly(2026, 1, 1));

        stopwatch.Stop();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(100);
    }
}
