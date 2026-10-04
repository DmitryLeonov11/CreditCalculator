using ApplicationEntity = CreditCalculator.Domain.Entities.Application;

namespace CreditCalculator.Application.Scoring;

public sealed class ScoringService : IScoringService
{
    private readonly IReadOnlyList<IScoringRule> _rules;

    public ScoringService(IEnumerable<IScoringRule> rules)
    {
        _rules = rules.ToArray();
    }

    public ScoringEvaluation Evaluate(ApplicationEntity application)
    {
        var results = _rules.Select(rule => rule.Evaluate(application))
            .OrderBy(result => result.RuleCode, StringComparer.Ordinal)
            .ToArray();

        var score = Math.Clamp(results.Sum(result => result.Points), 0, 100);
        var decision = results.Any(result => result.IsStopRule && !result.Passed)
            ? ScoringDecision.Rejected
            : score switch
            {
                >= 70 => ScoringDecision.AutoApproved,
                >= 40 => ScoringDecision.UnderReview,
                _ => ScoringDecision.Rejected
            };

        return new ScoringEvaluation(score, decision, results);
    }
}