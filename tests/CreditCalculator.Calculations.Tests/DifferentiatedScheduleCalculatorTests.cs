using CreditCalculator.Calculations.Schedules;
using FluentAssertions;

namespace CreditCalculator.Calculations.Tests;

public class DifferentiatedScheduleCalculatorTests
{
    [Fact]
    public void BuildSchedule_MatchesReferenceValues()
    {
        var result = DifferentiatedScheduleCalculator.BuildSchedule(120000m, 12m, 12, new DateOnly(2026, 1, 1));

        result.Payments.Should().HaveCount(12);
        result.Payments[0].PrincipalPart.Should().Be(10000.00m);
        result.Payments[0].InterestPart.Should().Be(1200.00m);
        result.Payments[0].Payment.Should().Be(11200.00m);
        result.Payments[^1].Payment.Should().Be(10100.00m);
        result.TotalPaid.Should().Be(127800.00m);
        result.Overpayment.Should().Be(7800.00m);
        result.Payments[^1].RemainingBalance.Should().Be(0m);
    }

    [Fact]
    public void BuildSchedule_PaymentDecreasesEveryMonth()
    {
        var result = DifferentiatedScheduleCalculator.BuildSchedule(20000m, 18m, 24, new DateOnly(2026, 1, 1));

        for (var i = 1; i < result.Payments.Count; i++)
            result.Payments[i].Payment.Should().BeLessThan(result.Payments[i - 1].Payment);
    }

    [Fact]
    public void BuildSchedule_PrincipalPartIsConstantExceptLastPayment()
    {
        var result = DifferentiatedScheduleCalculator.BuildSchedule(20000m, 18m, 24, new DateOnly(2026, 1, 1));

        result.Payments.Take(result.Payments.Count - 1)
            .Should().OnlyContain(item => item.PrincipalPart == result.Payments[0].PrincipalPart);
    }

    [Fact]
    public void BuildSchedule_RemainingBalanceNeverGoesNegative()
    {
        var result = DifferentiatedScheduleCalculator.BuildSchedule(20000m, 18m, 24, new DateOnly(2026, 1, 1));

        result.Payments.Should().OnlyContain(item => item.RemainingBalance >= 0m);
    }
}
