using CreditCalculator.Calculations.Schedules;
using FluentAssertions;

namespace CreditCalculator.Calculations.Tests;

public class PrincipalSumInvariantTests
{
    [Theory]
    [InlineData(20000, 18, 24)]
    [InlineData(150000, 12, 180)]
    [InlineData(12000, 0, 12)]
    [InlineData(500, 60, 1)]
    [InlineData(300000, 1, 240)]
    public void AnnuitySchedule_PrincipalPartsSumToPrincipal(decimal principal, decimal annualRatePercent, int termMonths)
    {
        var result = AnnuityScheduleCalculator.BuildSchedule(principal, annualRatePercent, termMonths, new DateOnly(2026, 1, 1));

        result.Payments.Sum(item => item.PrincipalPart).Should().Be(principal);
    }

    [Theory]
    [InlineData(20000, 18, 24)]
    [InlineData(150000, 12, 180)]
    [InlineData(12000, 0, 12)]
    [InlineData(500, 60, 1)]
    [InlineData(300000, 1, 240)]
    public void DifferentiatedSchedule_PrincipalPartsSumToPrincipal(decimal principal, decimal annualRatePercent, int termMonths)
    {
        var result = DifferentiatedScheduleCalculator.BuildSchedule(principal, annualRatePercent, termMonths, new DateOnly(2026, 1, 1));

        result.Payments.Sum(item => item.PrincipalPart).Should().Be(principal);
    }
}
