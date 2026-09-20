using FluentAssertions;

namespace CreditCalculator.Calculations.Tests;

public class ScheduleValidationTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1000)]
    public void BuildSchedule_Annuity_InvalidPrincipal_Throws(decimal principal)
    {
        var act = () => AnnuityScheduleCalculator.BuildSchedule(principal, 18m, 24, new DateOnly(2026, 1, 1));

        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("principal");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-12)]
    public void BuildSchedule_Annuity_InvalidTerm_Throws(int termMonths)
    {
        var act = () => AnnuityScheduleCalculator.BuildSchedule(20000m, 18m, termMonths, new DateOnly(2026, 1, 1));

        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("termMonths");
    }

    [Fact]
    public void BuildSchedule_Annuity_NegativeRate_Throws()
    {
        var act = () => AnnuityScheduleCalculator.BuildSchedule(20000m, -1m, 24, new DateOnly(2026, 1, 1));

        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("annualRatePercent");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1000)]
    public void BuildSchedule_Differentiated_InvalidPrincipal_Throws(decimal principal)
    {
        var act = () => DifferentiatedScheduleCalculator.BuildSchedule(principal, 18m, 24, new DateOnly(2026, 1, 1));

        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("principal");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-12)]
    public void BuildSchedule_Differentiated_InvalidTerm_Throws(int termMonths)
    {
        var act = () => DifferentiatedScheduleCalculator.BuildSchedule(20000m, 18m, termMonths, new DateOnly(2026, 1, 1));

        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("termMonths");
    }

    [Fact]
    public void BuildSchedule_Differentiated_NegativeRate_Throws()
    {
        var act = () => DifferentiatedScheduleCalculator.BuildSchedule(20000m, -1m, 24, new DateOnly(2026, 1, 1));

        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("annualRatePercent");
    }

    [Fact]
    public void CalculateMonthlyPayment_Annuity_InvalidPrincipal_Throws()
    {
        var act = () => AnnuityScheduleCalculator.CalculateMonthlyPayment(0m, 18m, 24);

        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("principal");
    }
}
