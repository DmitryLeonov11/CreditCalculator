using CreditCalculator.Calculations.Schedules;
using FluentAssertions;

namespace CreditCalculator.Calculations.Tests;

public class AnnuityScheduleCalculatorTests
{
    [Fact]
    public void BuildSchedule_ShortTerm_MatchesReferenceValues()
    {
        var result = AnnuityScheduleCalculator.BuildSchedule(20000m, 18m, 24, new DateOnly(2026, 1, 1));

        result.Payments.Should().HaveCount(24);
        result.Payments[0].Payment.Should().Be(998.48m);
        result.TotalPaid.Should().Be(23963.59m);
        result.Overpayment.Should().Be(3963.59m);
        result.Payments[^1].RemainingBalance.Should().Be(0m);
    }

    [Fact]
    public void BuildSchedule_LongTerm_MatchesReferenceValues()
    {
        var result = AnnuityScheduleCalculator.BuildSchedule(150000m, 12m, 180, new DateOnly(2026, 1, 1));

        result.Payments.Should().HaveCount(180);
        result.Payments[0].Payment.Should().Be(1800.25m);
        result.TotalPaid.Should().Be(324046.05m);
        result.Overpayment.Should().Be(174046.05m);
        result.Payments[^1].RemainingBalance.Should().Be(0m);
    }

    [Fact]
    public void CalculateMonthlyPayment_ZeroRate_ReturnsPrincipalDividedByTerm()
    {
        var payment = AnnuityScheduleCalculator.CalculateMonthlyPayment(12000m, 0m, 12);

        payment.Should().Be(1000m);
    }

    [Fact]
    public void BuildSchedule_ZeroRate_HasNoInterestAndNoKopeckTail()
    {
        var result = AnnuityScheduleCalculator.BuildSchedule(12000m, 0m, 12, new DateOnly(2026, 1, 1));

        result.Payments.Should().OnlyContain(item => item.InterestPart == 0m);
        result.TotalPaid.Should().Be(12000m);
        result.Overpayment.Should().Be(0m);
        result.Payments[^1].RemainingBalance.Should().Be(0m);
    }

    [Fact]
    public void BuildSchedule_RemainingBalance_NeverGoesNegative()
    {
        var result = AnnuityScheduleCalculator.BuildSchedule(20000m, 18m, 24, new DateOnly(2026, 1, 1));

        result.Payments.Should().OnlyContain(item => item.RemainingBalance >= 0m);
    }
}
