using CreditCalculator.Application.Scoring.Rules;
using CreditCalculator.Domain.Entities;
using CreditCalculator.Domain.Enums;
using FluentAssertions;
using Xunit;
using ApplicationEntity = CreditCalculator.Domain.Entities.Application;

namespace CreditCalculator.Application.Tests.Scoring;

public class AgeScoringRuleTests
{
    private readonly AgeScoringRule _rule = new();

    [Fact]
    public void Evaluate_ApplicantIsYoungerThan21_Fails()
    {
        var application = CreateApplication(new DateOnly(2005, 1, 2), Gender.Male, 12);

        var result = _rule.Evaluate(application);

        result.Passed.Should().BeFalse();
        result.Details.Should().Contain("минимальный возраст — 21");
    }

    [Theory]
    [InlineData(Gender.Male, 1963, 4, 1, 12)]
    [InlineData(Gender.Female, 1968, 12, 1, 11)]
    public void Evaluate_TermReachesRetirementDate_Fails(
        Gender gender,
        int birthYear,
        int birthMonth,
        int birthDay,
        int termMonths)
    {
        var application = CreateApplication(new DateOnly(birthYear, birthMonth, birthDay), gender, termMonths);

        var result = _rule.Evaluate(application);

        result.Passed.Should().BeFalse();
        result.Details.Should().Contain("пенсионного возраста");
    }

    [Theory]
    [InlineData(Gender.Male, 1963, 4, 2, 3)]
    [InlineData(Gender.Female, 1968, 12, 2, 10)]
    public void Evaluate_TermEndsBeforeRetirementDate_Passes(
        Gender gender,
        int birthYear,
        int birthMonth,
        int birthDay,
        int termMonths)
    {
        var application = CreateApplication(new DateOnly(birthYear, birthMonth, birthDay), gender, termMonths);

        var result = _rule.Evaluate(application);

        result.Passed.Should().BeTrue();
        result.RuleCode.Should().Be(AgeScoringRule.Code);
        result.Points.Should().Be(0);
    }

    [Fact]
    public void Evaluate_RequiredSnapshotMissing_FailsWithExplanation()
    {
        var result = _rule.Evaluate(new ApplicationEntity());

        result.Passed.Should().BeFalse();
        result.Details.Should().Contain("отсутствуют");
    }

    private static ApplicationEntity CreateApplication(DateOnly birthDate, Gender gender, int termMonths) => new()
    {
        CreatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
        TermMonths = termMonths,
        BirthDateAtApply = birthDate,
        GenderAtApply = gender
    };
}