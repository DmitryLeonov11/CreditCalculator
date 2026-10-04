namespace CreditCalculator.Application.Scoring;

public sealed record ScoringEvaluation(
    int Score,
    ScoringDecision Decision,
    IReadOnlyList<ScoringRuleResult> RuleResults);
