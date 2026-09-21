using CreditCalculator.Calculations.Schedules;
using FluentAssertions;

namespace CreditCalculator.Calculations.Tests;

public class EffectiveRateCalculatorTests
{
    [Fact]
    public void Calculate_AnnuitySchedule_MatchesCompoundedNominalRate()
    {
        var schedule = AnnuityScheduleCalculator.BuildSchedule(20000m, 18m, 24, new DateOnly(2026, 1, 1));

        var effectiveRate = EffectiveRateCalculator.Calculate(20000m, schedule.Payments);

        effectiveRate.Should().BeApproximately(19.5618m, 0.01m);
    }

    [Fact]
    public void Calculate_ZeroRateSchedule_ReturnsZero()
    {
        var schedule = AnnuityScheduleCalculator.BuildSchedule(12000m, 0m, 12, new DateOnly(2026, 1, 1));

        var effectiveRate = EffectiveRateCalculator.Calculate(12000m, schedule.Payments);

        effectiveRate.Should().BeApproximately(0m, 0.0001m);
    }

    [Fact]
    public void CalculateMonthlyRate_Converges_DiscountedPaymentsMatchPrincipal()
    {
        var schedule = AnnuityScheduleCalculator.BuildSchedule(150000m, 12m, 180, new DateOnly(2026, 1, 1));

        var monthlyRate = EffectiveRateCalculator.CalculateMonthlyRate(150000m, schedule.Payments);
        var presentValue = schedule.Payments.Sum(item => item.Payment / Pow(1m + monthlyRate, item.Number));

        presentValue.Should().BeApproximately(150000m, 0.01m);
    }

    private static decimal Pow(decimal value, int exponent)
    {
        var result = 1m;
        for (var i = 0; i < exponent; i++)
            result *= value;

        return result;
    }
}
