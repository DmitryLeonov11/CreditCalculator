using CreditCalculator.Application.Scoring;
using CreditCalculator.Domain.Entities;
using FluentAssertions;
using Xunit;
using ApplicationEntity = CreditCalculator.Domain.Entities.Application;

namespace CreditCalculator.Application.Tests.Scoring;

public class ScoringServiceTests
{
    [Fact]
    public void Evaluate_ExecutesAllRegisteredRulesInStableOrder()
    {
        var service = new ScoringService([new TestRule("SECOND"), new TestRule("FIRST")]);

        var results = service.Evaluate(new ApplicationEntity()).RuleResults;

        results.Select(result => result.RuleCode).Should().Equal("FIRST", "SECOND");
    }

    [Theory]
    [InlineData(70, ScoringDecision.AutoApproved)]
    [InlineData(40, ScoringDecision.UnderReview)]
    [InlineData(39, ScoringDecision.Rejected)]
    public void Evaluate_UsesScoreThresholds(int score, ScoringDecision decision)
    {
        var service = new ScoringService([new TestRule("SCORE", score)]);

        var result = service.Evaluate(new ApplicationEntity());

        result.Score.Should().Be(score);
        result.Decision.Should().Be(decision);
    }

    [Fact]
    public void Evaluate_StopRuleRejectsRegardlessOfScore()
    {
        var service = new ScoringService([new TestRule("STOP", 100, passed: false, isStopRule: true)]);

        var result = service.Evaluate(new ApplicationEntity());

        result.Score.Should().Be(100);
        result.Decision.Should().Be(ScoringDecision.Rejected);
    }

    [Theory]
    [InlineData(120, 100)]
    [InlineData(-20, 0)]
    public void Evaluate_ClampsScoreToZeroThroughOneHundred(int points, int expectedScore)
    {
        var service = new ScoringService([new TestRule("SCORE", points)]);

        service.Evaluate(new ApplicationEntity()).Score.Should().Be(expectedScore);
    }

    private sealed class TestRule(string code, int points = 0, bool passed = true, bool isStopRule = false) : IScoringRule
    {
        public ScoringRuleResult Evaluate(ApplicationEntity application) =>
            new(code, code, passed, points, string.Empty, isStopRule);
    }
}