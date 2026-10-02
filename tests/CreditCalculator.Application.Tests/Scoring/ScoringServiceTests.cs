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

        var results = service.Evaluate(new ApplicationEntity());

        results.Select(result => result.RuleCode).Should().Equal("FIRST", "SECOND");
    }

    private sealed class TestRule(string code) : IScoringRule
    {
        public ScoringRuleResult Evaluate(ApplicationEntity application) => new(code, code, true, 0, string.Empty);
    }
}