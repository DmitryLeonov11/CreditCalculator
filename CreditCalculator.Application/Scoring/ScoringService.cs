using ApplicationEntity = CreditCalculator.Domain.Entities.Application;

namespace CreditCalculator.Application.Scoring;

public sealed class ScoringService : IScoringService
{
    private readonly IReadOnlyList<IScoringRule> _rules;

    public ScoringService(IEnumerable<IScoringRule> rules)
    {
        _rules = rules.ToArray();
    }

    public IReadOnlyList<ScoringRuleResult> Evaluate(ApplicationEntity application) =>
        _rules.Select(rule => rule.Evaluate(application))
            .OrderBy(result => result.RuleCode, StringComparer.Ordinal)
            .ToArray();
}