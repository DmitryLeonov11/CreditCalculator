using ApplicationEntity = CreditCalculator.Domain.Entities.Application;

namespace CreditCalculator.Application.Scoring;

public interface IScoringRule
{
    ScoringRuleResult Evaluate(ApplicationEntity application);
}