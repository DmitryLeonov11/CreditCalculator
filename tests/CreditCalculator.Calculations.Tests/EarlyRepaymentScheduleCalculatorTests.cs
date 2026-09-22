using CreditCalculator.Calculations.Models;
using CreditCalculator.Calculations.Schedules;
using FluentAssertions;

namespace CreditCalculator.Calculations.Tests;

public class EarlyRepaymentScheduleCalculatorTests
{
    private static readonly DateOnly FirstPaymentDate = new(2026, 1, 1);

    [Fact]
    public void BuildReduceTermSchedule_SingleEarlyRepayment_ShortensTermAndKeepsPaymentConstant()
    {
        var earlyRepayments = new[] { new EarlyRepayment(6, 20000m) };

        var result = EarlyRepaymentScheduleCalculator.BuildReduceTermSchedule(
            100000m, 12m, 24, FirstPaymentDate, earlyRepayments);

        result.Payments.Should().HaveCount(20);
        result.Payments.Take(5).Should().OnlyContain(item => item.Payment == 4707.35m);
        result.Payments[5].Payment.Should().Be(24707.35m);
        result.TotalPaid.Should().Be(109525.38m);
        result.Overpayment.Should().Be(9525.38m);
        result.Payments[^1].RemainingBalance.Should().Be(0m);
    }

    [Fact]
    public void BuildReducePaymentSchedule_SingleEarlyRepayment_KeepsTermAndLowersPayment()
    {
        var earlyRepayments = new[] { new EarlyRepayment(6, 20000m) };

        var result = EarlyRepaymentScheduleCalculator.BuildReducePaymentSchedule(
            100000m, 12m, 24, FirstPaymentDate, earlyRepayments);

        result.Payments.Should().HaveCount(24);
        result.Payments.Take(5).Should().OnlyContain(item => item.Payment == 4707.35m);
        result.Payments[5].Payment.Should().Be(24707.35m);
        result.Payments[6].Payment.Should().Be(3487.71m);
        result.TotalPaid.Should().Be(111022.79m);
        result.Overpayment.Should().Be(11022.79m);
        result.Payments[^1].RemainingBalance.Should().Be(0m);
    }

    [Fact]
    public void ReduceTermMode_SavesMoreInterestThanReducePaymentMode()
    {
        var earlyRepayments = new[] { new EarlyRepayment(6, 20000m) };

        var reduceTerm = EarlyRepaymentScheduleCalculator.BuildReduceTermSchedule(
            100000m, 12m, 24, FirstPaymentDate, earlyRepayments);
        var reducePayment = EarlyRepaymentScheduleCalculator.BuildReducePaymentSchedule(
            100000m, 12m, 24, FirstPaymentDate, earlyRepayments);

        reduceTerm.Overpayment.Should().BeLessThan(reducePayment.Overpayment);
    }

    [Theory]
    [MemberData(nameof(EarlyRepaymentModes))]
    public void MultipleEarlyRepayments_PrincipalPartsSumToPrincipalExactly(
        Func<decimal, decimal, int, DateOnly, IReadOnlyList<EarlyRepayment>, PaymentScheduleResult> buildSchedule)
    {
        var earlyRepayments = new[]
        {
            new EarlyRepayment(3, 10000m),
            new EarlyRepayment(3, 5000m),
            new EarlyRepayment(10, 15000m)
        };

        var result = buildSchedule(100000m, 12m, 24, FirstPaymentDate, earlyRepayments);

        result.Payments.Sum(item => item.PrincipalPart).Should().Be(100000m);
        result.Payments[^1].RemainingBalance.Should().Be(0m);
    }

    [Theory]
    [MemberData(nameof(EarlyRepaymentModes))]
    public void OverpaymentExceedingBalance_ClosesLoanEarlyWithoutGoingNegative(
        Func<decimal, decimal, int, DateOnly, IReadOnlyList<EarlyRepayment>, PaymentScheduleResult> buildSchedule)
    {
        var earlyRepayments = new[] { new EarlyRepayment(3, 500000m) };

        var result = buildSchedule(100000m, 12m, 24, FirstPaymentDate, earlyRepayments);

        result.Payments.Should().HaveCount(3);
        result.Payments.Should().OnlyContain(item => item.RemainingBalance >= 0m);
        result.Payments[^1].RemainingBalance.Should().Be(0m);
    }

    public static TheoryData<Func<decimal, decimal, int, DateOnly, IReadOnlyList<EarlyRepayment>, PaymentScheduleResult>> EarlyRepaymentModes() => new()
    {
        EarlyRepaymentScheduleCalculator.BuildReduceTermSchedule,
        EarlyRepaymentScheduleCalculator.BuildReducePaymentSchedule
    };
}
