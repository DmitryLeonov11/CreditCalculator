namespace CreditCalculator.Application.Scoring;

public sealed record ScoringRuleResult(
    string RuleCode,
    string RuleName,
    bool Passed,
    int Points,
    string Details,
    bool IsStopRule = false);
