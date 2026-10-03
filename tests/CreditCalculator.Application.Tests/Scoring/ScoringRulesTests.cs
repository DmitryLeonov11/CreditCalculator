using CreditCalculator.Application.Scoring;
using CreditCalculator.Application.Scoring.Rules;
using CreditCalculator.Domain.Entities;
using CreditCalculator.Domain.Enums;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit;
using ApplicationEntity = CreditCalculator.Domain.Entities.Application;

namespace CreditCalculator.Application.Tests.Scoring;

public class EmploymentLengthScoringRuleTests
{
    private readonly EmploymentLengthScoringRule _rule = new();

    [Theory]
    [InlineData(2, false, 0)]
    [InlineData(3, true, 0)]
    [InlineData(24, true, 0)]
    [InlineData(25, true, 10)]
    public void Evaluate_UsesMinimumAndBonusBoundaries(int months, bool passed, int points)
    {
        var result = _rule.Evaluate(new ApplicationEntity { EmploymentMonthsAtApply = months });

        result.Passed.Should().Be(passed);
        result.Points.Should().Be(points);
    }
}

public class DebtToIncomeScoringRuleTests
{
    private readonly ScoringOptions _options = new()
    {
        MinimumLivingWageByn = 500m,
        BelarusianGoodsPdnThreshold = 0.50m
    };

    [Theory]
    [InlineData(false, 4000, 0, true, 20)]
    [InlineData(false, 2500, 0, true, 10)]
    [InlineData(false, 2500, 1, false, 0)]
    [InlineData(true, 2000, 0, true, 10)]
    [InlineData(true, 2000, 1, false, 0)]
    public void Evaluate_UsesConfiguredProductThreshold(
        bool belarusianMade,
        int income,
        int existingPayments,
        bool passed,
        int points)
    {
        var rule = new DebtToIncomeScoringRule(Microsoft.Extensions.Options.Options.Create(_options));
        var application = CreateApplication(income, existingPayments);
        application.CreditProduct.IsBelarusianMade = belarusianMade;

        var result = rule.Evaluate(application);

        result.Passed.Should().Be(passed);
        result.Points.Should().Be(points);
    }

    [Fact]
    public void Evaluate_DependentsReduceIncomeUsedForPdn()
    {
        var application = CreateApplication(income: 2500m, existingPayments: 0m);
        application.DependentsAtApply = 2;

        var result = new DebtToIncomeScoringRule(Microsoft.Extensions.Options.Options.Create(_options)).Evaluate(application);

        result.Passed.Should().BeFalse();
        result.Details.Should().Contain("ПДН").And.Contain("66.7");
    }

    private static ApplicationEntity CreateApplication(decimal income, decimal existingPayments) => new()
    {
        Amount = 12_000m,
        TermMonths = 12,
        InterestRate = 0m,
        IncomeAtApply = income,
        ExistingPaymentsAtApply = existingPayments,
        DependentsAtApply = 0,
        CreditProduct = new CreditProduct { Purpose = CreditPurpose.Consumer }
    };
}

public class LoanToIncomeScoringRuleTests
{
    private readonly LoanToIncomeScoringRule _rule = new();

    [Theory]
    [InlineData(1000, true, 0)]
    [InlineData(999, true, -10)]
    public void Evaluate_AppliesPenaltyOnlyAboveTwelveIncomes(int income, bool passed, int points)
    {
        var result = _rule.Evaluate(new ApplicationEntity { Amount = 12_000m, IncomeAtApply = income });

        result.Passed.Should().Be(passed);
        result.Points.Should().Be(points);
    }
}

public class DependentsScoringRuleTests
{
    [Fact]
    public void Evaluate_DeductsConfiguredLivingMinimumPerDependent()
    {
        var rule = new DependentsScoringRule(Microsoft.Extensions.Options.Options.Create(new ScoringOptions { MinimumLivingWageByn = 500m }));
        var application = new ApplicationEntity { IncomeAtApply = 2500m, DependentsAtApply = 2 };

        var result = rule.Evaluate(application);

        result.Passed.Should().BeTrue();
        result.Points.Should().Be(0);
        result.Details.Should().Contain("1000.00 BYN").And.Contain("1500.00 BYN");
    }
}

public class MortgageDownPaymentScoringRuleTests
{
    private readonly MortgageDownPaymentScoringRule _rule = new();

    [Theory]
    [InlineData(19_999, -10)]
    [InlineData(20_000, 0)]
    public void Evaluate_AppliesPenaltyBelowTwentyPercent(int downPayment, int points)
    {
        var application = new ApplicationEntity
        {
            Amount = 80_000m,
            DownPaymentAmountAtApply = downPayment,
            CreditProduct = new CreditProduct { Purpose = CreditPurpose.Mortgage }
        };

        var result = _rule.Evaluate(application);

        result.Passed.Should().BeTrue();
        result.Points.Should().Be(points);
    }

    [Fact]
    public void Evaluate_NonMortgageProductIsNotPenalized()
    {
        var application = new ApplicationEntity
        {
            Amount = 80_000m,
            CreditProduct = new CreditProduct { Purpose = CreditPurpose.Auto }
        };

        var result = _rule.Evaluate(application);

        result.Passed.Should().BeTrue();
        result.Points.Should().Be(0);
    }
}
