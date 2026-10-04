using ApplicationEntity = CreditCalculator.Domain.Entities.Application;

namespace CreditCalculator.Application.Scoring;

public interface IScoringService
{
    ScoringEvaluation Evaluate(ApplicationEntity application);
}