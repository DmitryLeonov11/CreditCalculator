using ApplicationEntity = CreditCalculator.Domain.Entities.Application;

namespace CreditCalculator.Application.Scoring;

public interface IScoringService
{
    IReadOnlyList<ScoringRuleResult> Evaluate(ApplicationEntity application);
}